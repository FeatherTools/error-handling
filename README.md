# <img src="https://github.com/FeatherTools/.github/blob/main/profile/feather-logo-200.png" alt="FeatherTools Logo" width="100" height="100"> Error-Handling

> Library for proper error handling with Option, Result, AsyncResult and their computation expressions.

## Inspiration
- Started as a copy of https://github.com/swlaschin/DomainModelingMadeFunctional/blob/master/src/OrderTaking/Result.fs
- Similar project is https://github.com/fsprojects/Chessie
- Also Inspired by [Suave/YoLo](https://github.com/SuaveIO/suave/blob/master/src/Suave/Utils/YoLo.fs)

## Install
*todo*

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
