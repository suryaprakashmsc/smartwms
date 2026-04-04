# ADR 0001: Monorepo and Clean Architecture

## Status

Accepted

## Context

The project needs to show enterprise-level thinking while staying manageable for a one-hour-per-day learning cadence.

## Decision

Use a single repository with:

- separate service folders for Inventory, Orders, Warehouse, and Notification
- a dedicated frontend workspace for React
- centralized `infra`, `pipeline`, and `docs` directories
- layered backend services based on Domain, Application, Infrastructure, and API

## Consequences

- onboarding stays simple because everything lives in one repository
- infrastructure and application changes can evolve together
- service boundaries remain visible without the overhead of multiple repositories
