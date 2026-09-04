# ARIS

ARIS (Adaptive Risk Intelligence System) is a risk-adjustment intelligence platform for healthcare — HCC mapping, gap-in-care detection, RAF calculation, and (eventually) RAG/agentic explanation over clinical evidence. It's built solo, phase-by-phase, as a set of ASP.NET Core microservices behind an Ocelot gateway with an Angular frontend, Docker-first throughout.

## Current state

Mid-Phase-1. What exists today:

- Solution scaffolding (`aris.sln`) and a shared library, `aris.BuildingBlocks` (`Result`/problem-details wrapper, `BaseEntity`, exception middleware, health-check contract, PHI-safe logging helpers, correlation-ID middleware).
- A working **IdentityService** (Api/Application/Domain/Infrastructure) with login, refresh/logout, session auto-expiry, and unit + integration test projects.
- The Angular app, **aris-web**, including the app shell and login screen.
- A `docker-compose.yml` wiring `sqlserver` + `identity-service` + `aris-web`.

Not yet scaffolded: the Ocelot gateway, `PatientService`, and the `HccMappingService`/`GapEngineService` stubs.

The full design lives under [`Documentations/`](Documentations/) — treat anything described there but not yet reflected in code as the target to build toward, not the current state.

## Repository layout

```
aris.sln
src/
  BuildingBlocks/aris.BuildingBlocks/        # shared library — no running service
  Services/
    IdentityService/
      aris.IdentityService.Api/
      aris.IdentityService.Application/
      aris.IdentityService.Domain/
      aris.IdentityService.Infrastructure/
apps/
  aris-web/                                  # Angular frontend
tests/
  aris.IdentityService.UnitTests/
  aris.IdentityService.IntegrationTests/
docker-compose.yml
Documentations/                              # full spec: functional, technical, plan, test, UI docs
```

## Prerequisites

- .NET SDK 10
- Node.js 22+ (Angular 22)
- Docker Desktop (or compatible engine) with Docker Compose

## Running with Docker Compose (recommended)

This is the actual exit-criteria bar for any slice — not just running from the IDE.

1. Copy the environment template and fill in a SQL Server password:
   ```
   cp .env.example .env
   ```
   `SQLSERVER_SA_PASSWORD` is required (must satisfy SQL Server's complexity policy). `JWT_SIGNING_KEY` can be left blank in local dev — IdentityService falls back to an ephemeral, Development-only signing key that's regenerated (invalidating any previously-issued tokens) on every container restart.

2. Start the stack:
   ```
   docker compose up --build
   ```

3. Services:
   - `aris-web` (Angular) — http://localhost:4200
   - `identity-service` (API) — http://localhost:5146
   - `sqlserver` — localhost:1433

## Running locally without Docker

**Backend** (from the repo root):
```
dotnet restore
dotnet build
dotnet run --project src/Services/IdentityService/aris.IdentityService.Api
```
You'll need a local SQL Server instance and a matching `ConnectionStrings__IdentityDb` value (see `docker-compose.yml` for the expected shape).

**Frontend**:
```
cd apps/aris-web
npm install
npm start
```

## Tests

```
dotnet test
```

## Documentation

Read in this order when you need context (see [`CLAUDE.md`](CLAUDE.md) for the full map and the project's non-negotiable engineering principles):

1. `Documentations/Holy Grail/ARIS — Complete Implementation and User Reference Documentation.md` — source-of-truth functional spec for the whole product.
2. `Documentations/Holy Grail/ARIS — Project Plan.md` — phase sequencing and build order.
3. `Documentations/Holy Grail/ARIS — Technical Documentation.md` — target end-state architecture.
4. `Documentations/Phases/ARIS — Phase 1 Functional Requirements.md`
5. `Documentations/Phases/ARIS — Phase 1 Technical Documentation.md`
6. `Documentations/Phases/ARIS — Phase 1 Detailed Plan.md`
7. `Documentations/Phases/ARIS — Phase 1 Test Documentation.md`
8. `Documentations/Phases/ARIS — Phase 1 UI Guidelines.md`

## Roadmap

Phase 1 (Platform/Identity/UI foundation, in progress) → Phase 2 (Clinical data ingestion/search) → Phase 3 (Deterministic risk intelligence: HCC/Gap/RAF) → Phase 4 (RAG & agentic intelligence) → Phase 5 (Complete persona workflows) → Phase 6 (Enterprise/scale/research).
