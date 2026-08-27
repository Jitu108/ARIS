# ARIS — Phase 1 Detailed Plan: Platform, Identity & UI Foundation

**Parent document:** `ARIS — Project Plan.md`
**Companion documents:** `ARIS — Phase 1 Functional Requirements.md` (what to build), `ARIS — Phase 1 Technical Documentation.md` (authoritative architecture/data/API/security/deployment design — this document covers execution planning: work breakdown, effort estimates, and phase-specific risks, and defers to the Technical Documentation for design detail), `ARIS — Phase 1 Test Documentation.md` (how it's verified), `ARIS — Phase 1 UI Guidelines.md` (visual design system + mockup)
**Source spec sections:** §93–§96 (development model, IdentityService, first vertical slice), §102 (Phase 1 scope), §108–§111 (Docker/communication/ownership principles), §117 (Foundation/Identity checklist)
**Scope:** Everything required to reach the Phase 1 exit criteria in §102 — an authenticated, deployable ARIS shell with working patient search, nothing more.
**Explicitly out of scope for Phase 1:** real HCC mappings, real gap logic, ingestion, search indexing, RAG/agents — HccMappingService and GapEngineService exist only as thin stubs this phase (real implementation is Phase 3).

---

## 1. Objective

Produce a working platform, not a backend skeleton. By the end of Phase 1, a user opens Angular, logs in through IdentityService, and can search for and view a patient — all running through Docker Compose, with the gateway, auth, and authorization architecture proven before any clinical intelligence is built on top of it.

---

## 2. Phase 1 Architecture

```text
                        ┌────────────┐
                        │ Angular UI │
                        └─────┬──────┘
                              │  JWT bearer
                              ▼
                     ┌─────────────────┐
                     │ Ocelot Gateway  │
                     └──┬───────────┬──┘
                        │           │
                        ▼           ▼
              ┌─────────────────┐ ┌────────────────┐
              │ IdentityService │ │ PatientService │
              └────────┬────────┘ └───────┬────────┘
                       │                  │
                       ▼                  ▼
                 ┌───────────┐      ┌───────────┐
                 │ IdentityDb│      │ PatientDb │
                 └───────────┘      └───────────┘

        (stubs, not wired to real logic yet)
              ┌────────────────────┐   ┌──────────────────┐
              │ HccMappingService  │   │ GapEngineService  │
              │ (static/seed data) │   │ (returns empty)   │
              └────────────────────┘   └───────────────────┘
```

Each service owns its own database (§111) — `IdentityDb` and `PatientDb` are separate SQL Server databases from day one, even though both run in the same SQL Server container in Phase 1. This avoids a costly split later.

RabbitMQ, OpenSearch, and Qdrant are **not** introduced in Phase 1 — they arrive in Phase 2 and Phase 4 respectively. Phase 1 is synchronous HTTP only (§110).

---

## 3. BuildingBlocks (shared kernel)

Build this before or alongside IdentityService — every subsequent service depends on it.

- **Result/Error wrapper** — a consistent `Result<T>` / problem-details shape so every service returns errors the same way.
- **Base entity** — `Id`, `CreatedAtUtc`, `CreatedBy`, `ModifiedAtUtc`, `ModifiedBy` (the `CreatedBy`/`ModifiedBy` fields double as the seed for the audit trail called out in the project plan's cross-phase workstreams).
- **Exception middleware** — maps domain/validation exceptions to consistent HTTP problem responses.
- **Health check contract** — standard `/health/live` and `/health/ready` shape every service implements.
- **PHI-safe logging helpers** — structured logging wrapper that redacts/omits PHI-shaped fields by convention now, so the habit is established before real clinical data exists (per the Project Plan's cross-phase workstream guidance).
- **Correlation ID middleware** — propagate a request/correlation ID from Ocelot through every downstream call; this is the seed for distributed tracing in Phase 6 and for audit reconstruction later.

---

## 4. IdentityService

### 4.1 Responsibilities (§94)

Authentication, user lifecycle, password management, token issuance, roles, claims, user profile, authentication audit.

### 4.2 Data model

```text
User
 - Id (guid)
 - Username / Email
 - PasswordHash
 - PasswordSalt (if not using a self-salting hash like BCrypt/Argon2)
 - DisplayName
 - IsActive
 - CreatedAtUtc / ModifiedAtUtc

Role
 - Id
 - Name  (Administrator | Clinician | Coder | RiskAnalyst | Auditor | Researcher)

UserRole
 - UserId
 - RoleId

RefreshToken
 - Id
 - UserId
 - TokenHash
 - ExpiresAtUtc
 - RevokedAtUtc (nullable)
 - ReplacedByTokenId (nullable, for rotation)

AuthAuditEvent
 - Id
 - UserId (nullable — failed logins may not resolve to a known user)
 - EventType  (LoginSucceeded | LoginFailed | Logout | TokenRefreshed | TokenRevoked)
 - TimestampUtc
 - IpAddress
 - CorrelationId
```

Six roles per §94: `Administrator`, `Clinician`, `Coder`, `RiskAnalyst`, `Auditor`, `Researcher`. Seed these on startup/migration — don't hardcode them in application logic.

### 4.3 Endpoints

| Method | Route | Purpose | Auth |
|---|---|---|---|
| POST | `/api/identity/login` | Authenticate, issue access + refresh token | Anonymous |
| POST | `/api/identity/refresh` | Exchange refresh token for new access token | Anonymous (token-bearing) |
| POST | `/api/identity/logout` | Revoke refresh token | Authenticated |
| GET | `/api/identity/me` | Current user profile + roles/claims | Authenticated |
| POST | `/api/identity/users` | Create user (admin bootstrap only in Phase 1) | Administrator |
| GET | `/api/identity/users/{id}` | Get user | Administrator |
| GET | `/health/live`, `/health/ready` | Health checks | Anonymous |

Full user CRUD/self-service (password reset flows, invite flows) is not required for Phase 1 exit criteria — a minimal admin-seeded user set is sufficient. Don't over-build user management here; §106 and §107 revisit administration more fully.

### 4.4 Token strategy

- Short-lived JWT access token (e.g., 15–30 min) containing `sub`, `roles`, `name`, and standard claims.
- Longer-lived opaque refresh token, stored hashed server-side, rotated on each use (old token revoked, new one issued) to limit replay risk.
- Signing: asymmetric (RS256) preferred even in Phase 1, so the same public key can later validate tokens if an external IdP is introduced (§94 explicitly keeps the door open to OIDC/OAuth2 later — don't paint into a symmetric-secret corner).
- Secrets (signing key, DB connection strings) come from environment/Docker secrets in Phase 1, not source control; a real Secrets Manager is Phase 6.

### 4.5 Authorization model (§95)

RBAC only in Phase 1 — no ABAC or resource-level (patient-level) authorization yet, that's explicitly a later phase. Each protected controller action should declare its required role(s) via policy, e.g. `[Authorize(Roles = "Clinician,Coder")]`, rather than checking roles ad hoc in handler code.

Critical rule from §95: **the Angular app is never the security boundary.** Every downstream service (PatientService included) must independently validate the JWT and enforce role checks — Ocelot forwarding a token is not equivalent to the downstream service trusting it blindly. Configure JWT bearer validation in PatientService (and every future service) directly, not only at the gateway.

---

## 5. PatientService

### 5.1 Responsibilities

Owns patient demographic data and exposes search/detail read APIs. In Phase 1 this is intentionally minimal — no encounters/diagnoses yet (those arrive with ingestion in Phase 2, though the Patient entity itself should be modeled with room for those relationships per the canonical model in §21).

### 5.2 Data model (Phase 1 subset of §21)

```text
Patient
 - Id (guid)
 - MRN (medical record number, unique)
 - FirstName / LastName
 - DateOfBirth
 - Sex
 - CreatedAtUtc / ModifiedAtUtc
 - SourceSystem (provenance seed, per §20 — even Phase 1 data should carry a source tag)
```

Seed a small synthetic patient dataset (per §67, synthetic data — never real PHI in a dev environment) for search/demo purposes.

### 5.3 Endpoints

| Method | Route | Purpose | Auth |
|---|---|---|---|
| GET | `/api/patients?query=&page=&pageSize=` | Search by MRN/name, paginated | Clinician, Coder, RiskAnalyst, Administrator |
| GET | `/api/patients/{id}` | Patient detail | Clinician, Coder, RiskAnalyst, Administrator |
| GET | `/health/live`, `/health/ready` | Health checks | Anonymous |

Pagination, empty-result, and error responses must be well-defined now — §97 calls these out explicitly as things this slice validates, and every later list/search screen in the app (gaps, evidence, work queues) will follow the same contract.

---

## 6. HccMappingService & GapEngineService (stubs)

These exist in Phase 1 purely so Ocelot routing, service discovery, and the pattern of "Angular calls a real microservice" are proven end-to-end — not because Phase 1 needs real risk logic.

- `HccMappingService`: expose `GET /api/hcc/health` and a placeholder `GET /api/hcc/models` returning a hardcoded/static list. No real ICD→HCC mapping logic yet.
- `GapEngineService`: expose `GET /api/gaps/health` and `GET /api/patients/{id}/gaps` returning an empty array. No real gap detection yet.

Keep these deliberately thin. Do not let Phase 1 scope creep into Phase 3's mapping/gap logic — the project plan's phase boundary exists specifically so the deterministic risk layer gets focused attention in Phase 3.

---

## 7. Ocelot Gateway

### 7.1 Routes (Phase 1 subset of §55)

```text
/identity/*   → IdentityService
/patients/*   → PatientService
/hcc/*        → HccMappingService   (stub)
/gaps/*       → GapEngineService    (stub)
```

- Ocelot should forward the `Authorization` header untouched and add/propagate the correlation ID header.
- Gateway-level rate limiting can be left at defaults in Phase 1; revisit under Phase 6 hardening.
- Angular talks only to the gateway base URL — no service hostnames/ports baked into frontend config (§55's explicit requirement).

---

## 8. Angular Application

### 8.1 Project structure (suggested)

```text
apps/aris-web/
 ├── core/
 │    ├── auth/            (auth service, token storage, refresh logic)
 │    ├── interceptors/    (HTTP auth interceptor, error interceptor)
 │    ├── guards/          (auth guard, role guard)
 │    └── layout/          (shell: header, sidebar)
 ├── features/
 │    ├── login/
 │    ├── dashboard/
 │    └── patients/
 │         ├── patient-search/
 │         └── patient-detail/
 └── shared/                (empty-state, loading, pagination components)
```

### 8.2 Screens (§102)

| Screen | Notes |
|---|---|
| Login | Username/password form → `POST /identity/login`; store access token in memory, refresh token per chosen storage strategy (prefer httpOnly cookie if the gateway can set one; otherwise a deliberately-reviewed secure storage choice — avoid plain `localStorage` for the refresh token if avoidable) |
| App shell | Header (user name, logout), sidebar (nav scoped to role) |
| Dashboard shell | Empty placeholder for now — real content arrives Phase 3+ |
| Patient search | Search box, paginated results table, loading/empty states |
| Patient details | Demographics panel only in Phase 1 |
| Unauthorized (403) | Shown when a route guard rejects on role, or backend returns 403 |
| Not found (404) | Standard fallback route |

### 8.3 Cross-cutting Angular pieces

- **Auth guard**: blocks navigation to protected routes without a valid access token.
- **Role guard**: blocks navigation to routes requiring a role the user's token claims don't include.
- **HTTP interceptor**: attaches `Authorization: Bearer <token>`, handles 401 by attempting a silent refresh once, then redirects to login on failure.
- **Error interceptor**: maps backend problem-details responses to a consistent UI error surface.

---

## 9. Docker Compose (Phase 1 services)

```text
docker-compose.yml
 ├── sqlserver          (single instance; IdentityDb + PatientDb as separate DBs)
 ├── identity-service
 ├── patient-service
 ├── hcc-mapping-service   (stub)
 ├── gap-engine-service    (stub)
 ├── ocelot-gateway
 └── aris-web (angular, served via nginx or dev server)
```

- Each service gets its own Dockerfile (multi-stage build: SDK → runtime).
- Environment configuration via `.env` / Docker Compose env files — connection strings, JWT signing key, gateway base URL for Angular build.
- Health checks defined in Compose (`healthcheck:` blocks) so dependent services wait for SQL Server readiness rather than crash-looping on startup.
- This Compose file is the reference environment used for the "Exit Criteria" validation below — if it doesn't work in Compose, Phase 1 isn't done, even if it works from the IDE (§108).

---

## 10. Testing Strategy for Phase 1

- **IdentityService**: unit tests for token issuance/validation/rotation; integration test for full login → protected-call → refresh → logout cycle.
- **PatientService**: unit tests for search/pagination logic; integration test hitting a real (test) SQL Server instance.
- **Gateway**: integration test confirming unauthenticated requests to protected routes return 401, and requests with a token lacking the right role return 403.
- **Angular**: component tests for login form and patient search table states (loading/empty/error/populated); one end-to-end smoke test (Cypress/Playwright) covering login → search → view patient → logout.

---

## 11. Suggested Work Breakdown (solo dev, ~5–7 weeks)

| # | Task | Depends on |
|---|---|---|
| 1 | Repo/solution scaffolding, BuildingBlocks, Dockerfile template | — |
| 2 | SQL Server + Ocelot + health checks in Compose (all pointing at stub/empty services) | 1 |
| 3 | IdentityService backend (auth, JWT, roles, refresh, audit events) | 1 |
| 4 | Angular: login + auth state + route guards + HTTP interceptor | 3 |
| 5 | End-to-end validation of Slice 1 (login → protected route → logout) in Docker Compose | 2, 3, 4 |
| 6 | PatientService backend (search + detail) | 1 |
| 7 | Angular: patient search + patient detail + shell (header/sidebar) | 4, 6 |
| 8 | HccMappingService + GapEngineService stubs, Ocelot routes wired | 1 |
| 9 | Unauthorized/not-found pages, role-guard wiring | 4, 7 |
| 10 | Full exit-criteria pass (§12 below) in Compose, plus test suite | 5, 7, 8, 9 |

Do not start task 6+ before task 5 passes — this is the vertical-slice discipline from the project plan: prove Slice 1 completely before starting Slice 2.

---

## 12. Exit Criteria (expanded from §102)

An authorized user, running the app entirely through Docker Compose, can:

1. Open Angular and reach the login screen.
2. Authenticate through IdentityService with a seeded user.
3. Receive an access token and have it silently attached to subsequent API calls.
4. Navigate to a protected route (blocked pre-login, allowed post-login).
5. Have a route requiring a role they don't hold correctly show the Unauthorized page.
6. Search for a seeded synthetic patient by name/MRN and see paginated results (including a correct empty-state when no match).
7. Open a patient's detail view.
8. Log out and be unable to reach protected routes or call protected APIs afterward.
9. Receive a proper 401 (not authenticated) or 403 (not authorized) — never a silent failure or leaked stack trace — for invalid/insufficient requests.
10. All of the above works from a clean `docker compose up`, not only from IDE-run services.

---

## 13. Phase 1 Risks

| Risk | Why it matters | Mitigation |
|---|---|---|
| Treating auth as a quick add-on | JWT/refresh/RBAC correctness is foundational — every later phase assumes it works | Budget real time (task 3–5 above are the bulk of the phase); write the integration test for the full login/refresh/logout cycle before moving on |
| Downstream services trusting Ocelot blindly | Violates §95's explicit rule that the gateway is not the security boundary | Configure independent JWT validation in PatientService now, so the pattern is established before more services exist |
| Symmetric JWT secret chosen for convenience | Makes future OIDC/external-IdP integration (§94) harder to retrofit | Use RS256 from the start even though Phase 1 has no external IdP yet |
| Skipping the audit-event table because "nothing to audit yet" | Retrofitting audit trails later is expensive (per Project Plan §6 cross-phase workstreams) | Log `LoginSucceeded`/`LoginFailed`/`Logout`/`TokenRefreshed` from day one, even with no consumer of that data yet |
| Over-building user management (self-service registration, password reset UI) | Not required for exit criteria; steals time from the core slice | Seed users via migration/admin endpoint only; defer self-service flows |

---

## 14. Phase 1 Deliverables Checklist (subset of §117)

**Foundation**
- [ ] Repository structure
- [ ] .NET solution structure (Clean Architecture)
- [ ] BuildingBlocks
- [ ] Dockerfiles
- [ ] Docker Compose
- [ ] SQL Server
- [ ] Health checks
- [ ] OpenAPI

**Identity**
- [ ] IdentityService
- [ ] JWT (RS256, access + refresh)
- [ ] Authentication
- [ ] RBAC (6 seeded roles)
- [ ] Claims
- [ ] Angular route guards
- [ ] HTTP interceptor

**UI**
- [ ] Login
- [ ] Dashboard (shell only)
- [ ] Patient search
- [ ] Patient details (demographics only)

Everything else in §117 (clinical data ingestion, messaging, search, HCC/RAF, AI, most of security/research/operations) belongs to later phases and should not be pulled forward.
