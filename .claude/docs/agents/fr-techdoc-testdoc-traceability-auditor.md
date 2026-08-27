# fr-techdoc-testdoc-traceability-auditor

**Definition:** `.claude/agents/fr-techdoc-testdoc-traceability-auditor.md`
**Tools:** Read, Grep, Glob
**Model:** inherit (whatever model the invoking session is using)

## What it does

Given a phase's documentation set (Functional Requirements, Technical Documentation, Test Documentation, and — where relevant — the RBAC matrix), checks that every `FR-<section>.<n>` is genuinely traced through all of them, and reports any FR that is:

- **Missing from the Technical Documentation's traceability table** — no design element named as satisfying it.
- **Missing from the Test Documentation's traceability table** — zero test IDs (`UT-`/`IT-`/`E2E-`/`SEC-`/`PERF-`) anywhere.
- **Missing from the RBAC matrix**, but *only* when the FR clearly implies a protected endpoint — FRs that are UI-only, business-rule-only, or explicitly anonymous/pre-auth are not expected to have a row, and the agent is told not to flag those.
- **Terminology-mismatched** — traced everywhere, but the actor/role named in the FR doesn't match what the Technical Documentation's design or the Test Documentation's scenarios actually describe.

## How it does it

1. Reads the phase's Functional Requirements document and enumerates every `FR-x.x` ID.
2. Reads that phase's Technical Documentation traceability table (and RBAC matrix, if present) and notes which FR IDs appear there.
3. Reads that phase's Test Documentation traceability table and notes which FR IDs appear, and with which test ID(s).
4. Cross-references all three, classifying every FR into one of: fully traced (with the Technical Documentation row and Test Documentation test ID(s) cited), missing from Technical Documentation, missing from Test Documentation, missing from RBAC matrix, or terminology mismatch (described concretely, not just flagged).
5. Reports as a table or list, most severe gaps first — a missing Test Documentation entry is treated as more severe than a small wording difference, since "zero tests trace to this requirement" is a bigger gap than "the phrasing differs slightly across two documents that otherwise agree."
6. If every FR is fully traced, says so explicitly and names how many FRs were checked across which three documents — the system prompt specifically forbids a bare "looks fine" with no count or scope named.

It's explicitly instructed **not** to flag a gap over wording differences alone — traceability requires substantive coverage, not verbatim repetition — and **not** to fix anything itself unless asked; if asked to fix, it's told to follow (or hand off to) the `aris-phase-documentation` skill's "cascading a change" procedure, since a real fix usually means adding a consistent row to more than one document at once.

## Why it exists

The Test Documentation's own traceability section states the standard this agent enforces, nearly verbatim: "if a future FR is added without a corresponding row here, treat that as a gap." This is a documented, named failure mode of the documentation-generation process the project actually uses — `aris-phase-documentation` (the skill that generates and cascades changes across a phase's five documents) is thorough, but a solo developer making a small, seemingly-contained addition to just the FR doc (or asking for a quick FR tweak outside a full cascade) can easily leave the Technical Documentation or Test Documentation's traceability table unupdated. This agent exists specifically to catch that after the fact, independent of whether the cascade was followed correctly at write time.

It's broader than the skill `aris-rbac-matrix-sync`, which only audits the RBAC-matrix slice of traceability (and does so in more depth — checking the matrix row against the actual code's authorization attribute, not just checking a row exists). This agent checks RBAC-matrix *presence*, not RBAC-matrix *correctness* — that finer-grained check is deliberately left to the skill so the two don't duplicate effort.

## When it fires

Invoke it whenever:
- A Functional Requirement is added, changed, or removed in any phase's FR document.
- Asked to audit a phase's documentation set for traceability gaps generally ("does every FR have a test ID," "audit Phase 1's traceability").
- Before considering a phase's documentation "done" for a given round of changes — a good complement to running `aris-phase-documentation`'s cascade, as an independent check that the cascade actually landed everywhere.

## How to invoke

- **Explicitly**: ask by name or clear intent — "run the traceability auditor on Phase 1," "does every FR have a test ID."
- **Implicitly**: unlike `auth-session-security-reviewer`, this agent's description doesn't use "proactively" — it's phrased as "Use whenever a Functional Requirement is added, changed, or removed... or when asked to audit a phase's documentation set for traceability gaps." That's still a real trigger condition, not just a reactive one: the assistant should spawn this agent on its own right after any FR addition/change/removal, even if the user only asked for the FR edit itself — the same way `aris-rbac-matrix-sync` (a skill) fires on its own after an endpoint change rather than waiting to be asked separately.

## Other details

- **Read-only** (no `Edit`/`Write`) — it's an audit, not an auto-fixer, by design. Fixing what it finds is a separate, explicit step.
- **Repeats identically every phase** — the same three-document cross-reference applies to Phase 2 through Phase 6's documentation, not just Phase 1's; nothing about its logic is Phase-1-specific.
- **Depends on the FR/Technical Documentation/Test Documentation ID schemes staying consistent** (`FR-<section>.<n>`, `UT-`/`IT-`/`E2E-`/`SEC-`/`PERF-` prefixes) — if a future phase's documents deviate from these conventions, this agent's cross-referencing logic would need to be told about the new scheme.
- **Doesn't validate that a cited test ID's *test actually passes*** — it only checks that a test ID is *named* in the traceability table, not that the corresponding test exists in the codebase or is green. That's a different kind of check (closer to `exit-criteria-verifier`, deferred until there's a real test suite to check against).