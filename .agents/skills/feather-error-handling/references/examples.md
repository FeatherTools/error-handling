# Examples

All example code for this skill lives here. Examples are ordered by increasing complexity and each is self-contained. Neutral placeholder names are used throughout.

## Basic — `result {}` with a typed error

```fs
open Feather.ErrorHandling

[<RequireQualifiedAccess>]
type ParseError =
    | Empty
    | NotANumber of string
    | Negative of int

let parseQuantity (raw: string): Result<int, ParseError> =
    result {
        let! raw = if raw = "" then Error ParseError.Empty else Ok raw

        let! quantity =
            (match System.Int32.TryParse raw with
             | true, value -> Some value
             | _ -> None)
            |> Result.ofOption (ParseError.NotANumber raw)

        if quantity < 0 then return! Error (ParseError.Negative quantity)

        return quantity
    }

parseQuantity "42"   // Ok 42
parseQuantity "abc"  // Error (NotANumber "abc")
parseQuantity "-1"   // Error (Negative -1)
```

## Side effects without breaking the chain

`tee` / `teeError` run an effect and pass the value through unchanged.

```fs
open Feather.ErrorHandling

let loadSetting (key: string): Result<string, string> =
    if key = "endpoint" then Ok "https://example.test" else Error $"missing setting {key}"

[ "endpoint"; "timeout" ]
|> List.map (
    loadSetting
    >> Result.tee (printfn "loaded: %s")
    >> Result.teeError (eprintfn "warning: %s")
)
|> Result.list   // [ "https://example.test" ] — errors are dropped, not reported
```

## Option helpers and `maybe {}`

```fs
open Feather.ErrorHandling
open Feather.ErrorHandling.Option.Operators

let tryFindPort (name: string): int option =
    if name = "api" then Some 8080 else None

let address =
    maybe {
        let! port = tryFindPort "api"
        return $"localhost:{port}"
    }

let port = tryFindPort "worker" <?=> 80                     // 80
let fallback = tryFindPort "worker" <??> tryFindPort "api"  // Some 8080
let required = tryFindPort "worker" <?!> "port not set"     // Error "port not set"

let lazyDefault = tryFindPort "worker" |> Option.orDefault (fun () -> 80)
let nullable = System.Nullable 3 |> Option.ofNullable        // Some 3
```

## Validation — accumulating failures

`let! ... and! ...` evaluates every binding and concatenates all failures. A `Result` binding is lifted to a one-element failure list; an inner `result {}` contributes at most one failure.

```fs
open Feather.ErrorHandling

type Account = { Name: string; Age: int; Email: string }

[<RequireQualifiedAccess>]
type AccountError =
    | NameEmpty
    | AgeInvalid of string
    | AgeTooLow of int
    | EmailInvalid of string

let validateName name =
    if name = "" then Error AccountError.NameEmpty else Ok name

let validateEmail (email: string) =
    if email.Contains "@" then Ok email else Error (AccountError.EmailInvalid email)

let validateAge (raw: string) =
    result {
        let! age =
            match System.Int32.TryParse raw with
            | true, value -> Ok value
            | _ -> Error (AccountError.AgeInvalid raw)

        if age < 18 then return! Error (AccountError.AgeTooLow age)

        return age
    }

let createAccount name age email: Validation<Account, AccountError> =
    validation {
        let! name = validateName name
        and! age = validateAge age
        and! email = validateEmail email

        return { Name = name; Age = age; Email = email }
    }

createAccount "alice" "30" "alice@example.test"  // Ok { ... }
createAccount "" "16" "nope"
// Error [ NameEmpty; AgeTooLow 16; EmailInvalid "nope" ]
```

## Validation stages sequenced with `result {}`

`validation {}` has no sequential `let!`. A `Validation` is a `Result<_, _ list>`, so bind whole stages in an outer `result {}`: the second stage runs only when the first succeeded.

```fs
open Feather.ErrorHandling

[<RequireQualifiedAccess>]
type OrderError =
    | SkuEmpty
    | QuantityNotPositive of int
    | OutOfStock of string

let checkStock (sku: string) (quantity: int) =
    if sku = "sold-out" then Error (OrderError.OutOfStock sku) else Ok (sku, quantity)

let placeOrder (sku: string) (quantity: int): Validation<string * int, OrderError> =
    result {
        let! sku, quantity =
            validation {
                let! sku = if sku = "" then Error OrderError.SkuEmpty else Ok sku
                and! quantity = if quantity <= 0 then Error (OrderError.QuantityNotPositive quantity) else Ok quantity

                return sku, quantity
            }

        return! checkStock sku quantity |> Validation.ofResult
    }

placeOrder "" 0          // Error [ SkuEmpty; QuantityNotPositive 0 ] — stock never checked
placeOrder "sold-out" 1  // Error [ OutOfStock "sold-out" ]
```

## Realistic — `asyncResult {}` workflow

Mirrors the library's login test: lift `Async<'T option>`, `bool`, `AsyncResult` and `Result` into one workflow, mapping each inner error into the workflow's error type.

```fs
open Feather.ErrorHandling
open Feather.ErrorHandling.AsyncResult.Operators

type User = { Username: string; Password: string }
type Token = Token of string

type AuthError = AuthError of string
type TokenError = TokenError of string

[<RequireQualifiedAccess>]
type LoginError =
    | InvalidUser
    | InvalidPassword
    | Unauthorized of AuthError
    | TokenFailed of TokenError

let login
    (tryGetUser: string -> Async<User option>)
    (authorize: User -> AsyncResult<unit, AuthError>)
    (createToken: User -> Result<Token, TokenError>)
    (username: string, password: string): AsyncResult<Token, LoginError> =

    asyncResult {
        let! user = username |> tryGetUser |> AsyncResult.ofAsyncOption LoginError.InvalidUser
        do! user.Password = password |> AsyncResult.ofBool LoginError.InvalidPassword
        do! user |> authorize <@> LoginError.Unauthorized

        return! user |> createToken |> Result.mapError LoginError.TokenFailed
    }
```

## Integration — Task-based .NET APIs

Lift every `Task` at the boundary with a `*Catch` variant so exceptions become a typed error. A faulted `Task` arrives in the handler as an `AggregateException`; unwrap it when the inner exception matters.

```fs
open System
open System.IO
open System.Threading.Tasks
open Feather.ErrorHandling

[<RequireQualifiedAccess>]
type StorageError =
    | ReadFailed of path: string * exn
    | WriteFailed of path: string * exn

let private unwrap: exn -> exn = function
    | :? AggregateException as e when not (isNull e.InnerException) -> e.InnerException
    | e -> e

let readText (path: string): AsyncResult<string, StorageError> =
    File.ReadAllTextAsync path
    |> AsyncResult.ofTaskCatch (fun e -> StorageError.ReadFailed (path, unwrap e))

let writeText (path: string) (content: string): AsyncResult<unit, StorageError> =
    File.WriteAllTextAsync(path, content)
    |> AsyncResult.ofEmptyTaskCatch (fun e -> StorageError.WriteFailed (path, unwrap e))

let copyUpper (source: string) (target: string): AsyncResult<unit, StorageError> =
    asyncResult {
        let! (content: string) = readText source
        do! content.ToUpperInvariant() |> writeText target
    }
```

## Kleisli composition with operators

```fs
open Feather.ErrorHandling
open Feather.ErrorHandling.Result.Operators

[<RequireQualifiedAccess>]
type PipelineError =
    | Empty
    | TooLong of int

let notEmpty (s: string) = if s = "" then Error PipelineError.Empty else Ok s
let maxLength limit (s: string) = if s.Length > limit then Error (PipelineError.TooLong s.Length) else Ok s

let toLower (s: string) = s.ToLowerInvariant()

let validateSlug: string -> Result<string, string> =
    notEmpty
    >=> maxLength 20
    >!> toLower
    >@> sprintf "%A"

let slugLength = Ok "Hello" >>= notEmpty <!> String.length   // Ok 5
```

## Collections of results

```fs
open Feather.ErrorHandling

let parse (raw: string): Result<int, string> =
    match System.Int32.TryParse raw with
    | true, value -> Ok value
    | _ -> Error $"not a number: {raw}"

let inputs = [ "1"; "x"; "3"; "y" ]

inputs |> List.map parse |> Result.sequence       // Error "not a number: x"
inputs |> List.map parse |> Validation.ofResults  // Error [ "not a number: x"; "not a number: y" ]
inputs |> List.map parse |> Result.list           // [ 1; 3 ]
```

## Parallel asynchronous work

`ofParallelAsyncResults` runs every computation and accumulates every `Error`; an exception thrown by any of them becomes the single error from the handler.

```fs
open Feather.ErrorHandling

let fetch (id: int): AsyncResult<string, string> =
    asyncResult {
        do! AsyncResult.sleep 10
        if id % 2 = 0 then return! Error $"item {id} unavailable"
        return $"item {id}"
    }

let all: AsyncResult<string list, string list> =
    [ 1 .. 5 ]
    |> List.map fetch
    |> AsyncResult.ofParallelAsyncResults (fun e -> e.Message)
// Error [ "item 2 unavailable"; "item 4 unavailable" ]

let throttled: AsyncResult<string list, string list> =
    [ 1; 3; 5 ]
    |> List.map fetch
    |> AsyncResult.ofMaxParallelAsyncResults 2 (fun e -> e.Message)
// Ok [ "item 1"; "item 3"; "item 5" ]
```

## Retry and timeout

`retryWith log waitMs attempts` runs the computation once and then up to `attempts` more times while it returns `Error`. The computation must be cold (re-executed on each run).

```fs
open Feather.ErrorHandling

let mutable calls = 0

let flaky: AsyncResult<string, string> =
    asyncResult {
        calls <- calls + 1
        if calls < 3 then return! Error $"attempt {calls} failed"
        return "done"
    }

let withRetry =
    flaky
    |> AsyncResult.retryWith (printfn "%s") 100 5
// prints "Retrying [1/5] in 100ms ..." and "Retrying [2/5] in 100ms ...", then Ok "done"

let bounded: Async<Result<string, string>> =
    flaky
    |> Async.withTimeout 500 (Error "timed out")
```

## Test — asserting on workflows with Expecto

```fs
module Example.WorkflowTest

open Expecto
open Feather.ErrorHandling

let private run (workflow: AsyncResult<'T, 'E>) = workflow |> Async.RunSynchronously

[<Tests>]
let tests =
    testList "Workflows" [
        testCase "should continue with the bound value when the Result is Ok" <| fun _ ->
            let actual =
                asyncResult {
                    let! quantity = Ok 3
                    return quantity * 2
                }
                |> run

            Expect.equal actual (Ok 6) "Should double the bound value"

        testCase "should accumulate every failure when several fields are invalid" <| fun _ ->
            let actual: Validation<int * int, string> =
                validation {
                    let! a = Error "a"
                    and! b = Error "b"
                    return a, b
                }

            Expect.equal actual (Error [ "a"; "b" ]) "Should keep failures in binding order"
    ]
```
