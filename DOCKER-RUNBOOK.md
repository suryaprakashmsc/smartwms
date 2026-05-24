# SmartWMS Docker Runbook

Last updated: 2026-05-24

This runbook is the single reference to build, run, stop, and clean Docker resources for:

- `smartwms-ui` (React + Nginx)
- `inventory-api` (.NET 8)
- `postgres` (database required by `inventory-api`)

## 1) Files used by Docker

- `pipeline/docker-compose.yml`: Session 2 full stack (`postgres` + `inventory-api` + `frontend` + `jenkins`)
- `pipeline/.env.example`: copy to `pipeline/.env` and set Cognito values
- `src/frontend/smartwms-ui/Dockerfile`: UI image build
- `src/frontend/smartwms-ui/nginx.conf`: UI static host + `/api` reverse proxy to `inventory-api:8080`
- `src/services/Inventory.API/API/Dockerfile`: Inventory API image build
- `src/services/Inventory.API/docker-compose.yml`: API-only stack (`inventory-api` + `postgres`)

## 2) Recommended way to run everything

Session 2 standard flow (from `pipeline` directory):

```powershell
cd d:\Work\Architect-poc\projects\smartwms\pipeline
copy .env.example .env
# Edit .env and set real values (especially POSTGRES_PASSWORD and Cognito fields)
docker compose up -d --build
```

If port `5432` is already used on your machine, run with a different host port (example `5433`):

```powershell
$env:POSTGRES_HOST_PORT=5433
docker compose up -d --build
```

Why this is recommended:

- `smartwms-ui` and `inventory-api` are on the same Docker network
- UI `/api` proxy resolves `inventory-api` correctly
- `inventory-api` uses `Host=postgres` (not `localhost`) for DB

## 3) Verify services

```powershell
docker compose ps
docker compose logs -f inventory-api
```

Open:

- UI: `http://localhost:3000`
- Inventory API health: `http://localhost:8081/api/health`
- Jenkins: `http://localhost:8090`

## 4) Stop services

Stop and keep data volume:

```powershell
docker compose down
```

Stop and also remove DB volume (full reset):

```powershell
docker compose down -v
```

## 5) Build or run individual parts

### UI only (image build + run)

```powershell
.\smartwms-ui.bat build 3000
.\smartwms-ui.bat run 3000
```

Note: UI-only run is for static/app startup checks. For real `/api` calls, run the full compose stack so `inventory-api` is reachable by container DNS.

### Inventory API + Postgres only

```powershell
docker compose -f src/services/Inventory.API/docker-compose.yml up -d --build
docker compose -f src/services/Inventory.API/docker-compose.yml logs -f inventory-api
```

## 6) What to keep vs remove in Docker

Based on your current list, keep these if you are running SmartWMS locally:

- Images:
  - `smartwms-ui:local`
  - `smartwms-inventory:local`
  - `postgres:16-alpine`
- Containers:
  - the currently running stack containers from `pipeline/docker-compose.yml`

Optional (remove if not needed):

- `jenkins/jenkins:lts` image and `jenkins` container (only needed for local Jenkins work)
- K3d/Kubernetes-related images and containers:
  - `kindest/node`
  - `docker/desktop-cloud-provider-kind`
  - `envoyproxy/envoy`
  - `docker/desktop-containerd-registry-mirror`
  - `ghcr.io/k3d-io/k3d-proxy`
  - `ghcr.io/k3d-io/k3d-tools`
  - `rancher/k3s`
  - containers like `k3d-smartwms-server-0`, `k3d-smartwms-serverlb`, `k3d-smartwms-tools`

Safe to remove:

- old stopped random containers created from repeated `docker run` tests

## 7) Cleanup commands (safe sequence)

Run from `d:\Work\Architect-poc\projects\smartwms\pipeline`.

### A) Stop SmartWMS compose stack

```powershell
cd d:\Work\Architect-poc\projects\smartwms\pipeline
docker compose down --remove-orphans
```

### B) Remove all stopped containers

```powershell
docker container prune -f
```

### C) Remove only SmartWMS images (forces recreate on next build)

```powershell
docker image rm smartwms-ui:local smartwms-inventory:local
```

### D) Remove Jenkins image/container (optional)

```powershell
docker rm -f smartwms-jenkins 2>$null
docker image rm jenkins/jenkins:lts
```

### E) Remove K3d/Kubernetes artifacts (optional)

If you use k3d and want full cleanup:

```powershell
k3d cluster delete smartwms
```

Then remove leftover k3d/k8s images if they still exist:

```powershell
docker image rm kindest/node:v1.34.3 docker/desktop-cloud-provider-kind:v0.5.0 envoyproxy/envoy:v1.36.4 docker/desktop-containerd-registry-mirror:v0.0.3 ghcr.io/k3d-io/k3d-proxy:5.8.3 ghcr.io/k3d-io/k3d-tools:5.8.3 rancher/k3s:v1.31.5-k3s1
```

### F) Remove dangling/unused layers and caches

```powershell
docker image prune -f
docker builder prune -f
```

Use this broader cleanup only if you want to remove all unused images and free maximum space:

```powershell
docker system prune -a --volumes -f
```

## 8) Rebuild clean from scratch (SmartWMS only)

```powershell
cd d:\Work\Architect-poc\projects\smartwms\pipeline
docker compose down -v --remove-orphans
docker image rm smartwms-ui:local smartwms-inventory:local 2>$null
docker compose up -d --build
```

## 9) Troubleshooting quick checks

### Error: `path "src/frontend/smartwms-ui" not found`

Run from repository root, or use:

```powershell
docker build -f src/frontend/smartwms-ui/Dockerfile -t smartwms-ui:local src/frontend/smartwms-ui
```

### Error: `Failed to connect to 127.0.0.1:5432`

Inside Docker, `localhost` points to the same container. Use `Host=postgres` when API and DB are in compose together.

### Error: `Bind for 0.0.0.0:5432 failed: port is already allocated`

Another container is already using host port `5432`.

Option 1 (recommended): run SmartWMS Postgres on another host port:

```powershell
cd d:\Work\Architect-poc\projects\smartwms\pipeline
$env:POSTGRES_HOST_PORT=5433
docker compose up -d --build
```

Option 2: stop/remove the other container that uses `5432`, then run compose again:

```powershell
docker ps --filter "publish=5432"
docker stop <container_name_or_id>
cd d:\Work\Architect-poc\projects\smartwms\pipeline
docker compose up -d --build
```

### Cognito redirect goes to `localhost:5173`

Rebuild UI image with correct build args (redirect URLs are baked at build time), then restart stack.
