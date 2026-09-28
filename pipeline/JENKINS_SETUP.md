# Jenkins setup

Controller verification completed: Jenkins 2.568.3, administrator authentication, anonymous access disabled, zero controller executors, localhost-only port, no controller Docker socket, and current installed-plugin updates. Blue Ocean deprecation and the CSP recommendation remain for compatibility review.

The dedicated-agent and pipeline changes are prepared but require live verification. Follow [the local delivery runbook](../docs/local-delivery-runbook.md) and [validation status](../docs/current-status.md).

1. Run `Setup-LocalAgent.ps1` locally; provide existing administrator credentials only at its private prompt.
2. Verify the `smartwms-local` node is online with one exclusive executor.
3. Run `Verify-LocalCI.ps1`; require tests, dependency audits, both image builds and image scans to pass.
4. Review, commit and push `working/jenkins-setup`. The job still selects `*/main` until deliberately updated.
5. Keep SCM credentials `github-token-user` and script path `pipeline/Jenkinsfile`, select the pushed branch, validate the pipeline and run manually.
6. Deploy only a successful Jenkins release bundle to the verified local k3d context.

`github-token` remains for optional API use; it is not the Git SCM credential. AWS/ECR credentials are not used by this local pipeline. ECR publishing is disabled to preserve the zero-spend constraint. The local build agent is for trusted code and uses a separate privileged Docker daemon; it is not a production multi-tenant security boundary.
