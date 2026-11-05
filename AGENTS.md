# Delivery policy

The user authorizes automatic delivery of completed work in this repository.

- Work on `main`. At the end of a completed task, run the relevant checks, commit
  the task's changes and push `main` without asking for another approval.
- Use `./ci/Complete-Task.ps1 -Message "..."` to run the complete validation and
  commit/push tracked project changes. Do not include another person's unrelated
  changes; stage only the completed task's files when the working tree is shared.
- Never push failed checks, unfinished work or secrets. An explicit request to
  keep a later task local overrides this default for that task.
- GitHub Actions owns production publishing. Wait for the workflow and verify
  its deployment before reporting delivery complete. Do not manually copy
  release files to production as part of the normal delivery path.
- The RSA publisher identity in `Assets/UpdatePublicKey.xml` must remain intact.
  The private key belongs only in the protected GitHub production environment;
  never put it in source, logs, artifacts or local persistent files.
- CI publishes the Windows application and browser extension download artifacts.
  The existing website, server configuration, runtime data and other products on
  the shared server are outside this deployment's scope.
- Launch local UI previews with `--skip-update` so the source-base version does
  not download the production binary over a development build.
