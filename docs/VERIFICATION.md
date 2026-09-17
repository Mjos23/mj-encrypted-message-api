# Verification before the first GitHub push

Date: September 17, 2026. Environment: Linux x64, .NET SDK 10.0.401,
ASP.NET Core runtime 10.0.12.

| Check | Result |
| --- | --- |
| Restore with committed dependency locks | Passed |
| Release build | Passed; zero warnings and zero errors |
| xUnit integration suite | 42 passed, zero failed, zero skipped |
| README key-generation command | Passed; generated a 32-byte random key |
| README C# client, valid request | HTTP success and `accepted`; exit code 0 |
| README C# client, tampered tag | HTTP rejection and `rejected`; exit code 1 |
| Whitespace formatting check | Passed |
| Workflow YAML and configured matrix | Parsed; three operating systems, read-only token |
| Full project-aware `dotnet format` | Not completed locally; blocked by the environment's named-pipe restriction |
| GitHub Actions execution | Pending repository publication |
| Windows and macOS execution | Pending the corresponding GitHub CI jobs |
| Public deployment | Not performed |

## Commands exercised

```sh
dotnet restore --locked-mode -m:1
dotnet build --configuration Release --no-restore -m:1
dotnet test --configuration Release --no-build -m:1 --logger "trx;LogFileName=tests.trx"
```

`-m:1` limits MSBuild to one worker. This environment rejected the default
multi-process build, so local checks used the sequential option. It does not
change the source, test cases, or assertions. The GitHub workflow retains the
normal build commands.

The full formatter could not connect to its build-host named pipe and reported
`SocketException (13): Permission denied`. A folder-based whitespace check
completed successfully on all five C# source files:

```sh
dotnet format whitespace . --folder --include src/MessageApi/Program.cs src/MessageClient/Program.cs tests/MessageApi.Tests/ApiFactory.cs tests/MessageApi.Tests/MessageEndpointTests.cs tests/MessageApi.Tests/StartupTests.cs --verify-no-changes
```

This narrower check is not a substitute for the full formatter. The full
`dotnet format --verify-no-changes --no-restore` gate remains in GitHub CI.
No successful remote CI status is claimed before that workflow runs.

## Scope of the evidence

The 42 cases belong to the checked-in xUnit suite. Tests host the real API over
Kestrel and include an independent Python-generated AES-GCM vector. Separate
checks exercised the real CLI sender using the documented project commands.
Temporary servers were stopped and generated keys were not written into the
project.

Replay acceptance is one explicitly documented test outcome. Passing this
suite does not add replay protection or establish a production security audit.
