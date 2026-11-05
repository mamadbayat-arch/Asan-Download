# CI/CD implementation

- [x] Verify GitHub administration, repository state and live update channel.
- [x] Add Windows build, UI/protocol/server tests and signed release workflow.
- [x] Add automatic release versions and a completion/commit/push procedure.
- [x] Add a restricted deployment receiver with signature checks and rollback.
- [x] Configure production environment, encrypted signing secret and short-lived GitHub OIDC identity.
- [x] Run local checks, push to main and verify a real GitHub Actions deployment.
- [x] Record operating instructions; production verification is recorded by the workflow.

# UI stability follow-up

- [x] Remove repeated entrance/theme overlay animations.
- [x] Keep input controls attached while switching responsive layout.
- [x] Batch language/theme layout and compose child painting.
- [x] Remove expensive background bitmap regeneration during resize.
- [x] Center dialog action groups horizontally and vertically.
- [x] Validate FA/EN, both themes, window sizes and rapid layout/language changes (101 UI checks).
- [x] Keep download failures visible without blocking the UI or consuming queued links.
- [x] Verify the complete pipeline in GitHub Actions run 41991425818 (all three jobs passed).
