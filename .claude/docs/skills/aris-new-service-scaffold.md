# aris-new-service-scaffold

**Definition:** `.claude/skills/aris-new-service-scaffold/SKILL.md`

## What it does

Scaffolds a new ARIS backend microservice — or brings an existing one up to the current standard — with the identical skeleton every service must share: the fixed §1.3 Clean Architecture folder structure, `BuildingBlocks` wiring, independent JWT/RBAC validation, the forced-password-change gate, correlation-ID propagation, PHI-safe logging, health checks, OpenAPI, a Dockerfile, and Ocelot/Compose wiring.

## How it does it

**§0 fixes the folder structure** (mirroring Phase 1 Technical Documentation §1.3): every service goes under `src/Services/<ServiceName>/` as four separate projects — `ARIS.<ServiceName>.{Domain,Application,Infrastructure,Api}` — never fewer, never a different split. Reference direction is `Api → Infrastructure → Application → Domain`; `Domain` has zero project references; `Application` never references `Infrastructure` (it defines the interfaces Infrastructure implements); `Api` is the sole composition root and the only layer wiring `BuildingBlocks`' ASP.NET-specific middleware. Tests mirror under a global `tests/` tree, not colocated inside the service folder. This structure is stated as fixed, not a placeholder — any deviation is a defect to fix, not a new variant to accept.

**§1 establishes `BuildingBlocks` first**, once, before or alongside the first service: the `Result<T>`/problem-details wrapper, `BaseEntity`, exception middleware, the health-check contract, the PHI-safe logging helper, and correlation-ID middleware. If a new service needs a `BuildingBlocks` piece that doesn't exist yet, the instruction is to extend `BuildingBlocks` itself, not reimplement it locally.

**§2 is the per-service checklist**, run in order for every new service — Structure (project split, DB ownership, `BaseEntity` inheritance), Security (independent JWT validation in *this* service — named as the single most-repeated risk in the Detailed Plan's risk table; role-based `[Authorize]` attributes; the forced-password-change gate wired in identically to every other service, with the fixed allow-list; no ABAC ahead of Phase 6), Observability & correctness (correlation ID, PHI-safe logging, health checks, RFC 7807 problem-details, OpenAPI), Deployment (multi-stage Dockerfile, Compose `healthcheck:`/`condition: service_healthy`, Ocelot route, env vars, image tagging), and an explicit non-goals re-check (no broker/search/vector-store/LLM ahead of their phase).

**§3 covers stub services**: `HccMappingService`/`GapEngineService` go through the *entire* checklist except real business logic — same JWT validation, health checks, Dockerfile, Compose wiring, Ocelot route; only the endpoint bodies stay static/empty. The skill is explicit that a stub's simplicity is not license to skip the security/observability half of the checklist.

**§4 covers applying it to a later-phase service**: re-read the checklist (it doesn't change phase to phase), read that phase's Technical Documentation for what's *additional* (e.g., Phase 2's outbox-pattern events, Phase 4's AI-audit hooks), and if a new pattern is itself going to recur, add it to this skill rather than leaving it to live in one phase's plan alone.

## Why it exists

Every backend service in ARIS — from `IdentityService` in Phase 1 through the Phase 4/5 workers — repeats the same shape, and this project is built solo across six phases. Without a single enforced skeleton, each new service scaffold is an opportunity to reintroduce exactly the mistakes the Phase 1 Detailed Plan's risk table names explicitly: downstream services trusting Ocelot's forwarded header instead of validating the JWT themselves, or the forced-password-change gate getting implemented only in IdentityService and silently skipped everywhere else. The §1.3 folder convention specifically was fixed (after comparing several alternatives) precisely because it's new and precise enough to drift without a shared reference — a `<ProjectReference>` in the wrong direction still compiles fine, so nothing about a "wrong" scaffold looks obviously broken at a glance.

## When it fires

Whenever adding `IdentityService`, `PatientService`, a stub service (`HccMappingService`/`GapEngineService`), or any later-phase service (`DataIngestService`, `RafCalculationService`, an Embedding Worker, an Agent Orchestrator, an Analytics/Audit Processor) — never re-derive the skeleton per service from first principles.

## How to invoke

- **Explicitly**: `/aris-new-service-scaffold`, or ask directly — "scaffold PatientService," "bring HccMappingService up to the current standard."
- **Implicitly**: the assistant should load this skill on its own whenever a task is about to add or touch a backend service's project structure — the description's own trigger list names `IdentityService`, `PatientService`, the stub services, and any later-phase service (`DataIngestService`, `RafCalculationService`, an Embedding Worker, an Agent Orchestrator, an Analytics/Audit Processor) explicitly, and closes with "never re-derive the skeleton per service from first principles" — meaning even a request that doesn't mention this skill by name (e.g. "add a new endpoint's project files for RafCalculationService") should still trigger it implicitly, since building service structure from memory instead is exactly what the skill exists to prevent.

## Other details

- **This is the skill the hooks `solution-structure-reference-lint` and `service-db-isolation-check` mechanically check compliance with** — the skill is how a service *should* get built; the hooks are the backstop against a later, unrelated edit drifting away from it.
- **This is also what `auth-session-security-reviewer` (an agent) checks was actually done correctly** for a newly-scaffolded service's JWT wiring and forced-change gate — the skill states the standard, the agent verifies it was met.
- **`aris-phi-safe-log-audit` and `aris-rbac-matrix-sync` both check something this skill sets up but doesn't itself deeply verify** — this skill's checklist says "wire in the PHI-safe logging helper" and "declare roles via `[Authorize]`," but confirming those are *used correctly* everywhere, not just present, is those two skills' job.
- **Explicitly time-bound**: its non-goals list (no broker/search/vector-store/LLM) is phrased as "ahead of *their* phase," meaning this skill's own checklist needs re-reading against each new phase's Technical Documentation, not assumed to be permanently fixed.