# aris-rbac-matrix-sync

**Definition:** `.claude/skills/aris-rbac-matrix-sync/SKILL.md`

## What it does

Keeps a phase's endpoint × role RBAC matrix in sync whenever an endpoint is added, changed, or removed in any ARIS service, and verifies the endpoint's actual authorization attribute matches the documented row with no drift between what the Technical Documentation says and what the code actually enforces.

## How it does it

**§1 is the update procedure for an endpoint change**: identify the endpoint (method + route + owning service); determine intended roles from the *Functional Requirements* document (not by guessing — e.g. Phase 1 FR §3.6 ties every user-management endpoint to Administrator-only), raising a flag if an endpoint has no clearly-named actor in the FR doc rather than inventing one at the API layer; update the matrix row using the exact existing table format (one column per seeded role, `✓`/`–`, plus the two established special markers `anonymous` and `✓ (self)` — never a new marker style); cross-check the row against the endpoint's actual `[Authorize(Roles = "...")]` attribute, treating any mismatch as drift to be fixed on whichever side is wrong (not silently reconciled by copying code into the doc without checking which is correct against the FR); verify the owning service actually validates the JWT independently (pointing to `aris-new-service-scaffold`'s security checklist); and update the phase's Traceability section so the mapping stays bidirectional — every FR needs a design element, every protected endpoint needs a traceable FR.

**§2 lists special cases that are not bugs**: anonymous, token-bearing endpoints (password-reset request/confirm, refresh) are protected by token possession/expiry/single-use mechanics, not a role check, and get the `anonymous` marker rather than a forced role column; roles with no functional area yet (Phase 1's `Auditor`/`Researcher`) correctly lacking access to a new endpoint isn't a gap to fill; a role change taking effect only on next login/refresh (not retroactively on an already-issued token) is documented, expected behavior; and the forced-password-change gate is explicitly *not* part of this matrix — it's a separate cross-cutting allow-list applied identically to every role, not a per-endpoint RBAC row, and shouldn't be conflated with it.

**§3 sets the reporting bar** for an audit invocation: which endpoints were checked, any row where code and doc disagree (file:line for code, section/row for doc), any endpoint missing entirely — never a bare "RBAC looks fine."

## Why it exists

Every phase's Technical Documentation carries an RBAC matrix (Phase 1's is §5.3; `aris-phase-documentation`'s skeleton requires the equivalent for every later phase). The Phase 1 Detailed Plan's risk table names the two failure modes this skill exists to prevent from recurring: the gateway silently becoming the actual security boundary (a documented row is meaningless if the service doesn't independently validate the JWT it's supposedly enforcing that role against), and a documented RBAC rule quietly drifting out of sync with what the code enforces as endpoints get added over time. RBAC-only is itself a deliberate, phase-bounded design decision — the master spec states "Phase 1 establishes the RBAC foundation. Later phases can introduce ABAC" — so this skill also guards against attribute/resource-level authorization creeping in early "just because it seems safer," which every phase's Technical Documentation Non-Goals section restates until Phase 6.

## When it fires

Whenever adding or changing a protected API endpoint in any phase; or when asked to audit/update a phase's RBAC matrix specifically.

## How to invoke

- **Explicitly**: `/aris-rbac-matrix-sync`, or ask directly — "update the RBAC matrix for this new endpoint," "audit Phase 1's RBAC matrix against the code."
- **Implicitly**: the assistant should apply this skill on its own whenever it adds, changes, or removes a protected API endpoint in any service — the description's trigger condition is exactly that, not a request to specifically update RBAC documentation. A request like "add a `DELETE /identity/users/{id}` endpoint" should trigger this skill implicitly as part of finishing that work, since leaving the matrix stale is precisely the drift this skill exists to prevent.

## Other details

- **Narrower and deeper than the agent `fr-techdoc-testdoc-traceability-auditor`'s RBAC check** — that agent only confirms an RBAC-matrix *row exists* for an FR that implies a protected endpoint, as one of several traceability dimensions; this skill checks the row's *correctness* against the actual authorization code, in depth. The two are meant to compose, not duplicate.
- **Depends on `aris-new-service-scaffold`'s §2 security checklist having actually been followed** — its own step 5 ("verify independent enforcement") explicitly defers to that skill's checklist rather than re-deriving what "independent JWT validation" means.
- **The FR-doc-first rule is deliberate**: this skill treats "who should have access" as a requirements decision that belongs in the FR document, not something to infer or invent while writing the API layer — an endpoint with no clearly-named actor is a signal to go back to the FR doc, not to guess.
- **Its special-cases list (§2) exists specifically to stop the skill from over-correcting** — without it, a well-meaning sync could "fix" a role's correctly-restricted access, or misread a stale-token role claim as a live bug, or fold the forced-change gate into the matrix where it doesn't belong.
