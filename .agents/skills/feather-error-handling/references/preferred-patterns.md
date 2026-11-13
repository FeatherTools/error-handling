# Preferred Patterns

## Core Principles

- Model expected failures as `Result<'T, 'Error>` with a domain error DU, not as exceptions. Exceptions enter the error track only at the boundary, through a `*Catch` lift or `AsyncResult.catch`.
- Pick the shape by failure semantics: `Result` / `result {}` stops at the first error; `Validation` / `validation {}` collects every error; `AsyncResult` / `asyncResult {}` is `Result` inside `Async`.
- `Validation<'T, 'F>` and `AsyncResult<'T, 'E>` are type abbreviations (`Result<'T, 'F list>` and `Async<Result<'T, 'E>>`), so standard `Result` / `Async` functions apply to them unchanged.
- Prefer `Async` over `Task`. Lift a `Task` into `AsyncResult` as soon as it is created and keep `Task` out of your own signatures.

## Recommended API Usage

- Sequential, fail-fast logic: `result {}` with `let!` / `do!` / `return!`; `if cond then return! Error e` works as an early exit. See `examples.md` → Basic.
- Convert at the edges: `Result.ofOption error`, `Result.ofChoice` / `toChoice`, `Result.toOption`, `Option.ofNullable` / `toNullable`, `AsyncResult.ofBool error`.
- Side effects mid-pipeline: `Result.tee` / `teeError`, `AsyncResult.tee` / `teeError`, `Option.tee` / `teeNone`, `Async.tee`. See `examples.md` → Side effects without breaking the chain.
- Error recovery: `Result.bindError` / `AsyncResult.bindError` (error → new `Result`), `Option.bindNone` / `mapNone` / `orDefault` (all take a thunk).
- Collect independent field checks: `validation { let! a = ... and! b = ... return ... }`. Both `Result` and `Validation` values can be bound; a `Result` failure is lifted to a one-item list. See `examples.md` → Validation — accumulating failures.
- Staged validation: bind each `validation {}` stage inside an outer `result {}`. See `examples.md` → Validation stages sequenced with `result {}`.
- Async workflows: `asyncResult {}` binds `AsyncResult`, `Result` (and so `Validation`), and — only when the error type is `exn` — `Async` and `Task`. See `examples.md` → Realistic.
- Task / Async interop: `AsyncResult.ofTaskCatch`, `ofEmptyTaskCatch` (non-generic `Task`), `ofAsyncCatch`, each taking an `exn -> 'Error` mapper. See `examples.md` → Integration.
- Lists: `Result.sequence` (first error), `Validation.sequence` / `Validation.ofResults` (all errors), `Result.sequenceConcat`, `Result.list` / `listCollect` (keep successes only). See `examples.md` → Collections of results.
- Concurrent async work: `AsyncResult.ofParallelAsyncResults`, `ofMaxParallelAsyncResults maxParallel`, `ofSequentialAsyncResults`, and the `*Asyncs` variants for plain `Async<'T>`. See `examples.md` → Parallel asynchronous work.
- Transient failures: `AsyncResult.retry waitMs attempts`, `retryWith log waitMs attempts`, `retryWithExponential log baseWaitMs attempts`; time limits with `Async.withTimeout ms fallback`. See `examples.md` → Retry and timeout.

## Error Handling

- One error DU per operation; wrap inner errors in a case of the outer DU and lift with `Result.mapError` / `AsyncResult.mapError` (or `<@>`) at the bind site: `do! authorize user <@> LoginError.Unauthorized`.
- Exception handlers passed to `ofTaskCatch` / `ofEmptyTaskCatch` receive the `AggregateException` from a faulted task; unwrap `InnerException` when the cause matters. `ofAsyncCatch` receives the original exception.
- `Result.orFail` throws (`failwithf "Error %A"`). Use it only where no caller can receive an error: startup, configuration, tests.
- Inside `result {}` and `asyncResult {}`, `try ... with e -> return! Error ...` turns an exception from that block into an error.

## Composition

- Kleisli `>=>` composes `'a -> Result<'b, 'e>` functions; `>!>` maps the success, `>@>` maps the error, `>@*>` / `>@@>` tee. `>->` composes error handlers. See `examples.md` → Kleisli composition with operators.
- Operator modules are opt-in: `open Feather.ErrorHandling.Result.Operators`, `AsyncResult.Operators`, or `Option.Operators`. Open only the one the file works in; the same symbols mean different things in each.
- In the operator modules the value is on the left and the function on the right: `r >>= f`, `r <!> f`, `r <@> f`, and `rValue <*> rFunction`.
- `Option.Operators` adds `opt <?=> default`, `opt <??> otherOpt`, `opt <?!> error` (to `Result`), and `opt |>! action` (`Option.iter`).
- `Async.apply` starts the function computation as a child and runs both in parallel; `Async.map` / `bind` / `retn` / `tee` cover plain `Async`.

## Integration with Other Libraries

- The package ships its `.fs` sources under `fable/`, so it can be referenced from Fable projects.
- The CEs (`result`, `asyncResult`, `validation`, `maybe`) are `[<AutoOpen>]`; `open Feather.ErrorHandling` brings them in. Avoid opening another error-handling library that defines builders with the same names (e.g. FsToolkit.ErrorHandling) in the same file.
- The `Result`, `Option`, `Async` and `AsyncResult` modules are `[<RequireQualifiedAccess>]` and extend the FSharp.Core modules of the same name: `Result.map` and `Result.tee` resolve side by side.

## Naming Conventions

- `of*` lifts into the type (`ofOption`, `ofTask`, `ofResult`); `to*` converts out (`toOption`, `toChoice`, `toNullable`).
- `*Catch` variants turn exceptions into errors; the plain variant (`ofTask`, `ofAsync`, `ofEmptyTask`) lets them propagate.
- `sequenceM` is monadic (first error, any error type); `sequenceA` is applicative (all errors, error type must be a list).
- `retn` lifts a value (`Async.retn`, `Option.retn`, `AsyncResult.retn` = `ofSuccess`).

## Testing Recommendations

- Assert on the full `Result` value (`Expect.equal actual (Error [ ... ]) ...`) so the error case and its payload are both checked. See `examples.md` → Test.
- Run `AsyncResult` in tests with `Async.RunSynchronously`; keep it out of production code paths.
- For `validation {}`, assert the order of accumulated failures — it follows binding order.
- Test retries with a counter captured by a cold `asyncResult {}` and a small `waitMs`, as the library's own retry tests do.
