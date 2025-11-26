module Feather.ErrorHandling.AsyncResult.Test

open Expecto
open Feather.ErrorHandling

module Login =
    open Feather.ErrorHandling.AsyncResult.Operators

    type User = {
        Username: string
        Password: string
    }
    type Token = Token of string

    type LoginError =
        | InvalidUser
        | InvalidPwd
        | Unauthorized of AuthError
        | TokenErr of TokenError

    and AuthError = AuthError of string
    and TokenError = TokenError of string

    let testLogin
        (tryGetUser: string -> Async<User option>)
        (isPwdValid: string -> User -> bool)
        (authorize: User -> AsyncResult<unit, AuthError>)
        (createAuthToken: User -> Result<Token, TokenError>)
        (username, password): AsyncResult<Token, LoginError> =

        asyncResult {
            let! user = username |> tryGetUser |> AsyncResult.ofAsyncOption InvalidUser
            do! user |> isPwdValid password |> AsyncResult.ofBool InvalidPwd
            do! user |> authorize <@> Unauthorized

            return! user |> createAuthToken |> Result.mapError TokenErr
        }

open Login

[<Tests>]
let asyncResultTest =
    testList "AsyncResult" [
        testCase "should compile different bind methods" <| fun _ ->
            let ar: AsyncResult<string, string> = asyncResult {
                let! r = Ok ""
                let! err_r = Error ""

                return "of result"
            }

            let av: AsyncResult<string, string list> = asyncResult {
                let validation: Validation<string, string> = Ok ""

                let! v = validation
                let! err_r = validation

                return "of result"
            }

            let ofAr: AsyncResult<string, string> = asyncResult {
                let! r = ar
                let! err_r = ar

                return "of async result"
            }

            let ofA: AsyncResult<string, exn> = asyncResult {
                let! a = async {
                    return ""
                }

                return a
            }

            let ofT: AsyncResult<string, exn> = asyncResult {
                let! a = task {
                    return ""
                }

                return a
            }

            Expect.equal true true "It is only required this test to compile"

        testCase "should handle seq/list creators" <| fun _ ->
            let dataSeq =
                Seq.initInfinite id
                |> Seq.takeWhile (fun i -> if i > 100 then failtest "Sequence is not handled properly" else true)
                |> Seq.map string

            let dataList = [ 0 .. 5 ] |> List.map string

            let assertSame expected message actual =
                match actual, expected with
                | Ok actual, Ok expected ->
                    Expect.equal (actual |> Seq.take 3 |> Seq.toList) (expected |> Seq.take 3 |> Seq.toList) message
                | Error actual, Error expected ->
                    Expect.equal actual expected message
                | _ ->
                    failtestf "Unexpected combination of result (actual/expected) in %A:\nActual %A\nExpected:%A" message actual expected

            [
                "Parallel", AsyncResult.ofParallelAsyncs id
                "Max Parallel", AsyncResult.ofMaxParallelAsyncs 3 id
                "Sequential", AsyncResult.ofSequentialAsyncs id
            ]
            |> List.iter (fun (caseName, f) ->
                dataSeq
                |> Seq.map Async.retn
                |> Seq.take 10
                |> Seq.toList
                |> f
                |> AsyncResult.map Seq.sort
                |> Async.RunSynchronously
                |> assertSame (Ok dataSeq) $"{caseName} sequence of asyncs (of seq)"

                dataList
                |> List.map Async.retn
                |> f
                |> AsyncResult.map Seq.sort
                |> Async.RunSynchronously
                |> assertSame (Ok dataSeq) $"{caseName} sequence of asyncs (of list)"
            )

            dataSeq
            |> Seq.map AsyncResult.ofSuccess
            |> Seq.take 10
            |> Seq.toList
            |> AsyncResult.sequenceM
            |> Async.RunSynchronously
            |> assertSame (Ok dataSeq) "Monadically handle sequance (of seq)"

            dataSeq
            |> Seq.map AsyncResult.ofSuccess
            |> Seq.take 10
            |> Seq.toList
            |> AsyncResult.sequenceA
            |> Async.RunSynchronously
            |> assertSame (Ok dataSeq) "Aplicativelly handle sequance (of seq)"

            ()

        testCase "Login example" <| fun _ ->
            let tryGetUser: string -> Async<User option> = fun username -> async { return Some { Username = username; Password = "password" } }
            let isPwdValid: string -> User -> bool = fun givenPassword { Password = userPassword } -> givenPassword = userPassword
            let authorize: User -> AsyncResult<unit, AuthError> = fun user -> AsyncResult.ofSuccess ()
            let createAuthToken: User -> Result<Token, TokenError> = fun user -> Ok (Token ("success." + user.Username))

            let user = "username", "password"

            let expected = Ok (Token "success.username")

            let login =
                testLogin
                    tryGetUser
                    isPwdValid
                    authorize
                    createAuthToken

            let result =
                user
                |> login
                |> Async.RunSynchronously

            Expect.equal result expected "User should log in."

        testCase "With Retry" <| fun _ ->
            let mutable i = -1

            let action = asyncResult {
                i <- i + 1

                match i with
                | 0 | 1 -> return! Error $"Error {i}"
                | i -> return $"Success {i}"
            }

            let actualWithoutRetry =
                i <- -1
                action
                |> Async.RunSynchronously
            Expect.equal actualWithoutRetry (Error "Error 0") "Action without retry should end with error."

            let actual =
                i <- -1
                action
                |> AsyncResult.retry 100 5
                |> Async.RunSynchronously
            Expect.equal actual (Ok "Success 2") "Action with retries should end with success as soon as possible."

        testCase "With Retry exponential" <| fun _ ->
            let mutable i = -1
            let mutable performedActions = []
            let mutable messages = []

            let expectedMessages = [
                "Retrying [1/10] in 10ms ..."
                "Retrying [2/10] in 20ms ..."
                "Retrying [3/10] in 30ms ..."
                "Retrying [4/10] in 40ms ..."
                "Retrying [5/10] in 50ms ..."
                "Retrying [6/10] in 60ms ..."
                "Retrying [7/10] in 70ms ..."
                "Retrying [8/10] in 80ms ..."
            ]
            let expectedActions = [ 0 .. 8 ]

            let action = asyncResult {
                i <- i + 1
                performedActions <- i :: performedActions
                //printfn "Action %d" i

                match i with
                | fail when fail <= 7 -> return! Error $"Error {i}"
                | i -> return $"Success {i}"
            }

            let actual =
                action
                |> AsyncResult.retryWithExponential (
                    fun m ->
                        messages <- m :: messages
                        //printfn "[Retry.E] %s" m
                ) 10 10
                |> Async.RunSynchronously
            Expect.equal actual (Ok "Success 8") "Action with retries should end with success as soon as possible."
            Expect.equal (performedActions |> List.rev) expectedActions "Actions should be performed in order."
            Expect.equal (messages |> List.rev) expectedMessages "Retry message should be printed."

        testCase "With Retry exponential - with error only" <| fun _ ->
            let mutable i = -1
            let mutable performedActions = []
            let mutable messages = []

            let expectedMessages = [
                "Retrying [1/10] in 10ms ..."
                "Retrying [2/10] in 20ms ..."
                "Retrying [3/10] in 30ms ..."
                "Retrying [4/10] in 40ms ..."
                "Retrying [5/10] in 50ms ..."
                "Retrying [6/10] in 60ms ..."
                "Retrying [7/10] in 70ms ..."
                "Retrying [8/10] in 80ms ..."
                "Retrying [9/10] in 90ms ..."
                "Retrying [10/10] in 100ms ..."
            ]
            let expectedActions = [ 0 .. 10 ]

            let action = asyncResult {
                i <- i + 1
                performedActions <- i :: performedActions
                //printfn "Action %d" i

                return! Error $"Error {i}"
            }

            let actual =
                action
                |> AsyncResult.retryWithExponential (
                    fun m ->
                        messages <- m :: messages
                        //printfn "[Retry.E] %s" m
                ) 10 10
                |> Async.RunSynchronously

            Expect.equal actual (Error "Error 10") "Action with retries should end with success as soon as possible."
            Expect.equal (performedActions |> List.rev) expectedActions "Actions should be performed in order."
            Expect.equal (messages |> List.rev) expectedMessages "Retry message should be printed."

        testCase "With with timeout - not in time" <| fun _ ->
            let action duration = asyncResult {
                //printfn "[Action] Start and return result in %dms" duration
                do! AsyncResult.sleep duration
                //printfn "[Action] Success"
                return "Success"
            }

            let actual =
                action 50
                |> Async.withTimeout 10 (Ok "Not In time")
                |> Async.RunSynchronously

            Expect.equal actual (Ok "Not In time") "Action should not be in time."

        testCase "With with timeout - in time" <| fun _ ->
            let action duration = asyncResult {
                //printfn "[Action] Start and return result in %dms" duration
                do! AsyncResult.sleep duration
                //printfn "[Action] Success"
                return "Success"
            }

            let actual =
                action 50
                |> Async.withTimeout 100 (Ok "Not In time")
                |> Async.RunSynchronously

            Expect.equal actual (Ok "Success") "Action should be in time."
    ]
