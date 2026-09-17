# GitHub repository

Repository: [Mjos23/mj-encrypted-message-api](https://github.com/Mjos23/mj-encrypted-message-api).

Project: **Bangel C# Encrypted Message API**.

The owner has made this repository public. Reviewers can inspect the source,
documentation, and GitHub Actions results. No live API is deployed, and no
license has been selected or added.

## Work with the source

Clone the public repository and run the checks:

```sh
git clone https://github.com/Mjos23/mj-encrypted-message-api.git
cd mj-encrypted-message-api
dotnet restore --locked-mode
dotnet build --configuration Release --no-restore
dotnet format --verify-no-changes --no-restore
dotnet test --configuration Release --no-build
```

Use the README for the accepted/rejected client demo and local key setup.
Generated keys, credentials, build outputs, and test results do not belong in
commits.

## Review GitHub checks

Open the repository's Actions tab and inspect the build-and-test run for the
latest commit. All three configured platforms and the full formatting check
must pass before describing GitHub CI as passing. Local verification and any
remaining limitations are recorded in `docs/VERIFICATION.md`.

## Prepare for applications

Use `docs/PORTFOLIO.md` to practice the demo and explain the design decisions.
The public repository link can be included in applications and shared with
hiring teams.

Suggested repository description:

> Bangel C# / ASP.NET Core API for AES-256-GCM message validation, with a CLI
> client, HTTP integration tests, and GitHub Actions.

Suggested topics: `csharp`, `dotnet`, `aspnet-core`, `rest-api`, `aes-gcm`,
`integration-testing`, `portfolio`.
