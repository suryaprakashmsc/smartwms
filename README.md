# smartwms

SmartWMS is a learning project for a Smart Inventory and Warehouse Management System built as a monorepo with .NET 8 backend services, a React frontend, Docker-based local dependencies, and starter CI/CD and infrastructure assets.

## Current status

The repository is beyond a pure empty scaffold. It currently contains:

- Three backend services that compile successfully: `Inventory.API`, `Orders.API`, and `Warehouse.API`
- A React frontend that builds successfully and includes routing, a demo login flow, and dashboard pages
- Local development infrastructure for PostgreSQL, pgAdmin, Redis, and Jenkins in [`pipeline/docker-compose.yml`](pipeline/docker-compose.yml)
- Starter deployment and architecture assets for Kubernetes, Helm, CloudFormation, and Jenkins

What is still true is that most business functionality is still starter-level. The codebase has the main project structure in place, but only Inventory has real PostgreSQL persistence so far; many Orders/Warehouse workflows are still mocked, in-memory, or placeholder implementations.

## Repository structure

```text
smartwms/
|-- src/
|   |-- services/
|   |   |-- Inventory.API/
|   |   |-- Orders.API/
|   |   |-- Warehouse.API/
|   |   `-- Notification.Lambda/
|   `-- frontend/
|       `-- smartwms-ui/
|-- infra/
|   |-- cloudformation/
|   |-- terraform/
|   |-- k8s/
|   `-- helm/
|-- pipeline/
|-- docs/
|-- smartwms.sln
`-- README.md
```

## What is implemented

### Backend

- Clean Architecture-style folder layout exists for Inventory, Orders, and Warehouse services:
  `Domain`, `Application`, `Infrastructure`, and `API`
- Each API project starts successfully, registers Swagger, controllers, application services, and infrastructure services
- Each API exposes:
  - a root endpoint returning a service metadata payload
  - a `/api/health` controller endpoint returning a health response with UTC timestamp
- Domain entities are defined for:
  - products
  - purchase orders
  - bin locations
- Infrastructure now includes PostgreSQL persistence (EF Core + migrations) for `Inventory.API` products.
- `Orders.API` and `Warehouse.API` still use in-memory repositories with seed data.
- Application layer contains starter commands, queries, interfaces, and service registration extension methods
- `Notification.Lambda` exists and compiles as a placeholder Lambda function

### Frontend

- React plus Vite frontend is present under `src/frontend/smartwms-ui`
- Routing is implemented for:
  - `/login`
  - `/`
  - `/products`
  - `/orders`
  - `/warehouse`
- A demo auth flow is implemented using `localStorage`
- The UI includes:
  - app shell/sidebar navigation
  - dashboard summary cards
  - products page with seeded sample rows
  - orders page with seeded sample rows
  - warehouse page with seeded sample rows
- A small API client helper is present for future backend integration

### DevOps and infrastructure starters

- Local Docker Compose services are configured for:
  - Jenkins
  - PostgreSQL
  - pgAdmin
  - Redis
- A Jenkins pipeline file exists at `pipeline/Jenkinsfile`
- Kubernetes manifests exist for deployment, service, and ingress
- A starter Helm chart exists under `infra/helm/smartwms`
- CloudFormation placeholder files exist for VPC, RDS, Lambda/SQS/SNS, and API Gateway
- Architecture docs and one ADR already exist under `docs/`

## What is not implemented yet

The following areas are still placeholder or incomplete:

- `Inventory.API` has working product endpoints and PostgreSQL persistence, but Orders/Warehouse APIs are still mostly starter-level beyond health and root metadata endpoints.
- `Orders.API` and `Warehouse.API` still use in-memory repositories.
- EF Core/PostgreSQL integration is not yet implemented for Orders/Warehouse services.
- Request/response models, validators, handlers, and richer workflow orchestration are still incomplete across the broader platform.
- No frontend-to-backend integration yet; UI pages currently render hard-coded sample data
- No real authentication or authorization; login is demo-only local storage state
- No automated test projects for backend or frontend
- No Dockerfiles for packaging the services and frontend
- `pipeline/Jenkinsfile` stages are placeholders with `echo` steps only
- Kubernetes, Helm, CloudFormation, and Terraform content is starter-level and not yet wired to a working deployment path

## Verification completed

The following checks were run successfully:

- April 5, 2026: `dotnet build smartwms.sln`
- April 5, 2026: `npm run build` in `src/frontend/smartwms-ui`
- April 6, 2026: `Inventory.API` restore/build, EF migration creation and `database update`, plus persistence verification across API restart

This confirms the current solution builds cleanly and Inventory persistence is operational, even though broader platform features are still early-stage.

## Recommended next steps

To move from scaffold to working project, this is the highest-value sequence:

1. Expand Inventory product endpoints and implement real endpoints for purchase orders and bin locations.
2. Add application handlers and service logic so commands and queries actually drive behavior.
3. Extend PostgreSQL-backed persistence with EF Core to `Orders.API` and `Warehouse.API`.
4. Connect the frontend pages to backend APIs and remove hard-coded demo data.
5. Add authentication properly, such as JWT or Cognito-backed flows, depending on the target environment.
6. Add automated tests:
   backend unit tests, API integration tests, and frontend component or page tests.
7. Add Dockerfiles for each deployable service and for the frontend.
8. Replace the placeholder Jenkins pipeline with restore, build, test, image build, and deploy stages.
9. Decide on the first real deployment target:
   local Kubernetes with k3d, or AWS-first using ECS/EKS/Lambda-related infrastructure.
10. Flesh out infrastructure manifests only after the application runtime path is stable.

## Suggested immediate sprint

If we want the fastest path to real progress, the next implementation sprint should be:

1. Orders service next:
   add real order endpoints, persistence, and tests.
2. Warehouse service after that:
   add real bin/location workflows with PostgreSQL persistence and tests.
3. Frontend integration next:
   replace static data with real API calls starting with products, then orders and warehouse.
4. After that, wire the Jenkins pipeline to build and test the solution automatically.

## Running locally

### Backend

```powershell
dotnet build smartwms.sln
```

Run an API from its `API` project folder with `dotnet run`.

### Frontend

```powershell
cd src/frontend/smartwms-ui
npm install
npm run dev
```

### Local infrastructure

```powershell
cd pipeline
docker compose up -d
```

Jenkins is available at `http://localhost:8081`.

### Inventory API PostgreSQL runbook

From the repository root, start PostgreSQL:

```powershell
docker compose -f pipeline/docker-compose.yml up -d postgres
docker ps --filter "name=smartwms-db"
```

Restore and build Inventory service projects:

```powershell
dotnet restore src/services/Inventory.API/API/Inventory.API.csproj
dotnet build src/services/Inventory.API/API/Inventory.API.csproj -c Debug
```

If `dotnet ef` is not installed, install it once locally:

```powershell
dotnet tool install dotnet-ef --tool-path ./.dotnet/tools --version 8.0.0
```

Create and apply migrations:

```powershell
cd src/services/Inventory.API
../../..\.dotnet\tools\dotnet-ef migrations add InitialCreate --project Infrastructure --startup-project API --output-dir Persistence/Migrations
../../..\.dotnet\tools\dotnet-ef database update --project Infrastructure --startup-project API
```

Verify schema in PostgreSQL:

```powershell
docker exec smartwms-db psql -U smartwms -d inventory -c "\dt"
docker exec smartwms-db psql -U smartwms -d inventory -c "SELECT column_name, data_type FROM information_schema.columns WHERE table_name='products' ORDER BY ordinal_position;"
```

Run API and validate persistence across restart:

```powershell
dotnet run --project src/services/Inventory.API/API/Inventory.API.csproj --urls http://localhost:62950
```

In Swagger (`http://localhost:62950/swagger`) or via API client:

1. `POST /api/Products` with a new SKU.
2. `GET /api/Products` and confirm the product appears.
3. Stop and start the API again.
4. `GET /api/Products` and confirm the same product still appears.

If step 4 succeeds, PostgreSQL persistence is working.

## Documentation

- Current implementation snapshot: `docs/current-status.md`
- Architecture overview: `docs/architecture.md`
- ADR: `docs/adr/0001-monorepo-and-clean-architecture.md`
- Frontend workspace notes: `src/frontend/smartwms-ui/README.md`




