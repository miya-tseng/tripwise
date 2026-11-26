# TripWise

[![CI](https://github.com/miya-tseng/tripwise/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/miya-tseng/tripwise/actions/workflows/ci.yml)

A trip planner built to practice **unit, integration and E2E testing** in .NET.

Plan a trip day by day: add activities with time slots, prevent overlapping schedules, and keep track of your budget.

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 10 Web API (Controllers), lightweight DDD |
| Database | PostgreSQL 17, EF Core 10 (Npgsql) |
| Frontend | Vue 3, TypeScript, Vite, Vue Router, Pinia |
| Unit tests | NUnit |
| Integration tests | NUnit + Testcontainers (real PostgreSQL) |
| E2E tests | NUnit + Playwright for .NET |
| Frontend tests | Vitest + Vue Test Utils |

## Project Structure

```
tripwise/
├── TripWise.slnx                  # .NET solution
├── global.json                    # pins .NET SDK 10.x
├── docker-compose.yml             # local PostgreSQL
├── backend/
│   ├── TripWise.Domain/           # entities, value objects, repository interfaces (no dependencies)
│   ├── TripWise.Application/      # services and DTOs
│   ├── TripWise.Infrastructure/   # EF Core DbContext, configurations, migrations, repositories
│   ├── TripWise.Api/              # controllers, middleware, Program.cs
│   ├── TripWise.UnitTests/
│   ├── TripWise.IntegrationTests/
│   └── TripWise.E2ETests/
└── frontend/                      # Vue 3 + TypeScript
```

### Layer dependencies

Each layer is its own project, so the compiler enforces the dependency direction:

```
Api ──► Application ──► Domain
 │                        ▲
 └──► Infrastructure ─────┘ (also references Application)
```

`TripWise.Domain` has no package or project references. Infrastructure services are registered with `AddInfrastructure()` and application services with `AddApplication()`.

## Getting Started

### Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) 24+
- Docker (Docker Desktop, OrbStack or Rancher Desktop)

### Run locally

```bash
# 1. Start PostgreSQL
docker compose up -d

# 2. Restore tools and apply database migrations
dotnet tool restore
dotnet ef database update --project backend/TripWise.Infrastructure --startup-project backend/TripWise.Api

# 3. Start the API (http://localhost:5022)
dotnet run --project backend/TripWise.Api

# 4. Start the frontend in another terminal (http://localhost:5173)
cd frontend
npm install
npm run dev
```

Check the API is connected to the database: http://localhost:5022/health should return `Healthy`.

### Exchange rates

Costs are converted to TWD with the latest rates from [open.er-api.com](https://www.exchangerate-api.com) (no API key, cached for 6 hours). To work offline or run E2E tests without calling the real API, start the API with fixed rates:

```bash
ExchangeRates__Provider=Fake dotnet run --project backend/TripWise.Api
```

### Add a migration

```bash
dotnet ef migrations add <Name> \
  --project backend/TripWise.Infrastructure \
  --startup-project backend/TripWise.Api \
  --output-dir Data/Migrations
```

## Running Tests

```bash
# Backend (from the repo root)
dotnet test

# Frontend
cd frontend
npm run test:unit
```

## Roadmap (MVP)

- [x] Development environment and project structure
- [ ] Trip management (create, list, delete)
- [ ] Activity scheduling with overlap prevention
- [ ] Budget tracking
- [ ] CI with GitHub Actions (unit → integration → E2E)
