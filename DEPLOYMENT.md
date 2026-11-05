# Automatic delivery

Completed tasks on `main` are validated, committed and pushed with
`./ci/Complete-Task.ps1 -Message "Describe the finished change"`.
Use `-StagedOnly` when other unfinished changes share the workspace.

The `Signed Windows release` GitHub Actions workflow then:

1. Builds and tests the Windows application, UI, update protocol, extension,
   server and deployment safeguards on `windows-2022`.
2. Chooses a version newer than the production release, updates the runner's
   assembly attributes, builds and signs using `UPDATE_SIGNING_KEY_XML` from
   the `production` environment, and validates the actual signed binary.
3. Uploads only the signed installer/update/extension, manifest, source archive
   and source provenance as a deployment artifact.
4. Authenticates to the production deployment endpoint using a short-lived
   GitHub OIDC token. No SSH deployment private key is required.
5. Verifies the live API, publisher signature and downloaded executable.

Pull requests run tests without release secrets or deployment access. The
production environment accepts only `main`. Signing material is read from the
environment in memory, never written to source, artifact files or key files.
The publisher public key remains `Assets/UpdatePublicKey.xml`.

## Server bootstrap and ownership

The local-only gateway listens on `127.0.0.1:4081`, behind the exact Nginx route
`/api/ci/deploy`. It validates GitHub's RS256 signature, issuer, audience,
immutable repository and owner IDs, workflow, main branch, production environment,
event and expiry. An unprivileged `asan-ci` account can invoke only the root-owned
release receiver through an exact sudo rule. No uploaded script is executed.

Root-owned installed files:

- `/usr/local/libexec/asan-download-ci/deploy.py`
- `/usr/local/libexec/asan-download-ci/oidc_gateway.py`
- `/usr/local/libexec/asan-download-deploy.py`
- `/etc/asan-download-ci.json`
- `/etc/asan-download-publisher.xml` (public key only)
- `/etc/systemd/system/asan-download-ci.service`
- `/etc/sudoers.d/asan-download-ci`

Only the existing public release manifest and three stable download artifacts
are replaced. Versioned artifacts and source provenance are also saved under
`downloads/<version>/`. Previous stable files and deployment records are retained
under `/srv/downloader/asan-download/releases/cicd/`.

The receiver rejects unexpected archive paths, links, duplicate files, oversized
uploads, stale commits, invalid signatures, mismatching installer contents and
same-version/downgrade attempts. It publishes the manifest last and restores all
previous stable files if the local release API or homepage health check fails.

The application server, website content, other products and runtime data are
preserved. Updating the root-owned deployment infrastructure is a separate
maintenance operation, not something uploaded release code can perform.

## Recovery

Inspect `systemctl status asan-download-ci` and
`journalctl -u asan-download-ci`. A failed build never invokes deployment; a
rejected package leaves the live channel untouched. Failed health checks roll
back the stable channel automatically. Fix the cause and rerun the workflow;
release numbering uses the currently published version and increases again.

Public downloads enforce a cooldown and per-IP quota. The CI smoke check makes
one executable download; do not add a burst of repeated production downloads.
