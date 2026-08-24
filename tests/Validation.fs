module Feather.ErrorHandling.Validation.Test

open Expecto
open Feather.ErrorHandling

type PersonValidationError =
    | NameEmpty
    | AgeNegative
    | EmailInvalid

module private ReadmeExample =
    type Person = { Name: string; Age: int }

    type PersonError =
        | NameEmpty
        | InvalidAge
        | AgeNegative
        | Underage

    module Int32 =
        let tryParse (s: string) =
            match System.Int32.TryParse(s) with
            | true, value -> Some value
            | false, _ -> None

    let validateName name = if name = "" then Error NameEmpty else Ok name

    let createPerson name age: Validation<Person, PersonError> = validation {
        let! name = validateName name
        and! age = result {
            let! age = age |> Int32.tryParse |> Result.ofOption InvalidAge
            if age < 0 then return! Error AgeNegative
            if age < 18 then return! Error Underage
            return age
        }

        return { Name = name; Age = age }
    }

[<Tests>]
let readmeExampleTest =
    testList "Validation README example" [
        testCase "should create a person when all fields are valid" <| fun _ ->
            let actual = ReadmeExample.createPerson "Alice" "30"

            Expect.equal actual (Ok { Name = "Alice"; Age = 30 }) "Should be Ok with the person"

        testCase "should fail when the age is not a number" <| fun _ ->
            let actual = ReadmeExample.createPerson "Alice" "thirty"

            Expect.equal actual (Error [ ReadmeExample.InvalidAge ]) "Should fail with InvalidAge only"

        testCase "should accumulate failures of all invalid fields" <| fun _ ->
            let actual = ReadmeExample.createPerson "" "-1"

            Expect.equal actual (Error [ ReadmeExample.NameEmpty; ReadmeExample.AgeNegative ]) "Should accumulate both failures"

        testCase "should fail with the first failing age check" <| fun _ ->
            let actual = ReadmeExample.createPerson "Alice" "16"

            Expect.equal actual (Error [ ReadmeExample.Underage ]) "Should fail with Underage only"
    ]

[<Tests>]
let validationTest =
    testList "Validation" [
        testCase "should combine successes with applicative sequencing (and!)" <| fun _ ->
            let actual: Validation<string * int, PersonValidationError> = validation {
                let! name = Ok "John"
                and! age = Ok 42

                return name, age
            }

            Expect.equal actual (Ok ("John", 42)) "Should be Ok with both values"

        testCase "should accumulate failures with applicative sequencing (and!)" <| fun _ ->
            let actual: Validation<string * int * string, PersonValidationError> = validation {
                let! name = Validation.ofResult (Error NameEmpty)
                and! age = Ok 42
                and! email = Validation.ofResult (Error EmailInvalid)

                return name, age, email
            }

            Expect.equal actual (Error [ NameEmpty; EmailInvalid ]) "Should accumulate both failures"

        testCase "should evaluate later bindings after a failure with applicative sequencing (and!)" <| fun _ ->
            let mutable secondEvaluated = false

            let _: Validation<string * int, PersonValidationError> = validation {
                let! name = Validation.ofResult (Error NameEmpty)
                and! age = Ok (secondEvaluated <- true; 42)

                return name, age
            }

            Expect.isTrue secondEvaluated "Should evaluate the Ok binding despite the preceding failure"

        // Validation is a Result, so `result {}` binds validation stages directly,
        // giving monadic sequencing between stages the applicative CE does not offer.
        testCase "should combine validation stages when sequenced through an outer result CE" <| fun _ ->
            let actual: Validation<string * int * string, PersonValidationError> = result {
                let! name, age = validation {
                    let! name = Ok "John"
                    and! age = Ok 42

                    return name, age
                }

                let! email = validation {
                    return! Ok "john@example.com"
                }

                return name, age, email
            }

            Expect.equal actual (Ok ("John", 42, "john@example.com")) "Should combine values from both stages"

        testCase "should short-circuit later stages when sequenced through an outer result CE" <| fun _ ->
            let mutable secondStageEvaluated = false

            let actual: Validation<string * int * string, PersonValidationError> = result {
                let! name, age = validation {
                    let! name = Validation.ofResult (Error NameEmpty)
                    and! age = Validation.ofResult (Error AgeNegative)

                    return name, age
                }

                let! email = validation {
                    return! Ok (secondStageEvaluated <- true; "john@example.com")
                }

                return name, age, email
            }

            Expect.equal actual (Error [ NameEmpty; AgeNegative ]) "Should fail with the first stage's accumulated failures"
            Expect.isFalse secondStageEvaluated "Should not evaluate the second stage"

        testCase "should return from a Validation directly" <| fun _ ->
            let actual: Validation<int, PersonValidationError> = validation {
                return! Ok 42
            }

            Expect.equal actual (Ok 42) "Should pass the Validation through"
    
        testCase "should bind a Result by lifting its failure into the Validation" <| fun _ ->
            let actual: Validation<string * int * string, PersonValidationError> = validation {
                let! name = Error NameEmpty
                and! age = Ok 42
                and! email = Error EmailInvalid

                return name, age, email
            }

            Expect.equal actual (Error [ NameEmpty; EmailInvalid ]) "Should accumulate both lifted failures"

        testCase "should bind a Validation without wrapping its failures again" <| fun _ ->
            let actual: Validation<string * int, PersonValidationError> = validation {
                let! name = Error [ NameEmpty; EmailInvalid ]
                and! age = Validation.ofResult (Error AgeNegative)

                return name, age
            }

            Expect.equal actual (Error [ NameEmpty; EmailInvalid; AgeNegative ]) "Should accumulate failures flat"

        testCase "should handle nested validations" <| fun _ ->
            let actual: Validation<int, PersonValidationError> = validation {
                let! a, b = validation {
                    let! a = Ok 1
                    and! b = Ok 2

                    return a, b
                }

                and! c = validation {
                    return! Ok 3
                }

                return a + b + c
            }

            Expect.equal actual (Ok 6) "Should pass the Validation through"
    ]
