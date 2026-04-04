# smartwms

SmartWMS is a real-world Smart Inventory and Warehouse Management System learning project built around .NET 8-style backend services, AWS Free Tier services, React, Docker, Kubernetes, and Jenkins CI/CD.

## Repository structure

```text
smartwms/
|-- src/
|   |-- services/
|   |   |-- Inventory.API/
|   |   |   |-- Domain/
|   |   |   |-- Application/
|   |   |   |-- Infrastructure/
|   |   |   `-- API/
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

## What is scaffolded

- Clean Architecture starter folders for Inventory, Orders, and Warehouse services
- A Visual Studio solution file that groups the backend service projects
- React plus Vite-style frontend skeleton with dashboard, products, orders, warehouse, and login pages
- Docker Compose for PostgreSQL, pgAdmin, and Redis
- Jenkins pipeline placeholder
- Kubernetes manifests and a starter Helm chart
- CloudFormation placeholders and architecture documentation

## Suggested next steps

1. Install the .NET 8 SDK, Node.js 20 LTS, Docker Desktop, and AWS CLI.
2. Open `smartwms.sln` and confirm the backend projects restore and build.
3. Turn each in-memory repository into a real persistence implementation.
4. Run the frontend with `npm install` and `npm run dev` inside `src/frontend/smartwms-ui`.
5. Expand the Jenkins and Kubernetes assets as you move into Phases 4 and 5.
