# ARIS — Phase 1 Technical Documentation

**Document type:** Technical Documentation (how the system is built — architecture, data, APIs, security, deployment)
**Companion documents:**
- `ARIS — Technical Documentation.md` (in `Documentations/Holy Grail/`) — whole-project architecture; this document is its Phase 1 drill-down and must stay consistent with it
- `ARIS — Phase 1 Functional Requirements.md` — what the system must do (FR-x.x IDs referenced throughout this document)
- `ARIS — Phase 1 Detailed Plan.md` — execution plan (work breakdown, effort estimates, phase-specific risks); this document supersedes it as the authoritative technical design reference and narrows scope to design only, not scheduling
- `ARIS — Phase 1 Test Documentation.md` — how this design is verified (unit/integration/E2E)
- `ARIS — Phase 1 UI Guidelines.md` — visual design system, component specs, and the clickable mockup for §6 (Angular Technical Design)
**Source:** `ARIS — Complete Implementation and User Reference Documentation.md` v2.0, §21, §55, §93–§96, §102, §108–§111
**Status:** Draft — Phase 1 technical baseline

This document specifies the system design for Phase 1: architecture, component responsibilities, data model, API contracts, security mechanics, and deployment topology. It does not contain effort estimates, task sequencing, or risk registers — those live in the Detailed Plan.

---

## 1. Architecture Overview

### 1.1 Component diagram

```text
                        ┌────────────┐
                        │ Angular UI │
                        └─────┬──────┘
                              │ HTTPS, JWT bearer
                              ▼
                     ┌─────────────────┐
                     │ Ocelot Gateway  │
                     └──┬───────────┬──┘
                        │ HTTP      │ HTTP
                        ▼           ▼
              ┌─────────────────┐ ┌────────────────┐
              │ IdentityService │ │ PatientService │
              └────────┬────────┘ └───────┬────────┘
                       │ EF Core           │ EF Core
                       ▼                   ▼
                 ┌───────────┐      ┌───────────┐
                 │ IdentityDb│      │ PatientDb │
                 └───────────┘      └───────────┘

              ┌────────────────────┐   ┌──────────────────┐
              │ HccMappingService  │   │ GapEngineService  │   (stubs — static/empty
              │ (static responses) │   │ (empty responses) │    responses only)
              └────────────────────┘   └───────────────────┘
```

All inter-service and UI-to-gateway communication in Phase 1 is synchronous HTTP/REST (§110). No message broker, search engine, or vector store is introduced this phase.

### 1.2 Component responsibilities

| Component | Responsibility | Owns |
|---|---|---|
| Angular UI | Login, navigation, patient search/detail rendering, client-side route/role guarding | No persistent state beyond the browser session |
| Ocelot Gateway | Single entry point, request routing, header/correlation-ID forwarding | No business data |
| IdentityService | Authentication, token issuance, role/claims management, user management (create/list/deactivate/reactivate/bulk-import/admin-reset), self-service password reset, forced-password-change enforcement, auth audit events | `IdentityDb` |
| PatientService | Patient demographic storage and read access | `PatientDb` |
| HccMappingService (stub) | Proves routing/health pattern only | Nothing persistent |
| GapEngineService (stub) | Proves routing/health pattern only | Nothing persistent |
| BuildingBlocks | Shared library (not a running service) consumed by every backend service | N/A |

Per §111, no service reaches into another service's database. `IdentityService` and `PatientService` communicate only through their public HTTP APIs (in Phase 1, they don't need to call each other at all — Ocelot routes each request to exactly one service).

### 1.3 .NET Solution Structure

This is the fixed convention every backend service follows, set by the first service scaffolded (`IdentityService`) and mirrored exactly by every service added afterward — in Phase 1 and in every later phase. Grouping is by *concern* (`src/`, `tests/`) at the top level, with one subfolder per service under `src/Services/`, and four separate projects per service enforcing Clean Architecture's dependency direction at compile time, not just by convention.

```text
ARIS/
├── ARIS.sln
├── docker-compose.yml
├── .env
├── src/
│   ├── BuildingBlocks/
│   │   └── ARIS.BuildingBlocks/                    # shared, non-running library (§3.3, Detailed Plan §3)
│   ├── Services/
│   │   ├── IdentityService/
│   │   │   ├── ARIS.IdentityService.Domain/         # entities (User, Role, RefreshToken, ...); zero project references
│   │   │   ├── ARIS.IdentityService.Application/     # use cases, interfaces (e.g. IUserRepository); references Domain
│   │   │   ├── ARIS.IdentityService.Infrastructure/  # EF Core, JWT issuance; implements Application's interfaces
│   │   │   └── ARIS.IdentityService.Api/             # controllers, Program.cs, Dockerfile — composition root
│   │   ├── PatientService/
│   │   │   ├── ARIS.PatientService.Domain/
│   │   │   ├── ARIS.PatientService.Application/
│   │   │   ├── ARIS.PatientService.Infrastructure/
│   │   │   └── ARIS.PatientService.Api/
│   │   ├── HccMappingService/                        # stub — same 4-layer shape, thin, not collapsed
│   │   │   ├── ARIS.HccMappingService.Domain/
│   │   │   ├── ARIS.HccMappingService.Application/
│   │   │   ├── ARIS.HccMappingService.Infrastructure/
│   │   │   └── ARIS.HccMappingService.Api/
│   │   └── GapEngineService/                         # stub, same shape
│   │       ├── ARIS.GapEngineService.Domain/
│   │       ├── ARIS.GapEngineService.Application/
│   │       ├── ARIS.GapEngineService.Infrastructure/
│   │       └── ARIS.GapEngineService.Api/
│   └── Gateway/
│       └── ARIS.Gateway/                             # Ocelot config + Dockerfile
├── apps/
│   └── aris-web/                                     # Angular — tree fixed separately in §6.1
└── tests/
    ├── ARIS.IdentityService.UnitTests/
    ├── ARIS.IdentityService.IntegrationTests/
    ├── ARIS.PatientService.UnitTests/
    └── ARIS.PatientService.IntegrationTests/
```

**Naming**: `ARIS.<ServiceName>.<Layer>` for every project — predictable and greppable, matches the `ARIS —` prefix already used across the documentation set.

**Reference direction**: `Api → Infrastructure → Application → Domain`. `Domain` has no project references at all. `Application` defines the interfaces `Infrastructure` implements (e.g., `IUserRepository`), so `Application` never references `Infrastructure`. `Api` is the composition root — the only layer wiring concrete `Infrastructure` implementations into DI — and the only layer that references `BuildingBlocks`' ASP.NET-specific middleware directly; `Domain`/`Application` may still use `BuildingBlocks`' framework-agnostic types (`Result<T>`, `BaseEntity`).

**Stubs keep the full 4-layer shape** (`HccMappingService`, `GapEngineService`) — thin `Domain`/`Application` layers, but present. This is what makes the "stubs still go through the whole scaffold checklist" rule in the Detailed Plan mechanical rather than a per-service judgment call about how much structure to collapse.

**Dockerfile placement**: one multi-stage Dockerfile per service, living at `src/Services/<Service>/ARIS.<Service>.Api/Dockerfile`, with the Docker build context set to the repository root so it can `COPY` both `BuildingBlocks` and the service's own layer projects (§7.1).

**Tests**: `tests/` mirrors `src/Services/` by service, split into `<Service>.UnitTests` and `<Service>.IntegrationTests` per service — matches the testing strategy in §10 of the Detailed Plan without introducing a second test-layout convention.

Every later-phase service (`DataIngestService`/Indexer in Phase 2, `RafCalculationService` in Phase 3, Embedding Worker/Agent Orchestrator in Phase 4, Analytics/Audit Processor in Phase 5) is added under `src/Services/` following this exact same shape — this section is the authoritative reference for that, not something each phase's Technical Documentation re-derives.

---

## 2. Technology Stack

| Layer | Technology |
|---|---|
| Frontend | Angular, TypeScript |
| API Gateway | Ocelot (.NET) |
| Backend services | ASP.NET Core (.NET), Clean Architecture layering per service |
| ORM / persistence | Entity Framework Core |
| Database | SQL Server (one instance in Phase 1, one database per service) |
| Auth tokens | JWT, RS256 signing |
| Containerization | Docker, Docker Compose (local/dev), Docker Hub (image registry) |
| API documentation | OpenAPI/Swagger per service |

---

## 3. Data Design

### 3.1 IdentityDb

```text
User
 - Id                UNIQUEIDENTIFIER  PK
 - Username           NVARCHAR(256)     UNIQUE, NOT NULL
 - Email               NVARCHAR(256)     UNIQUE, NOT NULL
 - PasswordHash        NVARCHAR(MAX)     NOT NULL   (Argon2id or BCrypt output; self-salting)
 - DisplayName         NVARCHAR(256)     NOT NULL
 - IsActive            BIT               NOT NULL DEFAULT 1
 - MustChangePassword  BIT               NOT NULL DEFAULT 0   (set by an administrator-initiated reset, FR-6.16)
 - CreatedAtUtc        DATETIME2         NOT NULL
 - ModifiedAtUtc       DATETIME2         NOT NULL

Role
 - Id                  INT               PK
 - Name                NVARCHAR(64)      UNIQUE, NOT NULL
                        -- seeded: Administrator, Clinician, Coder, RiskAnalyst, Auditor, Researcher

UserRole
 - UserId              UNIQUEIDENTIFIER  FK -> User.Id
 - RoleId              INT               FK -> Role.Id
 - PRIMARY KEY (UserId, RoleId)

RefreshToken
 - Id                  UNIQUEIDENTIFIER  PK
 - UserId               UNIQUEIDENTIFIER  FK -> User.Id
 - TokenHash            NVARCHAR(MAX)     NOT NULL   (raw token never stored)
 - ExpiresAtUtc         DATETIME2         NOT NULL
 - RevokedAtUtc         DATETIME2         NULL
 - ReplacedByTokenId    UNIQUEIDENTIFIER  NULL       (rotation chain)
 - CreatedAtUtc         DATETIME2         NOT NULL

AuthAuditEvent
 - Id                  UNIQUEIDENTIFIER  PK
 - UserId               UNIQUEIDENTIFIER  NULL       (nullable — failed logins may not resolve)
 - EventType            NVARCHAR(32)      NOT NULL   (LoginSucceeded | LoginFailed | Logout | TokenRefreshed | TokenRevoked |
                                                        UserCreated | UserDeactivated | UserReactivated | UserRolesChanged |
                                                        PasswordResetRequested | PasswordResetCompleted | BulkImportCompleted |
                                                       PasswordAdminReset | ForcedPasswordChangeCompleted)
 - ActorUserId          UNIQUEIDENTIFIER  NULL       (the Administrator performing a user-management action; NULL for self-service events like login/password reset)
 - TimestampUtc         DATETIME2         NOT NULL
 - IpAddress            NVARCHAR(64)      NULL
 - CorrelationId        NVARCHAR(64)      NULL

PasswordResetToken
 - Id                  UNIQUEIDENTIFIER  PK
 - UserId               UNIQUEIDENTIFIER  FK -> User.Id
 - TokenHash            NVARCHAR(MAX)     NOT NULL   (raw token never stored, same pattern as RefreshToken)
 - ExpiresAtUtc         DATETIME2         NOT NULL   (short-lived — target 30-60 min)
 - UsedAtUtc            DATETIME2         NULL       (set once consumed; a used token is never valid again)
 - CreatedAtUtc         DATETIME2         NOT NULL
```

Indexes: `User.Username` (unique), `User.Email` (unique), `RefreshToken.TokenHash` (unique), `AuthAuditEvent.UserId + TimestampUtc` (composite, for future audit queries), `PasswordResetToken.TokenHash` (unique).

Bulk import (FR-6.12/FR-6.13) is processed synchronously per request in Phase 1 — no persisted job/queue entity. The per-row result is returned directly in the API response (§4.1) and a single `BulkImportCompleted` audit event records the summary (row count, success count) with the correlation ID linking back to the request; this is sufficient at Phase 1 volumes and avoids introducing async job infrastructure before Phase 2 brings a real message broker (§56 of the whole-project Technical Documentation).

### 3.2 PatientDb

```text
Patient
 - Id                  UNIQUEIDENTIFIER  PK
 - Mrn                 NVARCHAR(64)      UNIQUE, NOT NULL
 - FirstName            NVARCHAR(128)     NOT NULL
 - LastName             NVARCHAR(128)     NOT NULL
 - DateOfBirth          DATE              NOT NULL
 - Sex                  NVARCHAR(16)      NOT NULL
 - SourceSystem         NVARCHAR(64)      NOT NULL   (provenance seed per §20; e.g. "SYNTHETIC")
 - CreatedAtUtc         DATETIME2         NOT NULL
 - ModifiedAtUtc        DATETIME2         NOT NULL
```

Indexes: `Patient.Mrn` (unique), composite index on `LastName, FirstName` to support name search.

No `Encounter`, `Diagnosis`, `Procedure`, `Note`, or `Claim` tables in Phase 1 — those arrive with `DataIngestService` in Phase 2 (§21, §103), even though the canonical model anticipates them.

### 3.3 Shared entity conventions (BuildingBlocks)

Every entity in every service inherits:

```text
BaseEntity
 - Id             (type varies by entity)
 - CreatedAtUtc    DATETIME2
 - CreatedBy       NVARCHAR(256)   -- actor identifier, seeds future audit trail
 - ModifiedAtUtc   DATETIME2
 - ModifiedBy      NVARCHAR(256)
```

---

## 4. API Design

All routes below are exposed at the gateway under the prefixes shown; the gateway strips/rewrites to each service's internal listen path per its Ocelot configuration.

### 4.1 IdentityService

**`POST /identity/login`** — Authenticate
```json
// Request
{ "username": "string", "password": "string" }

// 200 Response
{
  "accessToken": "jwt-string",
  "refreshToken": "opaque-string",
  "expiresInSeconds": 1800,
  "user": { "id": "guid", "displayName": "string", "roles": ["Clinician"] },
  "mustChangePassword": false
}

// 401 Response (invalid credentials — generic per FR-1.2)
{ "type": "...", "title": "Invalid credentials", "status": 401 }
```
A deactivated account's login attempt returns the identical 401 above — never a distinct "account deactivated" message, which would leak account status to whoever is attempting the login (same anti-enumeration principle as FR-6.11).

**`POST /identity/refresh`** — Exchange refresh token
```json
// Request
{ "refreshToken": "opaque-string" }

// 200 Response — same shape as login; old refresh token is revoked, new one returned (rotation)
```

**`POST /identity/logout`** — Revoke current refresh token (requires bearer token)
→ `204 No Content`

**`GET /identity/me`** — Current user profile (requires bearer token)
```json
{ "id": "guid", "displayName": "string", "email": "string", "roles": ["Clinician"] }
```

**`POST /identity/users`** — Create user (Administrator only)
```json
// Request
{ "username": "string", "email": "string", "password": "string", "displayName": "string", "roles": ["Coder"] }
// 201 Response: created user summary (no password/hash returned). New accounts are IsActive=1 immediately (FR-6.5) — no separate activation step in Phase 1.
// 409 Response — username or email already in use (FR-6.4): { "type": "...", "title": "Username or email already in use", "status": 409 }
```

**`GET /identity/users/{id}`** — Get user (Administrator only) → user summary (FR-6.3)

**`PUT /identity/users/{id}/roles`** — Replace an existing user's role assignment (Administrator only, FR-6.2)
```json
// Request
{ "roles": ["Coder", "RiskAnalyst"] }
// 200 Response: updated user summary. Existing sessions keep their current token's roles until that token expires/refreshes (§5.1) — the change takes effect on next login or token refresh, not retroactively on an already-issued access token.
```

**`GET /identity/users?query={string}&page={int}&pageSize={int}`** — List/browse users (Administrator only, FR-6.7)
```json
// 200 Response — same paginated shape as PatientService's search (§4.2), for UI consistency
{
  "items": [
    { "id": "guid", "username": "string", "email": "string", "displayName": "string", "roles": ["Coder"], "isActive": true }
  ],
  "page": 1, "pageSize": 20, "totalCount": 0
}
```
`query` is optional (matches username/email/display name); an empty `items` array with `totalCount: 0` represents "no accounts match," exactly mirroring the empty-state contract in `GET /patients` (FR-6.7 explicitly ties this UI back to the FR-4.2–FR-4.5 patterns).

**`POST /identity/users/{id}/deactivate`** — Deactivate a user (Administrator only, FR-6.8)
→ `204 No Content`. Sets `IsActive=0`, revokes every non-expired `RefreshToken` for that user (bulk `RevokedAtUtc` update), and records a `UserDeactivated` audit event with `ActorUserId` set to the acting Administrator. A `409` is returned if the target account is already inactive.

**`POST /identity/users/{id}/reactivate`** — Reactivate a user (Administrator only, FR-6.9)
→ `204 No Content`. Sets `IsActive=1`, records `UserReactivated`. Does not restore any previously revoked refresh token — the user must log in fresh, which is the correct behavior (§5.1's normal login flow issues new tokens).

**`POST /identity/password-reset/request`** — Request a password reset (anonymous, FR-6.10/FR-6.11)
```json
// Request
{ "usernameOrEmail": "string" }
// 200 Response — identical whether or not the account exists (FR-6.11):
{ "message": "If an account exists for that username or email, a password reset link has been sent." }
```
If the account exists and is active, a `PasswordResetToken` is created (short-lived, single-use) and the reset link is dispatched through whatever out-of-band channel Phase 1 wires up (e.g., a logged/emailed link — delivery mechanism is an implementation detail, not specified here). A `PasswordResetRequested` audit event is recorded only when a matching account exists; the response is identical either way.

**`POST /identity/password-reset/confirm`** — Complete a password reset (anonymous, token-bearing, FR-6.10)
```json
// Request
{ "token": "opaque-string", "newPassword": "string" }
// 200 Response: { "message": "Password updated. You can now sign in." }
// 400 Response — token invalid, expired, or already used: { "type": "...", "title": "This reset link is no longer valid.", "status": 400 }
```
On success: `PasswordHash` is updated, the token's `UsedAtUtc` is set (single-use, per §3.1), every existing `RefreshToken` for that user is revoked (a password reset ends all other sessions, the same way a security-conscious reset should), and `PasswordResetCompleted` is recorded.

**`POST /identity/users/bulk-import`** — Bulk-create users from a file (Administrator only, FR-6.12/FR-6.13)
```json
// Request: multipart/form-data, one file field ("file"), CSV with columns username,email,displayName,roles (roles = pipe-separated, e.g. "Coder|RiskAnalyst")
// 200 Response — always 200 if the file itself parses; per-row outcomes carry the actual results:
{
  "totalRows": 12,
  "succeeded": 10,
  "failed": 2,
  "results": [
    { "row": 1, "username": "string", "status": "created", "userId": "guid" },
    { "row": 2, "username": "string", "status": "failed", "reason": "Username or email already in use" }
  ]
}
// 400 Response — file itself is malformed (wrong columns, unreadable) and no rows could be processed at all
```
Rows are processed independently (FR-6.13) — one bad row never blocks the others. A single `BulkImportCompleted` audit event summarizes the batch (`totalRows`/`succeeded`/`failed` and the correlation ID), rather than one event per created user (those are covered by the per-row `UserCreated` events, same as an individual `POST /identity/users` call would produce).

**`POST /identity/users/{id}/reset-password`** — Administrator-initiated password reset (Administrator only, FR-6.14/FR-6.15)
```json
// Request — the Administrator's own typed value; confirm-password matching is a client-side-only check
// (same pattern as POST /identity/change-password below), so only the single final value is sent:
{ "newPassword": "string" }
// 200 Response — deliberately does not echo the password back:
{ "message": "Password updated." }
// 400 Response — password fails basic validation (e.g., empty): { "type": "...", "title": "Enter a new password.", "status": 400 }
// 409 Response — target account is not active (FR-6.9 must run first): { "type": "...", "title": "Account is inactive — reactivate it before resetting the password.", "status": 409 }
```
Sets `PasswordHash` to a hash of the Administrator-supplied password, sets `MustChangePassword=1`, revokes every existing `RefreshToken` for that user (same as FR-6.8/FR-6.10), and records `PasswordAdminReset` with `ActorUserId` set to the acting Administrator. Unlike `POST /identity/password-reset/confirm` (which updates a password the *user* chose), this endpoint updates a password the *Administrator* chose on the user's behalf — that's exactly why FR-6.16's forced-change gate exists: it is the mechanism that keeps the Administrator from continuing to know the account's real password past the next login.

**`POST /identity/change-password`** — Set a new password while authenticated (any authenticated user, acts on self only, FR-6.16)
```json
// Request
{ "newPassword": "string" }
// 200 Response: { "message": "Password updated." }
```
Updates `PasswordHash` and clears `MustChangePassword`. This is the only endpoint (besides `GET /identity/me`, `POST /identity/logout`, and `POST /identity/refresh`) a user with `MustChangePassword=1` is permitted to call — see §5.2 for how that gate is enforced.

**`GET /health/live`**, **`GET /health/ready`** → `200 OK` / `503 Service Unavailable`

### 4.2 PatientService

**`GET /patients?query={string}&page={int}&pageSize={int}`** — Search (roles: Clinician, Coder, RiskAnalyst, Administrator)
```json
// 200 Response
{
  "items": [
    { "id": "guid", "mrn": "string", "firstName": "string", "lastName": "string", "dateOfBirth": "date" }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 0
}
```
Empty `items` with `totalCount: 0` represents "no results" (FR-4.3) — distinct from a non-2xx response, which represents a failure (FR-4.5).

**`GET /patients/{id}`** — Detail (same roles)
```json
// 200 Response
{ "id": "guid", "mrn": "string", "firstName": "string", "lastName": "string", "dateOfBirth": "date", "sex": "string" }

// 404 Response — patient not found
```

**`GET /health/live`**, **`GET /health/ready`**

### 4.3 HccMappingService (stub)

**`GET /hcc/models`** → `200 OK`, hardcoded static array. **`GET /health/live`**, **`GET /health/ready`**.

### 4.4 GapEngineService (stub)

**`GET /patients/{id}/gaps`** → `200 OK`, always `[]` in Phase 1. **`GET /health/live`**, **`GET /health/ready`**.

### 4.5 Standard error shape (all services)

Every non-2xx response uses RFC 7807 problem-details:

```json
{ "type": "https://...", "title": "string", "status": 400, "detail": "string", "traceId": "correlation-id" }
```

No stack traces, connection strings, or internal identifiers are ever included (NFR-4).

---

## 5. Security Design

### 5.1 Authentication mechanics

- Access token: JWT, RS256-signed, short-lived (target 15–30 min). Claims: `sub` (user id), `roles` (array), `name`, standard `iat`/`exp`/`iss`/`aud`.
- This elapsed-time expiry (access token lifetime, refresh token lifetime, and silent refresh in §6.3/§8.2) is what satisfies FR-1.3 in full — Phase 1 has no separate real-time idle/inactivity timer (§11).
- Refresh token: opaque random value, stored server-side only as a hash, long-lived, single-use with rotation (each refresh revokes the presented token and issues a new one; reuse of a revoked token revokes the entire chain for that user as a compromise signal).
- RS256 (asymmetric) is used specifically so a public key can validate tokens without sharing the private signing key — this keeps the door open to an external OIDC provider in a later phase without redesigning token validation in every service (§94).
- Signing key and connection strings are supplied via environment variables / Docker secrets in Phase 1, never committed to source control. A managed Secrets Manager is a Phase 6 concern.
- Several administrative/security actions explicitly revoke a user's outstanding refresh tokens rather than merely relying on their normal expiry: deactivation (FR-6.8), self-service password-reset completion (FR-6.10), and administrator password reset (FR-6.14). Each bulk-updates `RefreshToken.RevokedAtUtc` for that `UserId` as part of the same request, so access doesn't linger on an already-issued token after any of them.

### 5.2 Authorization mechanics (RBAC only — §95)

- Each protected endpoint declares its allowed role(s) via ASP.NET Core authorization policy (e.g., `[Authorize(Roles = "Clinician,Coder,RiskAnalyst,Administrator")]`), evaluated from the JWT's `roles` claim.
- **Every backend service validates the JWT independently** (signature, expiry, issuer/audience) — the gateway forwarding a token is not treated as validation. This directly implements FR-2.4 and the explicit rule in §95 that Angular/gateway is never the sole security boundary.
- Ocelot forwards the `Authorization` header unmodified and does not perform authorization decisions itself in Phase 1 (routing only); each downstream service is the actual enforcement point.
- No patient-level or attribute-based restriction exists in Phase 1 (matches FR-4.6/FR-5's stated scope boundary) — any user holding an authorized role can access any patient record. ABAC and patient-level scoping are explicitly deferred (§95).
- **Forced-password-change gate (FR-6.16):** a small piece of middleware runs after JWT validation, on every request, in every service — if the authenticated user's `MustChangePassword` claim/flag is `true`, the request is rejected with `403` and a distinct problem-details `type` (e.g. `password-change-required`) unless the route is allow-listed (`POST /identity/change-password`, `GET /identity/me`, `POST /identity/logout`, `POST /identity/refresh`). This is enforced identically in every backend service, not only IdentityService, so a user stuck in this state cannot reach `PatientService` or any other API by going around IdentityService. The Angular interceptor (§6.3) treats this specific error type as a hard redirect to the mandatory change-password screen, distinct from how it treats a normal 403 (which routes to the Unauthorized page, §6.2).

### 5.3 RBAC matrix (Phase 1 endpoints)

| Endpoint | Administrator | Clinician | Coder | RiskAnalyst | Auditor | Researcher |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| `POST /identity/login` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `POST /identity/refresh` | anonymous (token-bearing) | anonymous (token-bearing) | anonymous (token-bearing) | anonymous (token-bearing) | anonymous (token-bearing) | anonymous (token-bearing) |
| `GET /identity/me` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `POST /identity/logout` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `POST /identity/users` | ✓ | – | – | – | – | – |
| `GET /identity/users` (list) | ✓ | – | – | – | – | – |
| `GET /identity/users/{id}` | ✓ | – | – | – | – | – |
| `PUT /identity/users/{id}/roles` | ✓ | – | – | – | – | – |
| `POST /identity/users/{id}/deactivate` | ✓ | – | – | – | – | – |
| `POST /identity/users/{id}/reactivate` | ✓ | – | – | – | – | – |
| `POST /identity/users/bulk-import` | ✓ | – | – | – | – | – |
| `POST /identity/users/{id}/reset-password` | ✓ | – | – | – | – | – |
| `POST /identity/change-password` | ✓ (self) | ✓ (self) | ✓ (self) | ✓ (self) | ✓ (self) | ✓ (self) |
| `POST /identity/password-reset/request` | anonymous | anonymous | anonymous | anonymous | anonymous | anonymous |
| `POST /identity/password-reset/confirm` | anonymous (token-bearing) | anonymous (token-bearing) | anonymous (token-bearing) | anonymous (token-bearing) | anonymous (token-bearing) | anonymous (token-bearing) |
| `GET /patients` (search) | ✓ | ✓ | ✓ | ✓ | – | – |
| `GET /patients/{id}` | ✓ | ✓ | ✓ | ✓ | – | – |

Auditor and Researcher can authenticate in Phase 1 but have no functional area yet beyond their own profile and (per §3.6 of the Functional Requirements) resetting their own password — this is intentional (matches the Functional Requirements doc's actor table). The password-reset endpoints, and `POST /identity/refresh`, are pre-authentication by nature (an anonymous, not-yet-logged-in caller must be able to reach them, presenting a refresh/reset token rather than an access token) and are protected instead by the token-possession and single-use/expiry/rotation mechanics in §5.1/§3.1, not by a role check.

### 5.4 Logging and PHI handling

- Structured logging via a shared BuildingBlocks logger wrapper; PHI-shaped fields (name, DOB, MRN) are excluded from log messages by convention — logs reference entity IDs, not identifying values (§51).
- Every request carries a correlation ID, generated at the gateway if absent, propagated to every downstream call and included in problem-details error responses (`traceId`) and audit events.

---

## 6. Angular Technical Design

### 6.1 Module/folder structure

```text
apps/aris-web/
 ├── core/
 │    ├── auth/            AuthService (login/refresh/logout, token storage)
 │    ├── interceptors/    AuthInterceptor (attaches bearer token, handles 401→refresh),
 │    │                    ErrorInterceptor (maps problem-details to UI error state)
 │    ├── guards/          AuthGuard, RoleGuard
 │    └── layout/          ShellComponent (header, sidebar, router-outlet)
 ├── features/
 │    ├── login/           LoginComponent
 │    ├── forgot-password/ ForgotPasswordComponent, ResetPasswordComponent
 │    ├── change-password/ ForcedChangePasswordComponent (mandatory screen after logging in with an administrator-set password)
 │    ├── dashboard/       DashboardComponent (placeholder)
 │    ├── patients/
 │    │    ├── patient-search/    PatientSearchComponent
 │    │    └── patient-detail/    PatientDetailComponent
 │    └── users/
 │         ├── user-list/         UserListComponent (list + deactivate/reactivate/reset-password actions)
 │         └── user-bulk-import/  UserBulkImportComponent
 └── shared/                EmptyStateComponent, LoadingComponent, PaginatorComponent, ErrorStateComponent
```

`users/` reuses the same list/pagination/empty/error state pattern as `patients/patient-search` (§6.4) rather than inventing a second one — this is why FR-6.7 explicitly ties the user list's states back to FR-4.2–FR-4.5.

### 6.2 Routing table

| Path | Component | Guard(s) |
|---|---|---|
| `/login` | LoginComponent | none (redirects away if already authenticated) |
| `/forgot-password` | ForgotPasswordComponent | none |
| `/reset-password` | ResetPasswordComponent | none (reads the reset token from a query param, e.g. `?token=...`) |
| `/change-password` | ForcedChangePasswordComponent | AuthGuard only (deliberately no RoleGuard — every role can land here) |
| `/` | DashboardComponent | AuthGuard, MustChangePasswordGuard |
| `/patients` | PatientSearchComponent | AuthGuard, MustChangePasswordGuard, RoleGuard(Clinician,Coder,RiskAnalyst,Administrator) |
| `/patients/:id` | PatientDetailComponent | AuthGuard, MustChangePasswordGuard, RoleGuard(same) |
| `/admin/users` | UserListComponent | AuthGuard, MustChangePasswordGuard, RoleGuard(Administrator) |
| `/admin/users/import` | UserBulkImportComponent | AuthGuard, MustChangePasswordGuard, RoleGuard(Administrator) |
| `/unauthorized` | UnauthorizedComponent | none |
| `**` | NotFoundComponent | none |

`MustChangePasswordGuard` reads the `mustChangePassword` flag from the current session (set at login, §8.1) and redirects to `/change-password` if true — this is the frontend half of FR-6.16; the backend gate in §5.2 is what makes it non-bypassable.

### 6.3 Token handling

- Access token held in memory (application state), not `localStorage`, to reduce XSS exfiltration surface.
- Refresh token handling: prefer an httpOnly, secure cookie set by the backend if the gateway/browser topology supports it in the dev environment; if not feasible in Phase 1, use a clearly-flagged interim storage approach with a follow-up noted for hardening — this trade-off should be revisited before Phase 6 security hardening, not left implicit.
- `AuthInterceptor` attaches `Authorization: Bearer <accessToken>` to every outgoing API request; on a `401`, it attempts exactly one silent refresh, retries the original request once, and routes to `/login` on repeated failure.
- Since refresh tokens are single-use with rotation (§5.1), concurrent requests that all hit `401` at once must not each independently call `/identity/refresh` — only the first triggers the call; subsequent 401s in flight during that window wait on its result and retry with the resulting token. Implemented as a shared in-flight refresh observable in `AuthInterceptor`, not a per-request refresh call.
- `ErrorInterceptor` treats a `403` carrying the `password-change-required` problem-details `type` (§5.2) as a hard redirect to `/change-password`, distinct from a normal `403` (which routes to `/unauthorized`) — this is what keeps a user stuck in the forced-change state even if `MustChangePasswordGuard` were somehow bypassed client-side.

### 6.4 State handling for lists (search)

`PatientSearchComponent` models four explicit UI states — `idle`, `loading`, `error`, `results` (where `results` includes the zero-results case, rendered via `EmptyStateComponent`) — matching FR-4.3/FR-4.4/FR-4.5's requirement that these states never be conflated.

---

## 7. Deployment Design

### 7.1 Docker Compose topology

```text
docker-compose.yml
 ├── sqlserver              (one instance; IdentityDb + PatientDb as separate databases)
 ├── identity-service        depends_on: sqlserver (healthy)
 ├── patient-service         depends_on: sqlserver (healthy)
 ├── hcc-mapping-service     depends_on: none (stub)
 ├── gap-engine-service      depends_on: none (stub)
 ├── ocelot-gateway          depends_on: identity-service, patient-service, hcc-mapping-service, gap-engine-service
 └── aris-web                depends_on: ocelot-gateway
```

- Each backend service: multi-stage Dockerfile (SDK build stage → ASP.NET runtime stage).
- `aris-web`: build stage compiles Angular, runtime stage serves via a lightweight static server (e.g., nginx), configured with the gateway's base URL injected at build/deploy time.
- Compose `healthcheck:` blocks target each service's `/health/ready`; dependent services use `depends_on: condition: service_healthy` rather than a fixed startup delay.

### 7.2 Environment configuration

| Variable | Consumed by | Purpose |
|---|---|---|
| `SQLSERVER_SA_PASSWORD` | sqlserver, all services | DB auth |
| `IDENTITY_DB_CONNECTION` | identity-service | Connection string |
| `PATIENT_DB_CONNECTION` | patient-service | Connection string |
| `JWT_SIGNING_KEY` (private) | identity-service | Token signing |
| `JWT_PUBLIC_KEY` | patient-service, gap-engine-service, hcc-mapping-service | Token validation |
| `GATEWAY_BASE_URL` | aris-web (build-time) | API base URL for the frontend |
| `PASSWORD_RESET_LINK_BASE_URL` | identity-service | Base URL used to build the reset link embedded in a `PasswordResetToken` (e.g., `https://<gateway>/reset-password`) |

All secrets are supplied via `.env`/Compose secrets, never baked into images or committed to source control.

**Password reset delivery in Phase 1**: no real email/SMTP integration is built this phase — `identity-service` logs the generated reset link (structured log, not console noise) rather than sending it through a real channel. This is a deliberate, stated simplification: it proves the full token-issuance/expiry/single-use mechanics (FR-6.10/FR-6.11) end-to-end without standing up email infrastructure before it's otherwise needed. Wiring a real transactional-email provider is a follow-up, not a Phase 1 gap to silently work around.

### 7.3 Image tagging

Local development uses `latest`; any image pushed to Docker Hub as part of Phase 1 validation should also carry an immutable tag (`aris/patient-service:0.1.0` or `git-<short-sha>`), per §109 — even though production deployment itself is out of scope until Phase 6.

---

## 8. Sequence Flows

### 8.1 Login

```text
Angular (LoginComponent)
  → POST /identity/login {username, password}       [Ocelot → IdentityService]
IdentityService
  → validate credentials, issue access+refresh token, record AuthAuditEvent(LoginSucceeded|LoginFailed)
  → 200 {accessToken, refreshToken, user, mustChangePassword}  or  401 {problem-details}
Angular
  → store accessToken in memory, refreshToken per §6.3
  → mustChangePassword=true  → navigate to "/change-password" (MustChangePasswordGuard would redirect here anyway)
  → mustChangePassword=false → navigate to "/"
```

### 8.2 Authenticated request with silent refresh

```text
Angular (any protected call)
  → AuthInterceptor attaches Authorization: Bearer <accessToken>
  → request → Ocelot → target service
target service
  → validates JWT independently (§5.2); 401 if invalid/expired
Angular (on 401)
  → AuthInterceptor calls POST /identity/refresh {refreshToken}
  → on success: retry original request once with new accessToken
  → on failure: clear session, navigate to /login
```

### 8.3 Patient search

```text
Angular (PatientSearchComponent)
  → GET /patients?query=...&page=...&pageSize=...    [Ocelot → PatientService]
PatientService
  → validates JWT + role (Clinician|Coder|RiskAnalyst|Administrator)
  → queries PatientDb, returns paginated result (possibly empty)
Angular
  → renders results / empty-state / error-state per §6.4
```

### 8.4 Deactivate a user

```text
Angular (UserListComponent)
  → POST /identity/users/{id}/deactivate              [Ocelot → IdentityService]
IdentityService
  → validates JWT + role (Administrator only)
  → sets User.IsActive=0, revokes all non-expired RefreshToken rows for that user,
    records AuthAuditEvent(UserDeactivated, ActorUserId=<admin>)
  → 204
Angular
  → row updates to "Inactive" in place (re-fetch or optimistic update, per §6.4's pattern)
```

### 8.5 Self-service password reset

```text
Angular (ForgotPasswordComponent)
  → POST /identity/password-reset/request {usernameOrEmail}   [Ocelot → IdentityService]
IdentityService
  → looks up account; if found and active, creates PasswordResetToken, logs the reset link (§7.2)
  → 200 {message} — identical response whether or not the account was found (FR-6.11)
Angular
  → shows the generic confirmation message, regardless of backend outcome

Angular (ResetPasswordComponent, opened via the reset link's ?token=...)
  → POST /identity/password-reset/confirm {token, newPassword}
IdentityService
  → validates token (unexpired, unused); 400 if not
  → updates PasswordHash, marks token UsedAtUtc, revokes all RefreshToken rows for that user,
    records AuthAuditEvent(PasswordResetCompleted)
  → 200 {message}
Angular
  → confirms success, navigates to /login
```

### 8.6 Administrator password reset, then forced change at next login

```text
Angular (UserListComponent)
  → Administrator enters newPassword + confirmPassword in the reset modal; client blocks submit until they match
  → POST /identity/users/{id}/reset-password {newPassword}   [Ocelot → IdentityService]
IdentityService
  → validates JWT + role (Administrator only); 409 if target account is inactive
  → hashes the supplied password into PasswordHash, sets MustChangePassword=1,
    revokes all RefreshToken rows for that user,
    records AuthAuditEvent(PasswordAdminReset, ActorUserId=<admin>)
  → 200 {message}  — never echoes the password back
Angular
  → shows a success state ("Password reset — they'll need to set a new password at next login"); the entered value is not redisplayed, only cleared from the form

--- later, the affected user logs in with the password the Administrator set ---

Angular (LoginComponent)
  → POST /identity/login {username, password}
IdentityService
  → 200 {..., mustChangePassword: true}
Angular
  → navigates to /change-password (MustChangePasswordGuard); every other route redirects here too

Angular (ForcedChangePasswordComponent)
  → POST /identity/change-password {newPassword}
IdentityService
  → validates JWT (any role; MustChangePassword=1 users may call this endpoint — see the allow-list in §5.2)
  → updates PasswordHash, sets MustChangePassword=0, records AuthAuditEvent(ForcedPasswordChangeCompleted)
  → 200 {message}
Angular
  → mustChangePassword now false on next profile check; navigates to "/"
```

### 8.7 Bulk user import

```text
Angular (UserBulkImportComponent)
  → POST /identity/users/bulk-import (multipart file)          [Ocelot → IdentityService]
IdentityService
  → validates JWT + role (Administrator only); parses file
  → for each row: validate → create user (or record failure reason), independently (FR-6.13)
  → records AuthAuditEvent(BulkImportCompleted, summary) once for the batch
  → 200 {totalRows, succeeded, failed, results[]}
Angular
  → renders a per-row result table (created / failed + reason), per FR-6.13
```

---

## 9. Observability (Phase 1 minimum)

- Structured logs (JSON) from every service, each entry carrying `correlationId`, `service`, `level`, `message`, and no PHI-shaped fields.
- `/health/live` and `/health/ready` on every service, used by Compose and (later) any orchestrator.
- No distributed tracing backend (OpenTelemetry collector, dashboards) is stood up in Phase 1 — the correlation-ID convention established here is what Phase 6 wires into full tracing (§73), so the propagation mechanism must exist now even though nothing visualizes it yet.

---

## 10. Traceability — Functional Requirements → Technical Design

| Functional requirement | Satisfied by |
|---|---|
| FR-1.1–FR-1.4 (Authentication) | §4.1 IdentityService endpoints, §5.1 token mechanics, §8.1/§8.2 sequence flows |
| FR-1.5 (Auth audit events) | §3.1 `AuthAuditEvent` table |
| FR-2.1–FR-2.4 (Authorization) | §5.2 authorization mechanics, §5.3 RBAC matrix, §6.2 route guards |
| FR-6.1, FR-6.4, FR-6.6 (Create user) | §4.1 `POST /identity/users`, §3.1 `User` unique `Username`/`Email` |
| FR-6.2 (Role assignment/change) | §4.1 `PUT /identity/users/{id}/roles` |
| FR-6.3 (Get user by id) | §4.1 `GET /identity/users/{id}` |
| FR-6.5 (New account usable immediately) | §4.1 `POST /identity/users` (`IsActive=1` default, §3.1) |
| FR-6.6 (Administrator-only enforcement, all user-management actions) | §5.3 RBAC matrix |
| FR-6.7 (List/browse users) | §4.1 `GET /identity/users`, §6.1/§6.2 `UserListComponent` and route |
| FR-6.8 (Deactivate) | §4.1 `POST /identity/users/{id}/deactivate`, §5.1 refresh-token revocation, §8.4 |
| FR-6.9 (Reactivate) | §4.1 `POST /identity/users/{id}/reactivate` |
| FR-6.10 (Self-service password reset) | §4.1 `POST /identity/password-reset/request` + `/confirm`, §3.1 `PasswordResetToken`, §8.5 |
| FR-6.11 (Anti-enumeration on reset request) | §4.1 identical response shape regardless of account existence |
| FR-6.12/FR-6.13 (Bulk import, per-row reporting) | §4.1 `POST /identity/users/bulk-import`, §8.7 |
| FR-6.14 (Administrator resets password directly) | §4.1 `POST /identity/users/{id}/reset-password`, §3.1 `MustChangePassword`, §8.6 |
| FR-6.15 (New/confirm match required; password never echoed back) | §6.11-style client-side match check (UI Guidelines), §4.1 response shape never includes the password |
| FR-6.16 (Forced password change before anything else) | §5.2 forced-change gate, §6.2 `MustChangePasswordGuard`, §4.1 `POST /identity/change-password`, §8.6 |
| FR-3.1–FR-3.4 (App shell/navigation) | §6.1 layout module, §6.2 routing table |
| FR-4.1–FR-4.5 (Patient search) | §4.2 `GET /patients`, §6.4 explicit UI states |
| FR-4.6 (Role-based, not patient-level, access) | §5.2 explicit scope boundary |
| FR-5.1–FR-5.4 (Patient details) | §4.2 `GET /patients/{id}`, §3.2 `Patient` entity (demographics only) |
| NFR-1/NFR-2 (Response feel) | §3.2 indexing (`Mrn`, `LastName+FirstName`) supports fast lookups; specific latency validation belongs to the test plan, not this document |
| NFR-3 (No cross-user/patient leakage) | §5.2 independent per-service JWT validation |
| NFR-4 (Safe error messages) | §4.5 standard problem-details shape |

---

## 11. Explicit Non-Goals for Phase 1 (technical)

To keep this document bounded to Phase 1, the following are deliberately absent from the design above and should not be added under this phase's implementation:

- Any message broker, outbox pattern, or event contracts (Phase 2, §56–§57)
- Any search index (OpenSearch) or vector store (Qdrant) (Phase 2 / Phase 4)
- Real HCC mapping or gap-detection logic (Phase 3, §104)
- LLM/agent integration of any kind (Phase 4, §105)
- ABAC, patient-level authorization, OIDC/external IdP integration, secrets manager, KMS (Phase 6, §107)
- Distributed tracing backend/dashboards (Phase 6, §107) — only the correlation-ID convention is established now
- Client-side idle/inactivity detection (mouse/keyboard activity tracking, an idle-timeout warning modal) — FR-1.3 is satisfied by elapsed-time token expiry + silent refresh (§5.1, §6.3, §8.2) alone; a real-time idle timer is not part of Phase 1's design
