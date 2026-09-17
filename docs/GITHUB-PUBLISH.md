# GitHub repository

Repository: [Mjos23/upgraded-octo-fortnight](https://github.com/Mjos23/upgraded-octo-fortnight).

Project: **Bangel C# Encrypted Message API**.

The repository was created privately by its owner. Uploading this project does
not change that visibility or deploy a live API. No license has been selected
or added.

## Work with the source

After the initial source upload:

```sh
git clone https://github.com/Mjos23/upgraded-octo-fortnight.git
cd upgraded-octo-fortnight
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
Private repositories cannot be inspected by hiring teams without access.
Any later visibility or licensing decision remains with the owner.

Suggested repository description:

> Bangel C# / ASP.NET Core API for AES-256-GCM message validation, with a CLI
> client, HTTP integration tests, and GitHub Actions.

Suggested topics: `csharp`, `dotnet`, `aspnet-core`, `rest-api`, `aes-gcm`,
`integration-testing`, `portfolio`.
