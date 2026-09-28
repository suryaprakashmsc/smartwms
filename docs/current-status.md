# Current validation status

Prepared 27 September 2026 in a writable review copy because the repository and Docker engine are outside the agent's write/execution permissions.

## Verified from the local Docker run

- Dedicated agent and isolated TLS builder running; setup reached local CI verification.
- Jenkins declarative pipeline validation passed.
- Latest saved container test reports: 20/20 backend tests passed, including real PostgreSQL concurrency and six readiness-probe cases; 4/4 frontend tests passed.
- Both rebuilt application images passed Trivy with zero high/critical findings. The earlier Debian OS and frontend libexpat findings were remediated.
- The four release checksum entries were independently verified. Local image tag: `20260927204604-6bd8d7e5352a`.
- This bundle includes uncommitted working-directory changes. Its old HEAD in `commit.txt` does not identify all of the built source. Commit and rebuild through Jenkins before treating it as a traceable release.

## Verified image-scan remediation

- Inventory runtime changed to digest-pinned .NET 8.0.31 Ubuntu Noble chiseled-extra, retaining ICU/timezone support and removing curl/shell/package-manager dependencies. Both the exact base and the full rebuilt application image scanned with zero high/critical findings.
- Compose now invokes a bounded in-process HTTP readiness check through `dotnet Inventory.API.dll --healthcheck`. Six added probe tests and the previous 13 non-PostgreSQL tests passed (19 total). A real CLI check confirmed exit 1 when readiness is unavailable. A positive CLI loopback-server test could not bind host port 8080, so positive behavior is currently covered by unit tests and still needs container validation.
- The existing frontend base had one high finding, CVE-2026-93990 in libexpat 2.8.4-r0. Dockerfile now installs the official Alpine 2.8.5-r0 fix; the full rebuilt frontend scanned with zero high/critical findings.
- Both images are scanned before reporting failure; high/critical findings still block the release manifest. A persistent local scanner cache reduces repeated DB downloads. Three simulated gate cases verified either image failing blocks release and both passing produces valid checksums.
- Next: commit the reviewed files, push the working branch and run the Jenkins SCM build. Agent setup is complete.

## Passed locally

- 13 backend security/domain/readiness tests, including wrong client, ID token, wrong issuer, invalid signature, expired/missing token, read/write permissions and forged audit identity.
- Four frontend transport tests; TypeScript checking, ESLint and production build.
- Frontend package audit: zero reported vulnerabilities after updating the lockfile.
- Backend restore/audit with all transitive packages: completed without warnings after patch updates.
- EF migration SQL generation: concurrency maps PostgreSQL's existing `xmin`; no application data rewrite or system-column alteration is emitted.
- PowerShell and shell syntax checks; YAML parsing and workload configuration checks.

## Must pass on the user's Docker environment

- Live readiness behavior of the newly built application containers.
- Jenkins authenticated SCM checkout, first build and artifact verification.
- Kubernetes rendering/server validation, migration job, rollouts, HTTP smoke checks, authenticated login, persistence and rollback.

Docker operations ran in the user's environment; their saved reports and artifact checksums were inspected independently. Kubernetes deployment and an authenticated Jenkins SCM build remain unverified. No deployment or Git push has happened as part of preparing this package.

## Remaining project scope

Cognito write-group membership must be deliberately assigned to the intended users; ordinary signed-in users can read but cannot write. Helm packaging and broader end-to-end/load/backup tests remain next-session work. Orders, Warehouse and Notification are still scaffolds. The project is not yet a production-ready system.
