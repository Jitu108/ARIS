---
name: aris-rbac-matrix-sync
description: Keep an endpoint × role RBAC matrix in sync whenever an endpoint is added, changed, or removed in any ARIS service, and verify the endpoint's actual authorization attribute matches the documented row with no drift. Use whenever adding/changing a protected API endpoint in any phase, or when asked to audit/update a phase's RBAC matrix (Technical Documentation §5.3-equivalent section).
---

# ARIS RBAC matrix sync

Every phase's Technical Documentation carries an endpoint × role RBAC matrix (Phase 1's is §5.3; the `aris-phase-documentation` skill's Technical Documentation skeleton requires one for every later phase too). This skill keeps that table accurate and keeps the two failure modes the Phase 1 Detailed Plan's risk table calls out from recurring: **the gateway silently becoming the actual security boundary**, and **a documented RBAC rule drifting out of sync with what the code enforces**.

RBAC only through Phase 5 — master spec: "Phase 1 establishes the RBAC foundation. Later phases can introduce ABAC." Don't add attribute/resource-level (patient-level) authorization ahead of Phase 6 just because it seems safer; that's explicitly out of scope until then (§95/§107, and every phase's Technical Documentation Non-Goals section restates it).

## 1. When an endpoint is added or changed

1. **Identify the endpoint**: HTTP method + route, and which service owns it.
2. **Determine intended roles from the Functional Requirements doc**, not from guessing — the owning phase's FR doc names the actor(s) for each `FR-x.x` (e.g., Phase 1 FR §3.6 ties every user-management endpoint to "Administrator only"; FR-4/FR-5 tie patient search/detail to "Clinician, Coder, RiskAnalyst, Administrator"). If an endpoint has no clearly-named actor in the FR doc, that's a signal to raise it rather than guess — RBAC decisions belong in the FR doc, not invented at the API layer.
3. **Update the matrix row** in that phase's Technical Documentation, following the exact existing table format: one column per seeded role (Phase 1: `Administrator, Clinician, Coder, RiskAnalyst, Auditor, Researcher`), `✓`/`–` per cell, plus the two special markers already established — `anonymous` (pre-auth endpoints: login, password-reset request/confirm) and `✓ (self)` (an authenticated user acting only on their own record, e.g. `POST /identity/change-password`). Don't invent a new marker style; reuse these two.
4. **Cross-check against the code**, if it exists: the endpoint's actual `[Authorize(Roles = "...")]` (or equivalent policy) must list exactly the roles in the matrix row — no more, no fewer. A role present in code but not the doc, or vice versa, is drift and must be fixed in whichever side is wrong, not silently reconciled by editing the doc to match code without checking which one is actually correct against the FR.
5. **Verify independent enforcement**: this endpoint's service validates the JWT itself (see [[aris-new-service-scaffold]] §2, "Security" checklist) — a correct matrix row is meaningless if the service just trusts Ocelot's forwarded header. If the service is newly created, confirm this was set up, not assumed.
6. **Update the Traceability section** (§10-style in Phase 1; every phase's Technical Documentation has the equivalent) so the FR ID maps to this endpoint and its RBAC row — traceability must stay bidirectional: every FR needs a design element, and every protected endpoint needs a traceable FR.

## 2. Special cases, not bugs

- **Anonymous, token-bearing endpoints** (`password-reset/request`, `password-reset/confirm`, `refresh`): these are pre-authentication by design and are protected by token possession + expiry + single-use mechanics (§5.1/§3.1), not a role check. Mark them `anonymous` in the matrix, don't try to force a role column onto them.
- **Roles with no functional area yet** (Phase 1: `Auditor`, `Researcher` — can authenticate and manage their own profile/password, nothing else): a new endpoint correctly excluding them isn't a gap to fill; check the current phase's Non-Goals section before assuming every role needs access to every new endpoint.
- **A role-change takes effect on next login/refresh, not retroactively** (§4.1) — don't treat an already-issued access token's stale role claim as a bug; that's documented, expected behavior, not something this sync should "fix."
- **Forced-password-change gate is not part of this matrix** — it's a separate cross-cutting allow-list (every role, applied identically in every service), not a per-endpoint RBAC row. Don't conflate the two when updating the table.

## 3. Reporting

When invoked as an audit (not just after adding one endpoint), report concretely: which endpoints were checked, any row where code and doc disagree (cite file:line for the code side, section/row for the doc side), and any endpoint missing from the matrix entirely. A clean result should name what was checked, not just say "RBAC looks fine."
