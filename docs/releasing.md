# Releasing

Publishing uses **NuGet trusted publishing**. The pack job asks GitHub for an OIDC token, exchanges that token at
nuget.org for an API key that lives for one hour, and pushes with it. Nothing long-lived is stored in the
repository, so there is no key to rotate or leak.

Reference: <https://learn.microsoft.com/nuget/nuget-org/trusted-publishing>

## One-time setup

### 1. Add a trusted publishing policy on nuget.org

Sign in, open the account menu and choose **Trusted Publishing**, then add a policy for this repository:

| Field | Value |
| --- | --- |
| Repository owner | `xiSage` |
| Repository | `GodotConfigFile` |
| Workflow file | `ci-cd.yml` — the file name only, without the `.github/workflows/` path |
| Environment | leave empty (see the note on environments below) |

Matching is case-insensitive. On a **private** repository the policy starts out only *temporarily* active for seven
days: nuget.org needs the repository and owner ids that arrive with the first OIDC token, and the policy becomes
permanent once a publish has succeeded. If the window lapses, restart it in the UI; nothing else changes.

### 2. Add the `NUGET_USER` repository secret

Its value is the nuget.org **profile name** the package belongs to — not the email address. The workflow passes it
to `NuGet/login@v1` as `user`.

### 3. Optional: gate the publish on a human approval

Create a GitHub environment named `release` with the required reviewers you want, then:

- add `environment: release` to the `pack` job in `.github/workflows/ci-cd.yml`, and
- set the policy's **Environment** field to `release`, so the two have to agree.

## Cutting a release

1. Set `<Version>` in `Directory.Build.props` (it is the single source of truth for the package version).
2. Commit, then tag and push:

   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

3. The pipeline runs `test`, then `aot-check`, then `pack`. Only a `v*` tag makes the last job log in and push;
   branch pushes and pull requests stop after uploading the package as an artifact.

The temporary API key is requested immediately before the push, because it expires after an hour and one OIDC token
buys exactly one key.
