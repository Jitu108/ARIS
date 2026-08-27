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
| IdentityService | Authentication, token issuance, role/claims management, auth audit events | `IdentityDb` |
| PatientService | Patient demographic storage and read access | `PatientDb` |
| HccMappingService (stub) | Proves routing/health pattern only | Nothing persistent |
| GapEngineService (stub) | Proves routing/health pattern only | Nothing persistent |
| BuildingBlocks | Shared library (not a running service) consumed by every backend service | N/A |

Per §111, no service reaches into another service's database. `IdentityService` and `PatientService` communicate only through their public HTTP APIs (in Phase 1, they don't need to call each other at all — Ocelot routes each request to exactly one service).

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
 - EventType            NVARCHAR(32)      NOT NULL   (LoginSucceeded | LoginFailed | Logout | TokenRefreshed | TokenRevoked)
 - TimestampUtc         DATETIME2         NOT NULL
 - IpAddress            NVARCHAR(64)      NULL
 - CorrelationId        NVARCHAR(64)      NULL
```

Indexes: `User.Username` (unique), `User.Email` (unique), `RefreshToken.TokenHash` (unique), `AuthAuditEvent.UserId + TimestampUtc` (composite, for future audit queries).

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
  "user": { "id": "guid", "displayName": "string", "roles": ["Clinician"] }
}

// 401 Response (invalid credentials — generic per FR-1.2)
{ "type": "...", "title": "Invalid credentials", "status": 401 }
```

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
// 201 Response: created user summary (no password/hash returned)
```

**`GET /identity/users/{id}`** — Get user (Administrator only) → user summary

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
- Refresh token: opaque random value, stored server-side only as a hash, long-lived, single-use with rotation (each refresh revokes the presented token and issues a new one; reuse of a revoked token revokes the entire chain for that user as a compromise signal).
- RS256 (asymmetric) is used specifically so a public key can validate tokens without sharing the private signing key — this keeps the door open to an external OIDC provider in a later phase without redesigning token validation in every service (§94).
- Signing key and connection strings are supplied via environment variables / Docker secrets in Phase 1, never committed to source control. A managed Secrets Manager is a Phase 6 concern.

### 5.2 Authorization mechanics (RBAC only — §95)

- Each protected endpoint declares its allowed role(s) via ASP.NET Core authorization policy (e.g., `[Authorize(Roles = "Clinician,Coder,RiskAnalyst,Administrator")]`), evaluated from the JWT's `roles` claim.
- **Every backend service validates the JWT independently** (signature, expiry, issuer/audience) — the gateway forwarding a token is not treated as validation. This directly implements FR-2.4 and the explicit rule in §95 that Angular/gateway is never the sole security boundary.
- Ocelot forwards the `Authorization` header unmodified and does not perform authorization decisions itself in Phase 1 (routing only); each downstream service is the actual enforcement point.
- No patient-level or attribute-based restriction exists in Phase 1 (matches FR-4.6/FR-5's stated scope boundary) — any user holding an authorized role can access any patient record. ABAC and patient-level scoping are explicitly deferred (§95).

### 5.3 RBAC matrix (Phase 1 endpoints)

| Endpoint | Administrator | Clinician | Coder | RiskAnalyst | Auditor | Researcher |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| `POST /identity/login` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `GET /identity/me` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `POST /identity/users` | ✓ | – | – | – | – | – |
| `GET /patients` (search) | ✓ | ✓ | ✓ | ✓ | – | – |
| `GET /patients/{id}` | ✓ | ✓ | ✓ | ✓ | – | – |

Auditor and Researcher can authenticate in Phase 1 but have no functional area yet beyond their own profile — this is intentional (matches the Functional Requirements doc's actor table).

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
 │    ├── dashboard/       DashboardComponent (placeholder)
 │    └── patients/
 │         ├── patient-search/    PatientSearchComponent
 │         └── patient-detail/    PatientDetailComponent
 └── shared/                EmptyStateComponent, LoadingComponent, PaginatorComponent, ErrorStateComponent
```

### 6.2 Routing table

| Path | Component | Guard(s) |
|---|---|---|
| `/login` | LoginComponent | none (redirects away if already authenticated) |
| `/` | DashboardComponent | AuthGuard |
| `/patients` | PatientSearchComponent | AuthGuard, RoleGuard(Clinician,Coder,RiskAnalyst,Administrator) |
| `/patients/:id` | PatientDetailComponent | AuthGuard, RoleGuard(same) |
| `/unauthorized` | UnauthorizedComponent | none |
| `**` | NotFoundComponent | none |

### 6.3 Token handling

- Access token held in memory (application state), not `localStorage`, to reduce XSS exfiltration surface.
- Refresh token handling: prefer an httpOnly, secure cookie set by the backend if the gateway/browser topology supports it in the dev environment; if not feasible in Phase 1, use a clearly-flagged interim storage approach with a follow-up noted for hardening — this trade-off should be revisited before Phase 6 security hardening, not left implicit.
- `AuthInterceptor` attaches `Authorization: Bearer <accessToken>` to every outgoing API request; on a `401`, it attempts exactly one silent refresh, retries the original request once, and routes to `/login` on repeated failure.

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

All secrets are supplied via `.env`/Compose secrets, never baked into images or committed to source control.

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
  → 200 {accessToken, refreshToken, user}  or  401 {problem-details}
Angular
  → store accessToken in memory, refreshToken per §6.3, navigate to "/"
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
| FR-2.5 (Role assignment) | §4.1 `POST /identity/users` |
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
