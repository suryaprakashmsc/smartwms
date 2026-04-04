# SmartWMS Architecture

SmartWMS is organized as a monorepo that combines backend services, a React frontend, platform assets, and delivery automation.

## Core building blocks

- `Inventory.API`: product catalog and stock visibility
- `Orders.API`: purchase order workflows and approvals
- `Warehouse.API`: bin locations, transfers, and movement flows
- `Notification.Lambda`: event-driven notification worker
- `smartwms-ui`: React frontend for dashboards and operational screens

## Delivery path

1. Run local dependencies with Docker Compose.
2. Develop backend APIs and frontend in parallel.
3. Containerize services.
4. Deploy to local Kubernetes with k3d.
5. Extend to AWS Free Tier with CloudFormation, Lambda, SQS, SNS, API Gateway, and ECR.

## Architecture style

Each backend service follows a Clean Architecture-style split:

- `Domain`: entities and business rules
- `Application`: use cases, commands, and queries
- `Infrastructure`: persistence and external integrations
- `API`: HTTP entry point
