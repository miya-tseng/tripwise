# TripWise

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
│   ├── TripWise.Api/              # Web API
│   │   ├── Controllers/           # HTTP endpoints
│   │   ├── Domain/                # business rules (no dependencies)
│   │   ├── Services/              # application logic
│   │   ├── Data/                  # EF Core DbContext, migrations, repositories
│   │   ├── Dtos/                  # request / response models
│   │   └── Middleware/            # error handling
│   ├── TripWise.UnitTests/
│   ├── TripWise.IntegrationTests/
│   └── TripWise.E2ETests/
└── frontend/                      # Vue 3 + TypeScript
```

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
dotnet ef database update --project backend/TripWise.Api

# 3. Start the API (http://localhost:5022)
dotnet run --project backend/TripWise.Api

# 4. Start the frontend in another terminal (http://localhost:5173)
cd frontend
npm install
npm run dev
```

Check the API is connected to the database: http://localhost:5022/health should return `Healthy`.

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
