# ARIS — Phase 1 Test Documentation

**Document type:** Test Documentation (how Phase 1 is verified — unit, integration, end-to-end, and supporting test concerns)
**Companion documents:**
- `ARIS — Phase 1 Functional Requirements.md` — FR-x.x IDs, this document's test cases trace back to them
- `ARIS — Phase 1 Technical Documentation.md` — architecture/API/data design being tested
- `ARIS — Phase 1 Detailed Plan.md` — work breakdown and phase-specific risks
- `ARIS — Phase 1 UI Guidelines.md` — visual/interaction spec; E2E cases in §6 below verify its state patterns (loading/results/empty/error, role-based nav, etc.)
**Source:** `ARIS — Complete Implementation and User Reference Documentation.md` v2.0, §74 (performance targets), §90 (Definition of Done), §102 (exit criteria)
**Status:** Draft — Phase 1 test baseline

This document defines what "tested" means for Phase 1. It does not restate functional or technical design — it specifies test layers, tooling, concrete test cases, and the criteria under which Phase 1 is considered verified.

---

## 1. Test Strategy Overview

Phase 1 uses a standard test pyramid, weighted toward the base — most confidence should come from fast unit and integration tests, with end-to-end tests reserved for the handful of flows that matter because a user actually walks through them:

```text
                    ┌─────────────┐
                    │   E2E (UI)  │   few, slow, high-confidence for real user flows
                    ├─────────────┤
                    │ Integration │   moderate count, real DB, real HTTP pipeline
                    ├─────────────┤
                    │    Unit     │   many, fast, isolated logic
                    └─────────────┘
```

| Layer | Answers | Runs against |
|---|---|---|
| Unit | Is this piece of logic correct in isolation? | In-memory, mocked dependencies |
| Integration | Do this service's components work together correctly, including its real database and HTTP pipeline? | Real SQL Server instance (test container), real ASP.NET Core pipeline |
| End-to-end (E2E) | Does the full user journey work through the real, deployed system? | Full Docker Compose stack, real browser |

A test belongs to the lowest layer that can actually verify it. Don't write an E2E test for something a unit test already proves.

---

## 2. Test Environments

| Environment | Purpose | How it's stood up |
|---|---|---|
| Local dev | Fast unit test iteration | `dotnet test` / Angular test runner directly, no containers required |
| Integration test env | Backend integration tests against real infra | SQL Server via a disposable test container (e.g., Testcontainers), spun up and torn down per test run |
| Full-stack / E2E env | Full user-journey verification | `docker compose up` using the same Compose file defined in the Technical Documentation — this is deliberately the same environment used for local development, not a separate "test-only" config |
| CI | Automated gate on every change | Runs unit + integration on every push; E2E at minimum before marking Phase 1 complete, ideally on every push once stable |

Using the same Docker Compose file for E2E as for local dev is intentional — if E2E only ever passes against a special test-only configuration, it isn't actually proving the thing users will run.

---

## 3. Tooling

| Layer | Backend (.NET) | Frontend (Angular) |
|---|---|---|
| Unit | xUnit, FluentAssertions, Moq | Jest (via Angular CLI's Jest builder), Angular Testing Library (component harnesses) |
| Integration | xUnit + `WebApplicationFactory` (real HTTP pipeline in-process), Testcontainers for SQL Server | — |
| E2E | — | Playwright or Cypress against the Compose stack |
| Coverage | Coverlet (or built-in coverage collector) | Jest coverage reporter |

Test containers (not an in-memory EF provider) are used for backend integration tests specifically so EF Core query translation, constraints, and indexes are validated against real SQL Server behavior — an in-memory provider would hide bugs that only appear against the real engine.

---

## 4. Unit Test Plan

### 4.1 IdentityService

| ID | Test | Verifies |
|---|---|---|
| UT-ID-01 | Valid credentials produce a signed access token containing correct `sub`, `roles`, `exp` claims | Token issuance logic |
| UT-ID-02 | Invalid password produces an authentication failure without revealing which check failed | FR-1.2 |
| UT-ID-03 | Password hash verification correctly accepts a matching password and rejects a non-matching one | Credential storage correctness |
| UT-ID-04 | Refresh token rotation issues a new token and marks the presented one revoked | Token rotation logic |
| UT-ID-05 | Reuse of an already-revoked refresh token is rejected | Replay/compromise handling |
| UT-ID-06 | Expired access token fails validation | Token expiry enforcement |
| UT-ID-07 | A user with no assigned role produces a token with an empty roles claim (and downstream authorization denies access) | FR-2 role model |
| UT-ID-08 | Auth audit event is constructed correctly for `LoginSucceeded`, `LoginFailed`, `Logout`, `TokenRefreshed` | FR-1.5 |

### 4.2 PatientService

| ID | Test | Verifies |
|---|---|---|
| UT-PT-01 | Search query logic correctly filters by partial name match | FR-4.1 |
| UT-PT-02 | Search query logic correctly filters by MRN | FR-4.1 |
| UT-PT-03 | Pagination logic returns the correct page/pageSize slice and correct `totalCount` | FR-4.2 |
| UT-PT-04 | Empty result set returns `items: []`, `totalCount: 0` — not an error | FR-4.3 |
| UT-PT-05 | Patient-detail mapping produces correct demographic fields, no extraneous data | FR-5.2 |

### 4.3 Angular

| ID | Test | Verifies |
|---|---|---|
| UT-NG-01 | `AuthInterceptor` attaches `Authorization` header when a token is present | Token attachment |
| UT-NG-02 | `AuthInterceptor` attempts exactly one silent refresh on a 401, then retries once | §6.3 refresh flow |
| UT-NG-03 | `AuthInterceptor` redirects to `/login` after a failed refresh, and clears session state | FR-1.4-adjacent (forced logout) |
| UT-NG-04 | `AuthGuard` blocks navigation with no valid session | FR-2.1 |
| UT-NG-05 | `RoleGuard` blocks navigation when the user's roles don't include a required role | FR-2.2 |
| UT-NG-06 | `PatientSearchComponent` renders `loading` / `results` / `empty` / `error` states correctly and never conflates them | FR-4.3, FR-4.4, FR-4.5 |
| UT-NG-07 | `LoginComponent` shows a generic error message on failed login, not a field-specific one | FR-1.2 |

### 4.4 Coverage expectation

Business logic (auth token handling, search/pagination, guard/interceptor decision logic) targets high coverage (≈80%+); simple DTOs, generated code, and framework wiring are not chased for coverage.

---

## 5. Integration Test Plan

Integration tests run the real ASP.NET Core pipeline (via `WebApplicationFactory`) against a real, disposable SQL Server test container — middleware, authentication, EF Core, and the database are all real; only external boundaries (if any existed in Phase 1, which has none) would be faked.

### 5.1 IdentityService

| ID | Test | Verifies |
|---|---|---|
| IT-ID-01 | `POST /identity/login` with seeded valid credentials returns 200 with a usable access + refresh token | End-to-end login |
| IT-ID-02 | `POST /identity/login` with invalid credentials returns 401 with the generic problem-details message | FR-1.2 |
| IT-ID-03 | `POST /identity/refresh` with a valid refresh token returns a new token pair and revokes the old one (verified by attempting reuse → rejected) | FR token lifecycle |
| IT-ID-04 | `POST /identity/logout` revokes the current refresh token; a subsequent refresh attempt with it fails | FR-1.4 |
| IT-ID-05 | `GET /identity/me` with a valid bearer token returns the correct user/roles | Profile retrieval |
| IT-ID-06 | `GET /identity/me` with no token returns 401 | FR-2.1 |
| IT-ID-07 | `POST /identity/users` as Administrator succeeds; as any other role returns 403 | FR-2.5, RBAC matrix |
| IT-ID-08 | `GET /health/live` and `/health/ready` return 200 when the database is reachable, and `/health/ready` returns 503 when it is not | Health check contract |

### 5.2 PatientService

| ID | Test | Verifies |
|---|---|---|
| IT-PT-01 | `GET /patients?query=` with no token returns 401 | FR-2.1 |
| IT-PT-02 | `GET /patients?query=` with a token lacking an authorized role (e.g., Auditor) returns 403 | RBAC matrix |
| IT-PT-03 | `GET /patients?query=<seeded-name>` with an authorized role returns the correct matching patient(s) | FR-4.1 |
| IT-PT-04 | `GET /patients?query=<no-match>` returns 200 with an empty result set, not 404 | FR-4.3 |
| IT-PT-05 | `GET /patients/{id}` for an existing seeded patient returns correct demographics | FR-5.2 |
| IT-PT-06 | `GET /patients/{id}` for a non-existent ID returns 404 with problem-details, no stack trace | FR-5.3, NFR-4 |
| IT-PT-07 | Pagination across a seeded set larger than one page returns correct `page`/`pageSize`/`totalCount` and correct item slices | FR-4.2 |

### 5.3 Cross-service / Gateway

| ID | Test | Verifies |
|---|---|---|
| IT-GW-01 | A request to `/patients/*` through the gateway without a token is rejected before or at PatientService — never silently proxied through as if authenticated | FR-2.4 |
| IT-GW-02 | Correlation ID present on an inbound request is forwarded unchanged to the downstream service; if absent, the gateway generates one | §9 observability convention |
| IT-GW-03 | `/hcc/*` and `/gaps/*` stub routes resolve correctly through the gateway and return their static/empty responses | Stub routing proof (Phase 1 doc §6) |

---

## 6. End-to-End (E2E) Test Plan

Run against the full Docker Compose stack, using a browser automation tool. These are the tests that most directly validate the Phase 1 Definition of Done (Functional Requirements doc §8).

| ID | Scenario | Steps | Expected result | Traces to |
|---|---|---|---|---|
| E2E-01 | Successful login | Open app → enter valid seeded credentials → submit | Lands on dashboard shell, user identity visible in shell | FR-1.1, FR-3.1, FR-3.3 |
| E2E-02 | Failed login | Open app → enter invalid credentials → submit | Generic error shown, user remains on login | FR-1.2 |
| E2E-03 | Protected route without session | Navigate directly to `/patients` with no prior login | Redirected to login | FR-2.1 |
| E2E-04 | Role-restricted action denied | Log in as a role without patient access (e.g., Auditor) → attempt to reach `/patients` | Unauthorized page shown, not a crash or blank screen | FR-2.3 |
| E2E-05 | Patient search — results | Log in as Clinician → search a seeded patient name | Paginated results shown, correct patient(s) listed | FR-4.1, FR-4.2 |
| E2E-06 | Patient search — no results | Search a term matching no patient | Explicit "no results" state shown | FR-4.3 |
| E2E-07 | Patient search — loading state | Trigger a search | Loading indicator visible before results/error render | FR-4.4 |
| E2E-08 | Patient detail view | From search results, open a patient | Correct demographics displayed | FR-5.1, FR-5.2 |
| E2E-09 | Logout | While logged in, select logout, then attempt to reach `/patients` again | Redirected to login; direct API calls with the old token also fail | FR-1.4 |
| E2E-10 | Unknown route | Navigate to a nonexistent path | Not-found page shown | FR-3.4 |
| E2E-11 | Session expiry mid-use | Force token expiry (test hook or short-lived token in test config) → perform an action | Silent refresh succeeds and the action completes, OR the user is cleanly returned to login if refresh also fails — never a hung or broken UI | §6.3 refresh flow |
| E2E-12 | Full stack boots clean | `docker compose up` from a clean state | All services report healthy; app is reachable and functional without manual intervention | §102 exit criterion 10 |

---

## 7. Security-Relevant Test Cases

Not a full security audit (that's a later-phase concern), but the minimum Phase 1 must verify given it already handles authentication:

| ID | Test | Verifies |
|---|---|---|
| SEC-01 | A tampered/invalid JWT signature is rejected by every backend service independently, not only at the gateway | §5.2 independent validation rule |
| SEC-02 | An expired JWT is rejected even if otherwise well-formed | Token expiry enforcement |
| SEC-03 | A JWT with a role claim that doesn't match any real role behaves as "no matching role" (denied), not as an unhandled error | Defensive role handling |
| SEC-04 | Error responses (401/403/404/500) never include stack traces, SQL fragments, or internal identifiers | NFR-4 |
| SEC-05 | Refresh tokens are never returned or logged in plaintext anywhere (API responses aside from the initial issuance, logs, error messages) | Token storage design |
| SEC-06 | SQL injection attempt in the patient search query parameter is safely handled (parameterized query, no error leak, no unintended results) | Basic input-handling hygiene |

---

## 8. Non-Functional / Performance Checks

Lightweight checks appropriate to Phase 1 scope — full load testing is not required yet, but the targets from the spec should be sanity-checked against the seeded dataset:

| ID | Check | Target | Source |
|---|---|---|---|
| PERF-01 | `GET /patients/{id}` response time against seeded data | < 300 ms (NFR-2) | §74 |
| PERF-02 | `GET /patients?query=` response time against seeded data | Feels interactive; consistent with sub-second guidance (NFR-1) | §74 |

These are sanity checks against a small synthetic dataset, not a load/stress test — formal performance testing under realistic data volume is out of scope until later phases.

---

## 9. Test Data Strategy

- All Phase 1 test data is synthetic — no real PHI, ever, in any test environment (consistent with §67 of the parent spec, applied early).
- A fixed, version-controlled seed set is used for integration and E2E tests: a small number of users covering each of the six roles, and a small number of patients covering the search/pagination/empty-result/not-found cases explicitly (e.g., a patient with a common name to test partial match, an MRN-only lookup case, and a guaranteed-zero-match search term).
- Seed data is deterministic and idempotent (safe to re-seed on every test run) — tests must not depend on data left over from a previous run.

---

## 10. CI Integration

Suggested gating, from fastest to slowest:

```text
On every push:
  1. Unit tests (backend + frontend)          — must pass
  2. Integration tests (backend)                — must pass
On every push once stable / before merge to main:
  3. E2E tests against a fresh Compose stack    — must pass
```

A failing unit or integration test blocks merge. E2E failures should also block merge once the suite is stable enough not to produce noise; if flaky early on, track flakiness explicitly rather than muting failures silently.

---

## 11. Defect Tracking (lightweight, solo dev)

Given the solo-developer context established in the Project Plan, a full defect-tracking system is unnecessary overhead for Phase 1. A defect found during testing should be:

1. Captured as a failing test (unit, integration, or E2E as appropriate) before it's fixed, not just fixed ad hoc — this prevents regression.
2. Linked back to the FR-x.x or test ID it violates, so the traceability matrix in §12 stays accurate.

---

## 12. Traceability — Functional Requirements → Test Cases

| Functional requirement | Unit | Integration | E2E |
|---|---|---|---|
| FR-1.1 (login) | UT-ID-01 | IT-ID-01 | E2E-01 |
| FR-1.2 (reject invalid credentials generically) | UT-ID-02, UT-NG-07 | IT-ID-02 | E2E-02 |
| FR-1.3 (session expiry) | UT-ID-06 | — | E2E-11 |
| FR-1.4 (logout) | UT-NG-03 | IT-ID-04 | E2E-09 |
| FR-1.5 (auth audit events) | UT-ID-08 | IT-ID-01–04 (implicitly, via event side effects) | — |
| FR-2.1 (auth required) | UT-NG-04 | IT-ID-06, IT-PT-01 | E2E-03 |
| FR-2.2 (role-restricted access) | UT-NG-05 | IT-ID-07, IT-PT-02 | — |
| FR-2.3 (clear unauthorized message) | — | — | E2E-04 |
| FR-2.4 (backend enforces independently of UI) | — | IT-GW-01 | — |
| FR-2.5 (role assignment) | — | IT-ID-07 | — |
| FR-3.1 (consistent shell) | — | — | E2E-01 |
| FR-3.3 (dashboard landing) | — | — | E2E-01 |
| FR-3.4 (not-found page) | — | — | E2E-10 |
| FR-4.1 (patient search) | UT-PT-01, UT-PT-02 | IT-PT-03 | E2E-05 |
| FR-4.2 (pagination) | UT-PT-03 | IT-PT-07 | E2E-05 |
| FR-4.3 (no-results state) | UT-PT-04, UT-NG-06 | IT-PT-04 | E2E-06 |
| FR-4.4 (loading state) | UT-NG-06 | — | E2E-07 |
| FR-4.5 (search error state) | UT-NG-06 | — | — (simulated failure recommended as an additional E2E case once a fault-injection approach is chosen) |
| FR-5.1 (open patient detail) | — | — | E2E-08 |
| FR-5.2 (correct demographics) | UT-PT-05 | IT-PT-05 | E2E-08 |
| FR-5.3 (detail load failure state) | — | IT-PT-06 | — |
| NFR-1/NFR-2 (response feel) | — | — | PERF-01, PERF-02 |
| NFR-3 (no cross-user/patient leakage) | — | IT-GW-01 | — |
| NFR-4 (safe error messages) | — | IT-PT-06 | SEC-04 |

Every FR-x.x from the Functional Requirements document has at least one test case above. If a future FR is added without a corresponding row here, treat that as a gap to close before Phase 1 sign-off.

---

## 13. Phase 1 Test Exit Criteria

Phase 1 testing is considered complete when:

1. All unit tests in §4 pass, with business-logic coverage at the targeted level (§4.4).
2. All integration tests in §5 pass against a real SQL Server test container, run from a clean seed.
3. All E2E tests in §6 pass against a freshly-started Docker Compose stack (`docker compose up` from a clean state, per E2E-12).
4. All security-relevant test cases in §7 pass.
5. Performance sanity checks in §8 meet their targets against the seeded dataset.
6. Every row in the traceability matrix (§12) is covered by at least one passing test.
7. No known defect remains open against any Must-priority requirement in the Functional Requirements document.

This is the testing counterpart to the functional Definition of Done (Functional Requirements doc §8) and the technical exit criteria (Technical Documentation, implicitly, via the traceability matrix in that document's §10) — all three should agree before Phase 1 is declared done.
