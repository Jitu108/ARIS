# phase-scope-guard

**Definition:** `.claude/agents/phase-scope-guard.md`
**Tools:** Read, Grep, Glob, Bash
**Model:** inherit (whatever model the invoking session is using)

## What it does

Judges whether a stub service, a "thin" feature, or an early piece of infrastructure has quietly grown capability that belongs to a later phase — the cases that require reading intent, not matching a banned keyword or package name (that mechanical half is handled by the hook `phase-dependency-package-guard`). Specifically watches for:

1. **A stub gaining real logic** — `HccMappingService`/`GapEngineService` must stay static/empty-response stubs in Phase 1; the agent reads the actual endpoint implementation to check whether it's still a hardcoded array/unconditional `[]`, or has grown a lookup table, algorithm, or conditional behavior.
2. **Infrastructure arriving ahead of its phase through a workaround** — not literally "RabbitMQ" or "OpenSearch" (the hook already catches that), but a hand-rolled background job queue, in-memory pub/sub, or search/ranking function that reimplements the same capability without naming a banned dependency.
3. **Authorization scope exceeding RBAC** — code that starts filtering results by something tied to the *acting user* specifically (e.g., "only patients this Clinician has viewed before") is ABAC/resource-level scoping arriving before Phase 6, even when framed as a small usability improvement rather than a formal authorization feature.
4. **Violation of the current phase's own stated Non-Goals** — the agent is told to re-read the *current* phase's Technical Documentation §11 (or equivalent) each time, since the list of what's "ahead of phase" is phase-specific and changes as phases progress; it's explicitly warned not to apply Phase 1's Non-Goals to Phase 3 code.

It's also given an explicit "what is NOT scope creep" list so it doesn't over-flag: a stub returning richer *static* data is still a stub; modeling entity relationships that anticipate a later phase (e.g., `Patient` designed with room for future `Encounter` relationships) is explicitly sanctioned as long as those relationships aren't being populated or queried yet; building the correlation-ID convention now, even though nothing visualizes it until Phase 6, is required, not creep.

## How it does it

1. Identifies which phase the touched service/feature belongs to, and reads that phase's actual Technical Documentation Non-Goals section (and Detailed Plan scope statement) directly rather than relying on a remembered summary of where the boundary sits.
2. Reads the actual code change — not just the PR/commit description, since the system prompt notes scope creep is usually not something the description mentions.
3. For anything flagged, states concretely: what the code does now, which phase that capability belongs to, and the specific documentation line that draws the boundary.
4. If nothing has crept, says so and names what was checked (which service, against which phase's Non-Goals) rather than a generic "looks fine."

## Why it exists

CLAUDE.md states plainly: "Don't build ahead of the current phase. Each phase document lists explicit non-goals — respect them." The Detailed Plan is equally direct about *why* this matters structurally, not just as a rule: "Keep these deliberately thin. Do not let Phase 1 scope creep into Phase 3's mapping/gap logic — the project plan's phase boundary exists specifically so the deterministic risk layer gets focused attention in Phase 3." The mechanical half of this — a banned package reference — is cheap to catch with a regex (`phase-dependency-package-guard`). The harder half is exactly what this agent exists for: a stub's endpoint can grow real behavior using nothing but plain conditionals and in-memory data structures, with no dependency added at all, and *that* requires reading what the code actually does and comparing it against the specific phase's stated boundary — a judgment call a keyword search can't make.

## When it fires

Invoke it when reviewing a diff, PR, or new code specifically for phase-boundary conformance — most relevant right now for `HccMappingService`/`GapEngineService` changes (the two Phase 1 stubs most at risk of premature enrichment), but applicable to any phase's boundary once later phases are underway (e.g., checking that Phase 2's ingestion work doesn't quietly start doing Phase 3's HCC mapping, or that Phase 4's RAG layer doesn't bypass Phase 3's deterministic rules engine as a fallback shortcut).

## How to invoke

- **Explicitly**: ask by name or clear intent — "check this stub for scope creep," "run phase-scope-guard on the GapEngineService change."
- **Implicitly**: the description's trigger condition is "reviewing a diff, PR, or new code against the current phase's stated scope" — broader than a single endpoint or file type, and easy to under-invoke because scope creep rarely announces itself in a PR description (the agent's own system prompt notes this explicitly). The assistant should treat any change to `HccMappingService`/`GapEngineService` (Phase 1's two stubs) as a standing implicit trigger, and extend the same instinct to any later-phase boundary once those phases are underway — e.g. Phase 2 ingestion work brushing up against Phase 3's mapping logic.

## Other details

- **Read-only** (no `Edit`/`Write`) — flags scope creep for a human/main-conversation decision, doesn't unilaterally strip out the offending code.
- **Deliberately narrow relative to a full code review** — it isn't a general-purpose reviewer and shouldn't be asked to catch bugs, style issues, or anything unrelated to phase-boundary conformance; that scope discipline is what keeps its output focused enough to act on directly.
- **Its correctness depends on it re-reading the current phase's Non-Goals each time**, rather than caching an assumption from a prior review — the system prompt is explicit about this because the set of "things not yet allowed" changes release to release, and an agent that silently reused Phase 1's boundary against Phase 3 code would produce actively wrong findings, not just stale ones.
- **Complements, but doesn't replace, `phase-dependency-package-guard`** (the hook) — a change that adds both a banned package *and* quietly enriches a stub would be caught twice, which is fine; the two are meant to have overlapping coverage at the edges rather than a hard boundary between what each one checks.
