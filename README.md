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
