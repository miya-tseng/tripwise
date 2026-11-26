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

### Google Places

Place search uses Google Places API (New) when an API key is configured, and fake Tokyo / Kyoto / Taipei places otherwise. The key is only used by the API and is never sent to the browser.

```bash
dotnet user-secrets set "GooglePlaces:ApiKey" "<server key>" --project backend/TripWise.Api
```

Optional settings: `GooglePlaces:LanguageCode` (default `zh-TW`) and `GooglePlaces:Provider=Fake` to force fake places.

E2E tests expect fake places and fixed rates. If you configured real keys, run the API for E2E with:

```bash
GooglePlaces__Provider=Fake ExchangeRates__Provider=Fake Gemini__Provider=Fake dotnet run --project backend/TripWise.Api
```

### AI suggestions (Gemini)

"Suggestions" asks Gemini for places that are usually planned together with an activity's place, then resolves each one to a real place with Google Places. Without a key, fake suggestions are used.

```bash
dotnet user-secrets set "Gemini:ApiKey" "<gemini key>" --project backend/TripWise.Api
```

Defaults: `Gemini:Model=gemini-3.5-flash-lite` with fallback `gemini-3.1-flash-lite`, results cached per place for 24 hours. Use a key from a project **without billing** to stay on the free tier.

### Day map

Each day can show its places on a map (Maps JavaScript API). The map needs a **separate browser key** restricted by HTTP referrer, set in `frontend/.env.local` (git-ignored):

```bash
VITE_GOOGLE_MAPS_BROWSER_KEY=<browser key>
VITE_GOOGLE_MAPS_MAP_ID=<map id, optional, defaults to DEMO_MAP_ID>
```

Without a browser key the map button is hidden and the Google Maps links still work. The map script only loads when a day's map is opened.

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
