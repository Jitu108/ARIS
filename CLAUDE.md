# ARIS — Project Guide for Claude Code

ARIS is a risk-adjustment intelligence platform for healthcare (HCC mapping, gap-in-care detection, RAF calculation, and eventually RAG/agentic explanation over clinical evidence). It is being built solo, phase-by-phase, as a set of ASP.NET Core microservices behind an Ocelot gateway with an Angular frontend, Docker-first throughout.

**Current state:** implementation has started, mid-Phase-1. `Documentations/` remains the full spec — treat any detail below not yet reflected in code as the *design to build toward*. What exists today: solution scaffolding (`aris.sln`, `src/BuildingBlocks/aris.BuildingBlocks`), a working `IdentityService` (Api/Application/Domain/Infrastructure, with login, refresh/logout, session auto-expiry, and unit + integration test projects under `tests/`), the Angular app (`apps/aris-web`, including the app shell and login screen), and a `docker-compose.yml` wiring `sqlserver` + `identity-service` + `aris-web`. Not yet scaffolded: the Ocelot gateway, `PatientService`, and the `HccMappingService`/`GapEngineService` stubs — see the Project Plan's "Immediate Next Steps" for build order.

## Documentation map

Read in this order when context is needed:

1. `Documentations/Holy Grail/ARIS — Complete Implementation and User Reference Documentation.md` (v2.0) — the source-of-truth functional spec for the whole product (all 6 phases). Referenced everywhere else as `§<n>`.
2. `Documentations/Holy Grail/ARIS — Project Plan.md` — phase sequencing, milestones, effort estimates, vertical-slice build order.
3. `Documentations/Holy Grail/ARIS — Technical Documentation.md` — target end-state architecture across all phases (services, data, security, deployment). Components are tagged with the phase that introduces them — **never build something ahead of its tagged phase.**
4. `Documentations/Phases/ARIS — Phase 1 Functional Requirements.md` — what Phase 1 must do (FR-x.x IDs), no implementation detail.
5. `Documentations/Phases/ARIS — Phase 1 Technical Documentation.md` — Phase 1's authoritative architecture/data/API/security/deployment design.
6. `Documentations/Phases/ARIS — Phase 1 Detailed Plan.md` — Phase 1 work breakdown, task order, risks, exit-criteria checklist.
7. `Documentations/Phases/ARIS — Phase 1 Test Documentation.md` — Phase 1 test plan/traceability.
8. `Documentations/Phases/ARIS — Phase 1 UI Guidelines.md` — visual design system (colors, type, spacing) for the Angular app.

Each future phase gets its own Functional Requirements / Technical Documentation / Detailed Plan / Test Documentation / UI Guidelines set under `Documentations/Phases/`, following the Phase 1 pattern.

## Non-negotiable principles (apply to every phase)

- **Vertical slices only.** Every backend capability ships with its Angular UI in the same slice — never backend-only with UI deferred.
- **Identity first.** IdentityService + its UI is the foundation, not an afterthought.
- **Deterministic before generative.** HCC mapping and the Gap Engine (rules-based, Phase 3) must work and be trustworthy before RAG/LLM/agentic layers (Phase 4) are added on top.
- **Graceful degradation.** AI failure must never break core function. Layered fallback: canonical data → deterministic rules → keyword search → semantic search → LLM explanation → agentic reasoning. Each layer must survive the layers above it failing.
- **Evidence-first, human-in-the-loop.** No unsupported AI conclusions; humans retain final decision authority throughout.
- **Docker-first.** Every slice must be runnable via `docker compose up`, not only from the IDE — this is the actual exit-criteria bar, not IDE-only verification.
- **Service ownership.** Each service owns its own database exclusively; no service reaches into another service's database, ever (sync HTTP calls or async events only).
- **The gateway is never the sole security boundary.** Every backend service independently validates the JWT (signature, expiry, issuer/audience) and enforces its own role checks — Ocelot forwarding a token is not equivalent to trusting it.
- **Everything material is versioned.** API, schema, HCC model, mapping, rule, prompt, LLM, embedding model, retrieval config, agent config, UI — every AI/gap/RAF result must be traceable back to the exact versions that produced it.
- **PHI-safe logging from day one.** Logs reference entity IDs, never PHI-shaped fields (name, DOB, MRN). This habit starts in Phase 1, not retrofitted later.
- **Don't build ahead of the current phase.** Each phase document lists explicit non-goals — respect them (e.g., Phase 1 has no message broker, no search index, no real HCC/gap logic, no LLM/agent integration, no ABAC).

## Target architecture (introduced across phases — see phase tags)

```
Angular UI → Ocelot Gateway → IdentityService / PatientService / HccMappingService / GapEngineService
                                   ↓                                      ↓
                              SQL Server                          RafCalculationService
                                   ↓ (Outbox → RabbitMQ, Phase 2)
                     Indexer Worker → OpenSearch      Embedding Worker → Qdrant (Phase 4)
                                   ↓
                         Agent Orchestrator → LLM (provider-abstracted, Phase 4)
                                   ↓
                    Evidence-Grounded Explanation → Human Reviewer → Feedback/Audit
```

**Roadmap:** Phase 1 Platform/Identity/UI foundation → Phase 2 Clinical data ingestion/search → Phase 3 Deterministic risk intelligence (HCC/Gap/RAF) → Phase 4 RAG & agentic intelligence → Phase 5 Complete persona workflows → Phase 6 Enterprise/scale/research (ongoing).

## Phase 1 scope (what's actually being built right now)

**Goal:** an authenticated, deployable shell — login, patient search/detail, full user management — running end-to-end in Docker Compose. No clinical intelligence yet.

**Stack:** Angular + TypeScript · Ocelot (.NET) gateway · ASP.NET Core services, Clean Architecture per service · EF Core · SQL Server (one instance, one DB per service: `IdentityDb`, `PatientDb`) · JWT (RS256, access + rotating refresh tokens) · Docker/Docker Compose/Docker Hub · OpenAPI/Swagger per service. All inter-service and UI-to-gateway calls are synchronous HTTP — no broker/search/vector store in Phase 1.

**Services:**
- `IdentityService` — auth, JWT issuance, 6 seeded roles (`Administrator`, `Clinician`, `Coder`, `RiskAnalyst`, `Auditor`, `Researcher`), full user management (create/list/get/change-roles/deactivate/reactivate/bulk-import via CSV), self-service password reset, auth audit events. Owns `IdentityDb`.
- `PatientService` — patient demographics only (no encounters/diagnoses yet — those arrive with ingestion in Phase 2), search + detail read APIs. Owns `PatientDb`.
- `HccMappingService` / `GapEngineService` — thin stubs only (static/empty responses), to prove the routing/health pattern. Real logic is Phase 3 — do not implement it early.
- `BuildingBlocks` — shared (non-running) library: `Result`/problem-details wrapper, `BaseEntity` (Id/CreatedAtUtc/CreatedBy/ModifiedAtUtc/ModifiedBy), exception middleware, health-check contract, PHI-safe logging helpers, correlation-ID middleware. Build this first — everything depends on it.

**Key mechanics to get right:**
- Access token: short-lived JWT (15–30 min), RS256. Refresh token: opaque, hashed server-side, rotated on each use, reuse of a revoked token revokes the whole chain.
- Deactivation and password-reset-completion both revoke *all* outstanding refresh tokens for that user in the same request.
- Password reset is anti-enumeration: identical response whether or not the account exists.
- Angular holds the access token in memory (not `localStorage`); `AuthInterceptor` attaches the bearer token and does one silent refresh on 401 before redirecting to `/login`.
- Error responses are RFC 7807 problem-details everywhere — no stack traces, connection strings, or internal identifiers.
- Bulk import processes rows independently; one bad row never blocks the others.

Full endpoint contracts, RBAC matrix, sequence flows, and data schemas: `Documentations/Phases/ARIS — Phase 1 Technical Documentation.md`.

**UI design system (Phase 1):** original system, no pre-existing brand. IBM Plex Sans (UI text) / IBM Plex Mono (MRNs, IDs). Cool blue-teal accent (`--accent #0B6E8C`) on neutral slate grays. Borders over shadows — no drop-shadow "floating card" look, no left-border-accent decoration (except the active sidebar nav item). Red (`--error`) reserved for genuine failures; amber (`--warn`) for 403/access-restriction — don't conflate the two. Full tokens/type scale/spacing scale in `Documentations/Phases/ARIS — Phase 1 UI Guidelines.md`.

## When implementing

- Check which phase a capability belongs to before building it — the phase's Technical Documentation has an explicit "Non-Goals" section; respect it.
- Cross-phase workstreams that must be seeded early even though they're not a phase's main focus: audit-trail logging (from Phase 1), versioning discipline (as each dimension is introduced), PHI-safe logging (from Phase 1), feature flags (from Phase 3's rule engine or Phase 4's retrieval strategy).
- Follow the vertical-slice build order within a phase (see each phase's Detailed Plan) — don't start slice N+1 before slice N's exit criteria pass in Docker Compose.
- Solution/project scaffolding, `BuildingBlocks`, `IdentityService`, and the Angular workspace already exist — don't re-scaffold them. The next unbuilt pieces per the Project Plan's "Immediate Next Steps" are the Ocelot gateway, `PatientService`, and the `HccMappingService`/`GapEngineService` stubs.
