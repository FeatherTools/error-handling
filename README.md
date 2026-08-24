# <img src="https://github.com/FeatherTools/.github/blob/main/profile/feather-logo-200.png" alt="FeatherTools Logo" width="100" height="100"> Error-Handling

[![NuGet](https://img.shields.io/nuget/v/Feather.ErrorHandling.svg)](https://www.nuget.org/packages/Feather.ErrorHandling)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Feather.ErrorHandling.svg)](https://www.nuget.org/packages/Feather.ErrorHandling)
[![Checks](https://github.com/FeatherTools/error-handling/actions/workflows/tests.yaml/badge.svg)](https://github.com/FeatherTools/error-handling/actions/workflows/tests.yaml)

> Library for proper error handling with Option, Result, AsyncResult and their computation expressions.

## Inspiration
- Started as a copy of https://github.com/swlaschin/DomainModelingMadeFunctional/blob/master/src/OrderTaking/Result.fs
- Similar project is https://github.com/fsprojects/Chessie
- Also Inspired by [Suave/YoLo](https://github.com/SuaveIO/suave/blob/master/src/Suave/Utils/YoLo.fs)

## Install

```sh
paket add Feather.ErrorHandling
```

**Note**: You can also use this library in a Fable project.

## Validation

`validation {}` is applicative. It evaluates every binding joined with `and!` and collects all of their failures.
You can bind a `Result<'T, 'Failure>` or a `Validation<'T, 'Failure>` directly.

```fs
open Feather.ErrorHandling

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
    and! age = result {  // only one specific error may come from the result
        let! age = age |> Int32.tryParse |> Result.ofOption InvalidAge
        if age < 0 then return! Error AgeNegative
        if age < 18 then return! Error Underage

        return age
    }

    return { Name = name; Age = age }
}

createPerson "Alice" "30"     // Ok { Name = "Alice"; Age = 30 }
createPerson "Alice" "thirty" // Error [ InvalidAge ]
createPerson "" "-1"          // Error [ NameEmpty; AgeNegative ]
createPerson "Alice" "16"     // Error [ Underage ]
```

A `Validation<'T, 'Failure>` is a `Result<'T, 'Failure list>`, so when a stage should run only if the previous one
succeeded, bind the stages in an outer `result {}`.

## Release
1. Increment version in `ErrorHandling.fsproj`
2. Update `CHANGELOG.md`
3. Commit new version and tag it

## Development
### Requirements
- [dotnet core](https://dotnet.microsoft.com/learn/dotnet/hello-world-tutorial)

### Build
```bash
./build.sh build
```

### Tests
```bash
./build.sh -t tests
```
