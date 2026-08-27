---
name: aris-new-service-scaffold
description: Scaffold a new ARIS backend microservice (or bring an existing one up to the current standard) with the identical skeleton every service must share — Clean Architecture layering, BuildingBlocks wiring, independent JWT/RBAC validation, forced-password-change gate, correlation-ID propagation, PHI-safe logging, health checks, OpenAPI, Dockerfile, and Ocelot/Compose wiring. Use whenever adding IdentityService, PatientService, a stub service (HccMappingService/GapEngineService), or any later-phase service (DataIngestService, RafCalculationService, Embedding Worker, Agent Orchestrator, Analytics/Audit Processor) — never re-derive the skeleton per service from first principles.
---

# ARIS new-service scaffold

Every backend service in ARIS — from `IdentityService` in Phase 1 through the Phase 4/5 workers — repeats the same shape. This skill exists so that shape stays identical across all of them instead of drifting service by service. Source of truth: `Documentations/Phases/ARIS — Phase 1 Technical Documentation.md` (§1–§9, folder structure fixed in §1.3) and `Documentations/Phases/ARIS — Phase 1 Detailed Plan.md` (§3–§9). If a later phase's Technical Documentation adds a service-level requirement (e.g., an outbox table in Phase 2, an AI audit hook in Phase 4), fold it into this skill's checklist rather than letting it live only in that phase's doc.

## 0. Fixed folder structure (Technical Documentation §1.3)

Every new service goes under `src/Services/<ServiceName>/`, as four separate projects — never fewer, never a different layer split:

```text
src/Services/<ServiceName>/
├── ARIS.<ServiceName>.Domain/          # entities; zero project references
├── ARIS.<ServiceName>.Application/     # use cases, interfaces (e.g. IUserRepository); references Domain
├── ARIS.<ServiceName>.Infrastructure/  # EF Core, external integrations; implements Application's interfaces
└── ARIS.<ServiceName>.Api/             # controllers, Program.cs, Dockerfile — the composition root
```

- **Reference direction**: `Api → Infrastructure → Application → Domain`. `Domain` has no project references. `Application` never references `Infrastructure` — it defines the interfaces `Infrastructure` implements. `Api` is the only layer that wires concrete `Infrastructure` types into DI and the only layer referencing `BuildingBlocks`' ASP.NET-specific middleware directly (`Domain`/`Application` may still use `BuildingBlocks`' framework-agnostic types like `Result<T>`/`BaseEntity`).
- **Naming**: `ARIS.<ServiceName>.<Layer>` for every project, no variation.
- **Dockerfile**: lives at `ARIS.<ServiceName>.Api/Dockerfile`, build context at the repo root.
- **Tests**: mirror under `tests/ARIS.<ServiceName>.UnitTests/` and `tests/ARIS.<ServiceName>.IntegrationTests/` — a global `tests/` tree grouped by service, not colocated inside `src/Services/<ServiceName>/`.
- **Stubs get the same four projects, thin** (see §3) — never collapsed to fewer projects or folded into a single file just because there's no real logic yet.

This structure is fixed, not a first-service-decides placeholder — treat any deviation (extra/missing layer, different reference direction, tests colocated with source) as a defect to fix, not a new variant to accept.

## 1. BuildingBlocks — build first, once

Before or alongside the first service, `BuildingBlocks` must exist and every service must reference it (Detailed Plan §3):

- `Result<T>` / problem-details wrapper — every service returns errors the same shape (§4.5: `{ type, title, status, detail, traceId }`, no stack traces/connection strings/internal identifiers).
- `BaseEntity` — `Id`, `CreatedAtUtc`, `CreatedBy`, `ModifiedAtUtc`, `ModifiedBy` (every entity in every service inherits this; `CreatedBy`/`ModifiedBy` double as the audit-trail seed).
- Exception middleware — maps domain/validation exceptions to the problem-details shape.
- Health-check contract — standard `/health/live` + `/health/ready` shape.
- PHI-safe logging helper — structured logger wrapper that excludes PHI-shaped fields (name, DOB, MRN) by convention; log entity IDs, never identifying values.
- Correlation-ID middleware — reads/generates a correlation ID, propagates it to every downstream call, includes it in problem-details (`traceId`) and any audit event.

If `BuildingBlocks` doesn't yet have a piece this new service needs, extend `BuildingBlocks` itself — don't reimplement it locally in the service.

## 2. Per-service checklist

Run through all of these for every new service, in this order. Nothing here is optional or deferred to "later cleanup" — a service that skips one of these isn't done, per the project's non-negotiable principles (CLAUDE.md: "the gateway is never the sole security boundary," "PHI-safe logging from day one").

**Structure**
- [ ] Clean Architecture project split matching the established convention, referencing `BuildingBlocks`.
- [ ] Owns exactly one database (if it persists anything) — never reaches into another service's DB. Sync HTTP or async events only for cross-service communication (§111).
- [ ] Entities inherit `BaseEntity`.

**Security (independent of the gateway — §5.2/§95)**
- [ ] JWT bearer validation configured **in this service directly** (signature, expiry, issuer/audience) — never assume Ocelot forwarding the header is equivalent to validation. This is the single most-repeated risk in the Phase 1 Detailed Plan's risk table; every new service re-introduces it if skipped.
- [ ] Each protected endpoint declares its role(s) via `[Authorize(Roles = "...")]` (or the service's policy equivalent) — no ad hoc role checks in handler code.
- [ ] Forced-password-change gate middleware wired in, identically to every other service, with the same allow-list pattern (`/identity/change-password`, `/identity/me`, `/identity/logout`, `/identity/refresh` are the only Phase-1 exceptions) — a service that omits this lets a `MustChangePassword=1` user route around IdentityService entirely (Detailed Plan §13, the task-17 risk).
- [ ] No ABAC / patient-level scoping — RBAC only, unless the owning phase explicitly introduces it (Phase 6, §95/§107). Don't add it early "for safety."

**Observability & correctness**
- [ ] Correlation-ID middleware wired in; every log line and error response carries it.
- [ ] PHI-safe logging helper used for all structured logs — no raw entity objects string-interpolated into a log message or exception text.
- [ ] `/health/live` and `/health/ready` implemented against the shared contract.
- [ ] Every non-2xx response uses the RFC 7807 problem-details shape (§4.5) — check this specifically on any handwritten error path, since it's easy to leak a stack trace or raw exception message by accident.
- [ ] OpenAPI/Swagger generated for the service.

**Deployment**
- [ ] Multi-stage Dockerfile (SDK build stage → ASP.NET runtime stage), matching the pattern of existing services.
- [ ] Compose entry added: `healthcheck:` block targeting `/health/ready`; any dependent service uses `depends_on: condition: service_healthy`, never a fixed startup delay (§7.1).
- [ ] Ocelot route added (`/<prefix>/*` → this service) in the gateway config; Angular never gets a direct service hostname/port (§55 — talks only to the gateway base URL).
- [ ] New env vars (connection string, any service-specific secret) added to `.env`/Compose secrets — never committed, never baked into the image. If the service validates JWTs, it needs `JWT_PUBLIC_KEY`; only `IdentityService` holds `JWT_SIGNING_KEY` (private).
- [ ] Image tag: `latest` for local dev; an immutable tag (`aris/<service>:<version>` or `git-<sha>`) for anything pushed to a registry (§7.3).

**Explicit non-goals to check against on every new service** (§11 of Phase 1 Technical Documentation — re-verify per phase, since the list of what's "ahead of phase" changes):
- No message broker / outbox pattern before Phase 2.
- No search index or vector store before Phase 2/Phase 4.
- No real HCC mapping or gap-detection logic before Phase 3 — a "real" service in that domain stays a stub (static/empty response) until its phase arrives.
- No LLM/agent integration before Phase 4.
- No distributed tracing backend before Phase 6 — only the correlation-ID convention needs to exist now.

## 3. Stub services (HccMappingService, GapEngineService in Phase 1)

Stubs still go through the full checklist above **except** real business logic — they get the same JWT validation, health checks, Dockerfile, Compose wiring, and Ocelot route as a full service, but their endpoints return hardcoded/static data (`GET /hcc/models` → static array) or an empty result (`GET /patients/{id}/gaps` → `[]`). The point of a stub is to prove the routing/health/security pattern end-to-end, not to skip the pattern because "there's no real logic yet." Don't let a stub's simplicity become an excuse to skip JWT validation or the forced-change gate — that's exactly the class of mistake the Detailed Plan's risk table warns about.

## 4. Applying this to a later-phase service

When a later phase's Technical Documentation introduces a new service (`DataIngestService`/Indexer in Phase 2, `RafCalculationService` in Phase 3, Embedding Worker/Agent Orchestrator in Phase 4, Analytics/Audit Processor in Phase 5):

1. Re-read this checklist first — it doesn't change phase to phase.
2. Read that phase's Technical Documentation for what's *additional* (e.g., Phase 2 adds outbox-pattern event publishing; Phase 4 adds AI-audit-trail hooks on agent tool calls) — layer these on top of, not instead of, the checklist above.
3. If the new phase's pattern is itself going to recur (e.g., every Phase 4+ service needs an AI-audit hook), that's a signal this skill should gain a new subsection — update it here rather than letting the pattern live only in one phase's plan.
