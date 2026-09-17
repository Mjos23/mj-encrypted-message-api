# Bangel C# Encrypted Message API

Repository: [Mjos23/upgraded-octo-fortnight](https://github.com/Mjos23/upgraded-octo-fortnight).

A C# / ASP.NET Core API that validates an AES-256-GCM encrypted message and
returns **accepted** or **rejected**. One route, a matching command-line client,
and repeatable tests against a real HTTP server.

```text
POST /message
Valid authentication tag:   200 {"status":"accepted"}
Invalid message request:   400 {"status":"rejected"}
```

The project demonstrates API design, authenticated encryption, input validation,
configuration outside source code, integration testing, and CI configuration.
The API and client use .NET's built-in libraries; only the test project adds
NuGet packages.

## Run the demo

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0),
version 10.0.401 or newer in the .NET 10 series. Open a terminal in this folder.

```sh
dotnet restore --locked-mode
dotnet build --configuration Release --no-restore
dotnet run --project src/MessageClient --configuration Release --no-build -- --new-key
```

The last command prints a random Base64 key. Use that value in both terminals
below. It is a local secret; do not put it in a Git commit or an HTTP request.

**Terminal 1 — start the API (PowerShell):**

```powershell
$env:MESSAGE_API_KEY_BASE64 = "PASTE_YOUR_GENERATED_BASE64_KEY"
dotnet run --project src/MessageApi --configuration Release --no-build
```

Wait for `Now listening on: http://127.0.0.1:8000`.

**Terminal 2 — send a message (PowerShell):**

```powershell
$env:MESSAGE_API_KEY_BASE64 = "PASTE_THE_SAME_BASE64_KEY"
dotnet run --project src/MessageClient --configuration Release --no-build -- "Hello Mike"
```

Expected:

```json
{"status":"accepted"}
```

Change one bit in the authentication tag:

```sh
dotnet run --project src/MessageClient --configuration Release --no-build -- "Hello Mike" --tamper
```

Expected:

```json
{"status":"rejected"}
```

The client returns exit code `0` for accepted and `1` for rejected.
On Bash or Zsh, replace the PowerShell assignment with
`export MESSAGE_API_KEY_BASE64="YOUR_GENERATED_BASE64_KEY"`.
Stop the API with Ctrl+C in terminal 1.

## Endpoint contract

**Default address:** `POST http://127.0.0.1:8000/message`

**Content type:** `application/json`

Send exactly three Base64 string fields:

```json
{
  "nonce": "BASE64_OF_12_BYTES",
  "ciphertext": "BASE64_OF_ENCRYPTED_MESSAGE",
  "tag": "BASE64_OF_16_BYTE_TAG"
}
```

These are schema placeholders. The included client creates a valid encrypted
request; plaintext JSON such as `{"message":"Hello"}` is rejected.

| Input | Contract |
| --- | --- |
| Algorithm | AES-256-GCM |
| Shared key | 32 random bytes, configured as `MESSAGE_API_KEY_BASE64` |
| Nonce | 12 bytes; a fresh value for each encryption under the same key |
| Ciphertext | 1–65,536 bytes |
| Authentication tag | 16 bytes |
| Associated data | Exact UTF-8 bytes `message-api:v1:POST:/message` |
| HTTP body | At most 98,304 bytes |

`accepted` means the ciphertext, nonce, tag, and associated data authenticate
under the server's configured key. The endpoint does not return the plaintext.
Malformed JSON, invalid Base64, wrong-sized values, wrong keys, changed data,
and missing, duplicate, or extra fields are rejected. A different HTTP method
receives the framework's normal method-not-allowed response.

## Design choices

- **Authenticated encryption:** .NET's `AesGcm` checks integrity while decrypting.
  The code uses a standard cryptographic implementation.
- **Fixed protocol context:** associated data ties each envelope to the version,
  HTTP method, and route agreed by the client and server.
- **Simple response contract:** request-validation failures receive the same
  rejection body, without a detailed cryptographic failure reason.
- **Bounded input:** field sizes, JSON depth, and HTTP body size are constrained
  before accepting a message.
- **Explicit key setup:** startup fails when the key is absent or malformed.
  Plaintext buffers are cleared after validation; key buffers are cleared when
  the server shuts down.
- **One route:** the scope stays small enough to inspect and demonstrate.

## Test and verify

```sh
dotnet restore --locked-mode
dotnet build --configuration Release --no-restore
dotnet format --verify-no-changes --no-restore
dotnet test --configuration Release --no-build
```

Tests run through `WebApplicationFactory` using Kestrel on a dynamically
assigned loopback port. They exercise HTTP parsing, cryptographic verification,
response bodies, and request-size enforcement together. A fixed envelope
generated independently with Python's `cryptography` library checks
interoperability. The fixture key is public test data, not a server secret.

The suite covers valid and Unicode messages, one-bit changes to each encrypted
field, wrong keys and associated data, schema errors, size boundaries,
oversized chunked requests, and invalid startup configuration. It also records
the intentional current behavior that a replayed valid message is accepted.

[GitHub Actions](.github/workflows/ci.yml) is configured to restore locked
dependencies, build, check formatting, and test on Linux, Windows, and macOS.
The workflow runs on pushes and pull requests. It does not deploy a service.
See [verification notes](docs/VERIFICATION.md) for the checks actually run before
publication; a configured workflow is not evidence of a successful GitHub run.

## Code map

| Path | Purpose |
| --- | --- |
| `src/MessageApi/Program.cs` | Startup, key loading, and the message handler |
| `src/MessageClient/Program.cs` | Key generation, encryption, and demo requests |
| `tests/MessageApi.Tests/` | HTTP and startup integration tests |
| `.github/workflows/ci.yml` | Build, formatting, and test jobs |
| `docs/SECURITY-MODEL.md` | Trust assumptions and limits |

## Current limits

This authenticates a message under a shared key. It does not establish an
individual user's identity, validate the meaning of the plaintext, or grant
permission to perform a business action.

**Replay protection is not implemented:** an unchanged valid envelope can be
accepted again. There is no key rotation service, user database, rate limiter,
or persistent audit log. These are separate extensions, not completed features.

The default server binds to loopback and has no public deployment. A remote
deployment needs an appropriate HTTPS configuration. See the
[security model](docs/SECURITY-MODEL.md) before extending its purpose.

## References

- [Microsoft: AES-GCM authenticated decryption](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm.decrypt?view=net-10.0)
- [Microsoft: ASP.NET Core integration tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)
- [Microsoft: Kestrel-backed test hosting](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1.usekestrel?view=aspnetcore-10.0)
