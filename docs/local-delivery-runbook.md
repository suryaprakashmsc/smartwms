# Local delivery runbook

This workflow keeps application compute, PostgreSQL, Jenkins, the image builder and Kubernetes local. It does not provision or publish to AWS. Existing Cognito configuration is reused. Helm is deliberately deferred until this deployment is validated.

## Agent and first pipeline

Run `pipeline/Setup-LocalAgent.ps1` from your own PowerShell with Docker Desktop running. It builds the digest-pinned official agent image, asks for your existing Jenkins administrator credentials, creates or validates the exclusive `smartwms-local` node with one executor, stores only that node's launch secret in an ignored private `.secrets` directory, and starts the agent/builder. Administrator credentials are not saved. The controller still has no Docker socket and no build executors.

The builder is privileged Docker-in-Docker, with TLS, its own data volume, no published ports and no host Docker socket. Restrict this node to trusted local source. Do not run untrusted pull requests or cloud deployment credentials on it. A production runner should be ephemeral and isolated on a separate host or equivalent security boundary.

Run `pipeline/Verify-LocalCI.ps1` next. It tests the working tree in the same builder before any Git commit. It creates a disposable PostgreSQL database, runs all backend tests (including concurrency), checks frontend code and dependencies, builds both images with the public Cognito configuration, scans high/critical vulnerabilities, and exports tarballs. Inspect `reports/` if any gate fails. Only build-specific test containers/networks are cleaned up; no global Docker pruning is used.

Once the agent is running, rerun only verification after code/image fixes; there is no need to repeat administrator sign-in or agent setup. Both image scan reports are retained for the current attempt. The scanner caches its database in a local builder volume and still checks for database updates. A high/critical finding in either image blocks release; the checksum manifest is created only after both pass. Treat old tarballs without a matching successful checksum manifest as unverified.

The Inventory runtime uses the official Ubuntu chiseled-extra image, which includes ICU and timezone data but has no shell or curl. Compose executes `dotnet Inventory.API.dll --healthcheck`, a three-second request to the local readiness endpoint; Kubernetes uses HTTP probes directly. Do not add diagnostic shell tools to the runtime image. The pinned frontend base requires the libexpat 2.8.5-r0 security update. Re-scan both final images after any base/package change.

After verification, review `git diff` and stage only the reviewed change list. Preserve unrelated existing changes such as `src/services/Inventory.API/docker-compose.yml`. Commit and push `working/jenkins-setup`, then change the existing `smartwms-pipeline` job from `*/main` to `*/working/jenkins-setup`, retaining `github-token-user` and script path `pipeline/Jenkinsfile`. Validate the declarative pipeline and run one manual build. Localhost cannot receive GitHub webhooks; keep the build manual until an appropriate trigger is configured.

The pipeline needs the saved public Jenkins variables `COGNITO_REGION`, `COGNITO_USER_POOL_ID`, `COGNITO_CLIENT_ID`, `COGNITO_DOMAIN`, `VITE_REDIRECT_SIGN_IN` and `VITE_REDIRECT_SIGN_OUT`. AWS account ID may remain stored but is not used by local CI. Neither `aws-credentials` nor the placeholder `ecr-credentials` is bound. Do not maintain static ECR passwords.

Download all five Jenkins release files: `smartwms-inventory.tar.gz`, `smartwms-ui.tar.gz`, `image-tag.txt`, `commit.txt`, `SHA256SUMS`. Only Jenkins builds from a reviewed commit establish a release provenance record; the local working-tree verification bundle is not a release.

## Database and Compose compatibility

Normal API startup no longer changes database schema or seeds demo data. Before starting a newly built API on Compose, run the explicit operation against the intended local database:

```powershell
cd pipeline
docker compose build inventory-api frontend
docker compose up -d postgres
docker compose run --rm --no-deps -e ASPNETCORE_ENVIRONMENT=Local inventory-api --migrate --seed-demo
docker compose up -d --no-deps inventory-api frontend
```

The existing database volume is retained. Back it up before any real-data migration. This migration tracks PostgreSQL's existing `xmin` token and does not rewrite stock quantities. Demo seeding occurs only in environment `Local`, only through this explicit command, and only on an empty products table. Existing rows whose maximum level was previously lost are not silently modified; review their intended capacity before a deliberate data correction.

The frontend now runs without root on container port 8080; host port remains 3000. PostgreSQL and API host bindings are localhost-only. Existing database passwords should be rotated through a coordinated local credential update because an old default was committed in appsettings; the code now requires runtime configuration. Do not change an existing PostgreSQL user's password merely by changing the container environment: initialized databases retain their existing user password.

## Authentication and authorization

The API accepts only valid Cognito access tokens for the configured pool and client. Reads require authentication. Writes additionally require the `inventory-writers` group claim. Assign that group only to intended users through an authorized Cognito administration step, then sign in again for a fresh token. Until then, reads should work and writes should return 403. Client-supplied `CreatedBy`/`AdjustedBy` values are ignored; the verified `sub` is the audit identity. A UI field alone cannot grant write permission.

## Deploy to the existing k3d cluster

Use the official k3d CLI and kubectl. Identify the intended local context with `kubectl config get-contexts`; do not guess a production context. The deployment script only accepts names beginning `k3d-`.

```powershell
./infra/k8s/Deploy-Local.ps1 -ReleaseDirectory '<downloaded Jenkins release folder>' -Context '<verified k3d context>'
```

The script validates all checksums, imports both images, preserves any existing database secret, applies `infra/k8s/local`, waits for PostgreSQL, runs one bounded migration job, and waits for both deployments. PostgreSQL uses a persistent claim in `local-path`. These resources require the standard k3d storage provisioner and network-policy support; they are local single-instance resources, not a high-availability database design. No cloud service or load balancer is created.

Use a temporary port-forward on localhost:3001 to inspect the frontend, `/api/health/ready` (200), and `/api/products` without a token (401). Test probes again after temporarily interrupting database access: readiness must fail while liveness remains healthy. Keep this disruption confined to the test environment.

The existing Cognito callbacks use `http://localhost:3000/callback` and `/login`. After the Kubernetes smoke checks, stop only the Compose frontend, then port-forward Kubernetes service `frontend` from 3000 to 8080. Test actual sign-in, inventory reads and an authorized write. This avoids changing the AWS allowlist. The optional Traefik ingress is separate and should only be applied when the k3d load balancer maps this same origin deliberately.

## Persistence and rollback acceptance

Record a test product and quantity, restart only the API pod, then the PostgreSQL pod, and verify the record persists. Do not delete the namespace, PVC or database volumes. Save an application-consistent database backup and prove restore to a separate disposable database before calling the backup strategy complete.

Retain the preceding verified image bundle and commit. For an application rollback, import those images and update both deployment image references together, then wait for rollouts and rerun smoke/login tests. `kubectl rollout undo` is suitable only when the preceding revision has the intended matching configuration and images. Database migrations must remain backward-compatible; an application rollback does not undo a database migration. Test this procedure before promoting beyond the local lab.

## Remaining production work

Finish live verification, choose an upgrade path for the .NET runtime, implement Helm values/templates and chart tests, replace the local single-instance database with an operated design when a budget exists, and add TLS, least-privilege runtime database roles, secret rotation, release signing, monitoring, backups/restore tests and load testing. Orders/Warehouse/Notification require their own product implementation and acceptance criteria. A passing local pipeline alone does not make those services complete.

## Sources

- [Jenkins build isolation](https://www.jenkins.io/doc/book/security/securing-builds/)
- [Jenkins Docker setup](https://www.jenkins.io/doc/book/installing/docker/)
- [Agent secret file support](https://github.com/jenkinsci/remoting/blob/master/docs/inbound-agent.md)
- [Cognito token verification](https://docs.aws.amazon.com/cognito/latest/developerguide/amazon-cognito-user-pools-using-tokens-verifying-a-jwt.html)
- [PostgreSQL concurrency token mapping](https://www.npgsql.org/efcore/modeling/concurrency.html)
