---
name: feather-error-handling
description: Use whenever generating or reviewing F# code that handles errors with the Feather.ErrorHandling library — the `result {}`, `asyncResult {}`, `validation {}` (`let! ... and!`) or `maybe {}` computation expressions, the `AsyncResult<'T, 'E>` / `Validation<'T, 'F>` types, lifting Tasks and Asyncs with `AsyncResult.ofTaskCatch` / `ofEmptyTaskCatch` / `ofAsyncCatch` / `ofAsyncOption` / `ofBool`, combining lists via `Result.sequence`, `Validation.ofResults`, `AsyncResult.sequenceM` / `sequenceA` / `ofParallelAsyncResults`, side effects via `tee` / `teeError`, `AsyncResult.retry` / `retryWith`, `Async.withTimeout`, `Result.orFail`, or the `Result.Operators` / `AsyncResult.Operators` / `Option.Operators` symbols (`>>=`, `<@>`, `>=>`, `<?=>`). Trigger also on mentions of FeatherTools/error-handling, railway-oriented programming, or accumulating validation errors in F#.
---

# Feather-Error-Handling

Library: [FeatherTools/error-handling](https://github.com/FeatherTools/error-handling)
NuGet: `Feather.ErrorHandling`

## Purpose

`Feather.ErrorHandling` extends F#'s `Option`, `Result` and `Async` with the functions and computation expressions needed for railway-oriented error handling: fail-fast `result {}`, applicative `validation {}` that accumulates failures, `asyncResult {}` for asynchronous workflows with typed errors, plus lifting from `Task`/`Async`/`Option`, list sequencing, parallel execution, retries and operator modules. It ships its sources for Fable.

## When to Use

- Writing domain logic where failures are values (`Result<'T, 'Error>`) instead of exceptions.
- Collecting every validation failure of a form, command or config record in one pass.
- Composing asynchronous steps that can fail, including .NET `Task` APIs whose exceptions must become typed errors.
- Running a list of fallible async operations in parallel, throttled or in sequence, with all errors collected.
- Retrying transient failures or bounding an async computation with a timeout.

## When NOT to Use

- The project already standardises on FsToolkit.ErrorHandling — the CE names (`result`, `asyncResult`, `validation`) collide.
- You need `task {}`-based workflows with typed errors — this library is `Async`-based; `Task` is only an input to lift.
- Exponential backoff, jitter or circuit breaking — `retry*` provide constant or linear waits only.

## Main Concepts

- **`result {}`** — fail-fast CE over `Result`; supports `let!`, `do!`, `return!`, `if … then return! Error e`, `for`, `while`, `try/with`, `use`.
- **`Validation<'T, 'F>`** — abbreviation for `Result<'T, 'F list>`; `Validation.apply` / `sequence` / `zip` accumulate failures; `ofResult`, `ofResults`, `toResult` convert.
- **`validation {}`** — applicative CE: `let! … and! …` evaluates every binding and concatenates failures; binds both `Result` and `Validation`; no sequential `let!`.
- **`AsyncResult<'T, 'E>`** — abbreviation for `Async<Result<'T, 'E>>`; the `AsyncResult` module covers map/bind/tee, lifting (`ofSuccess`, `ofError`, `ofResult`, `ofAsync*`, `ofTask*`, `ofEmptyTask*`, `ofOption`, `ofAsyncOption`, `ofBool`), sequencing and parallel helpers.
- **`asyncResult {}`** — CE over `AsyncResult`; also binds `Result`, and binds `Async` / `Task` with error type `exn`.
- **`maybe {}`** — CE over `Option`.
- **`Result` / `Option` / `Async` modules** — additions to the FSharp.Core modules: `tee`, `teeError`, `bindError`, `ofOption`, `sequence`, `list`, `orFail`; `bindNone`, `mapNone`, `orDefault`, `ofNullable`; `Async.map`, `apply`, `withTimeout`.
- **Operator modules** — `Result.Operators`, `AsyncResult.Operators`, `Option.Operators`; opt-in, value-on-the-left (`r >>= f`, `r <@> f`), Kleisli `>=>`.
- **Retry** — `AsyncResult.retry`, `retryWith`, `retryWithExponential` (linear backoff).

## Related Libraries

- **FSharp.Core** — the only dependency; the library extends its `Result`, `Option` and `Async` modules.

## Keywords for Search

Feather.ErrorHandling, FeatherTools, error-handling, railway-oriented programming, Result, result CE, Validation, validation CE, and!, applicative, AsyncResult, asyncResult CE, maybe CE, Option, Async, tee, teeError, bindError, ofOption, sequence, sequenceM, sequenceA, Validation.ofResults, Validation.zip, orFail, ofTaskCatch, ofEmptyTaskCatch, ofAsyncCatch, ofAsyncOption, ofBool, ofParallelAsyncResults, ofMaxParallelAsyncResults, ofSequentialAsyncResults, retry, retryWith, retryWithExponential, withTimeout, Operators, >>=, >=>, <@>, <!>, <?=>, <??>, <?!>, Kleisli, Fable, F#

## Reference Files

- For composition principles, recommended API usage, error mapping and testing guidance, read `references/preferred-patterns.md`.
- For CE pitfalls, misleading names, exception leaks and operator gotchas, read `references/anti-patterns.md`.
- For worked, self-contained code examples ordered by increasing complexity, read `references/examples.md`.
