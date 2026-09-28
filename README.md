# SmartWMS

SmartWMS is a local DevOps portfolio project: a React/Cognito frontend and a .NET Inventory API backed by PostgreSQL. Orders, Warehouse and Notification remain scaffolds; they are not deployed by the verified Inventory pipeline.

## Current implementation

- Real Cognito sign-in and Inventory API integration.
- PostgreSQL persistence, product creation and stock adjustments.
- API verification of token signature, issuer, expiry, access-token type and client ID. Writes require the `inventory-writers` Cognito group; audit identity comes from the verified token.
- Liveness at `/api/health/live` and database/schema readiness at `/api/health/ready`. `/api/health` is a legacy liveness endpoint.
- Explicit migration command; regular API startup does not migrate or seed.
- PostgreSQL optimistic concurrency prevents silent lost stock updates; conflicts return HTTP 409.
- Backend security/domain tests and a real-PostgreSQL concurrency test. Frontend TypeScript, lint and transport tests.
- A Jenkins pipeline with a dedicated agent, dependency audits, immutable image tags, vulnerability scans and local image artifacts. No AWS publishing stage.
- Local Kubernetes manifests with persistent PostgreSQL storage, probes, resource limits and restricted application containers. Helm remains a starter for the next milestone.

Prepared code is distinct from a verified deployment. See [validation status](docs/current-status.md) and [the runbook](docs/local-delivery-runbook.md) before running a build or deployment.

## Quick validation

```powershell
dotnet test tests/Inventory.Tests/Inventory.Tests.csproj -c Release --filter 'Category!=Postgres'
cd src/frontend/smartwms-ui
pnpm install --frozen-lockfile
pnpm typecheck
pnpm lint
pnpm test
pnpm build
```

The PostgreSQL test requires a disposable database through `SMARTWMS_TEST_DATABASE`. The CI test script provisions one automatically and does not connect to the application database.

## Local CI and deployment

1. Keep Jenkins at `http://localhost:8090` with administrator authentication and zero controller executors.
2. Run `pipeline/Setup-LocalAgent.ps1` in your own PowerShell. Enter administrator credentials only in its local prompt.
3. Run `pipeline/Verify-LocalCI.ps1` to test the working tree before committing it.
4. Commit the reviewed files, push the working branch, then configure Jenkins SCM to that branch and `pipeline/Jenkinsfile`.
5. After a successful Jenkins build, download its release artifacts and follow `infra/k8s/Deploy-Local.ps1` and the runbook.

The local builder uses a privileged Docker-in-Docker service for trusted code. It has its own daemon and no host Docker socket, AWS credentials or Kubernetes credentials. This is a learning setup, not a hardened production runner for untrusted contributions. Production needs isolated ephemeral runners, workload identity, release signing, tested backups, TLS and environment-specific operations controls.

## Cost boundary

No EKS, RDS, EC2, NAT gateway, paid scanner or ECR publishing is provisioned by this workflow. The existing AWS account is paid with zero remaining Free Tier credits; budget alerts do not cap spending. Existing Cognito service usage is still governed by the account's AWS pricing and eligibility. CloudFormation/Terraform stubs are reference material and are not executed.

## Layout

- `src/services/Inventory.API`: API, application, domain and persistence layers.
- `src/frontend/smartwms-ui`: React application.
- `tests/Inventory.Tests`: API, domain and PostgreSQL concurrency tests.
- `pipeline`: Jenkins, local agent and build scripts.
- `infra/k8s/local`: the supported local deployment manifests.
- `infra/helm`: starter chart for the next milestone.
- `docs`: architecture and operating notes.
