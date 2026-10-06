# Anti-Patterns

Format: **mistake → why it is wrong → fix**.

## Misleading Names and Doc Comments

- **Expecting `AsyncResult.retryWithExponential` to back off exponentially** → it waits `currentAttempt * baseWaitMs` (linear: 10, 20, 30 ms …); the source comment on `RetryPolicy` says exponential backoff was rejected on purpose → use it for linear backoff and size `baseWaitMs` / `attempts` with that in mind.

- **Trusting the "Run … in Parallel" doc comment on `AsyncResult.ofSequentialAsyncResults` / `ofSequentialAsyncs`** → both use `Async.Sequential` and run one computation at a time → use `ofParallelAsyncResults` / `ofMaxParallelAsyncResults` when you want concurrency.

- **Expecting `AsyncResult.sequenceM` to stop executing after the first `Error`** → it returns only the first error, but every computation in the list still runs, in order → if later computations must not run after a failure, bind them one by one in `asyncResult {}`.

- **Writing `AsyncResult.retry waitMs attempts` and expecting `attempts` total runs** → the computation runs once, then up to `attempts` more times (`retry 100 5` can run 6 times) → pass the number of *retries*, not total attempts.

## Computation Expression Pitfalls

- **Sequential `let!` in `validation {}`** → the builder has no `Bind`; two `let!` without `and!` does not compile (FS0708) → join independent checks with `and!`; sequence dependent stages by binding each `validation {}` in an outer `result {}`. See `examples.md` → Validation stages sequenced with `result {}`.

- **Using `do!`, loops, `try` or `if` without `else` inside `validation {}`** → the builder defines only `Return`, `ReturnFrom`, `BindReturn`, `MergeSources` and `Source`, so these do not compile → move the logic into a `result {}` or a function returning `Result` / `Validation`, then bind it.

- **Expecting an inner `result {}` bound with `and!` to report several failures** → `result {}` stops at its first error, so it contributes one failure → split independent checks into separate `and!` bindings.

- **Binding `Async<'T>` or `Task<'T>` inside `asyncResult {}` whose error type is a domain DU** → those overloads fix the error type to `exn`, so the CE fails to type-check → lift first: `let! x = work () |> AsyncResult.ofAsyncCatch MyError.Failed` (or `ofTaskCatch` / `ofEmptyTaskCatch`).

- **Calling a method or property on a value bound from an `AsyncResult` in `asyncResult {}` without a type annotation** (e.g. `let! content = readText path` then `content.ToUpperInvariant()`) → the `AsyncResult` and `Async<'T>` `Bind` overloads both match, so the type is unknown at the member lookup (FS0072) → annotate the binding: `let! (content: string) = readText path`. Record field access and passing the value to a typed function are unaffected.

## Exceptions and Interop

- **Using `AsyncResult.ofTask` / `ofAsync` / `ofEmptyTask` for code that can throw** → these do not catch; the exception escapes when the workflow runs → use `ofTaskCatch` / `ofAsyncCatch` / `ofEmptyTaskCatch` with an `exn -> 'Error` mapper.

- **Matching the exception type directly in an `ofTaskCatch` handler** → a faulted task reaches the handler as `AggregateException`, so `:? IOException` never matches → unwrap `InnerException` first. See `examples.md` → Integration.

- **Relying on `AsyncResult.ofAsyncOption` to surface exceptions** → an exception in the `Async<'T option>` is mapped to the same `onMissing` error as `None` → if the two must differ, lift with `ofAsyncCatch` and then `Result.ofOption` / `AsyncResult.ofOption` separately.

- **Relying on `Async.withTimeout` only for timeouts** → it returns `notInTimeValue` for *any* exception from the computation, not just the timeout → catch domain exceptions inside the computation (e.g. `AsyncResult.catch`) before applying the timeout.

- **Retrying a computation built from an already-started `Task`** → `retry` re-runs the same `Async`, which re-awaits the same completed task and gets the same result → build the computation lazily, e.g. `asyncResult { return! createTask () |> AsyncResult.ofTaskCatch f }`.

- **`Result.orFail` in request handlers, consumers or pure logic** → it throws `failwithf "Error %A"` and loses the typed error → propagate the `Result`; reserve `orFail` for startup/configuration code and tests.

## Operators

- **Writing `f <!> xR <*> yR` (Haskell style)** → in `Result.Operators` both are flipped: `r <!> f` is `Result.map f r` and `xR <*> fR` is `Result.apply fR xR`; applicative-style construction does not type-check → use `result {}` / `validation {}`, or `Result.apply` / `Validation.apply` directly.

- **Opening `Result.Operators`, `AsyncResult.Operators` and `Option.Operators` in one file** → they define the same symbols and the last `open` shadows the others; `<@>` is `mapError` for `Result` / `AsyncResult` but `mapNone` for `Option` → open only one operator module per scope, or use the named functions.

## Silently Dropped Errors

- **Using `Result.list` / `Result.listCollect` when errors matter** → they keep successes and discard every `Error` → use `Result.sequence` (first error) or `Validation.ofResults` (all errors), or log with `Result.teeError` before filtering.

- **Using `Result.sequence` to report all invalid inputs** → it is monadic and returns only the first error → use `Validation.sequence` / `Validation.ofResults`.

- **Ignoring a returned `Result` with `|> ignore`** → the failure disappears → bind it with `do!` / `let!`, or handle it with `Result.iter` / `Result.teeError` explicitly.
