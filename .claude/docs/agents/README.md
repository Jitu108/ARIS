# ARIS Claude Code agents

Three judgment-based reviewer personas, defined in `.claude/agents/`, invoked via the `Agent` tool when a task matches their description. These exist because they need to read code (or documents) and make a call a mechanical script can't — contrast with the project's hooks (`.claude/hooks/`), which are deterministic pattern-matching gates wired into `.claude/settings.json`, and with its skills, which package instructions for a model already doing the work in the main conversation rather than a separate reviewing persona.

| Agent | Reviews | Tools | Doc |
|---|---|---|---|
| `auth-session-security-reviewer` | Auth/session mechanics: JWT signing/validation, refresh-token rotation, the forced-password-change gate, anti-enumeration, secrets | Read, Grep, Glob, Bash | [auth-session-security-reviewer.md](auth-session-security-reviewer.md) |
| `fr-techdoc-testdoc-traceability-auditor` | Every FR-x.x traced through Technical Documentation, Test Documentation, and the RBAC matrix | Read, Grep, Glob | [fr-techdoc-testdoc-traceability-auditor.md](fr-techdoc-testdoc-traceability-auditor.md) |
| `phase-scope-guard` | Scope creep across the current phase's boundary — cases a keyword grep can't catch | Read, Grep, Glob, Bash | [phase-scope-guard.md](phase-scope-guard.md) |

## Why these three, and why as agents rather than hooks

All three were selected from a broader candidate list (surveyed against the project's documentation set) specifically because the thing they check requires reading and weighing evidence, not matching a fixed pattern:

- Whether JWT validation is *actually independent* per service, or a refresh-token reuse *actually* revokes the whole chain, requires reading the implementation and reasoning about a sequence of calls — not something a single-file regex can determine.
- Whether an FR is "fully traced" requires comparing three separate documents' worth of prose and tables for substantive (not literal) coverage, and telling a real gap apart from a wording difference that doesn't matter.
- Whether a stub has "quietly grown real logic" requires reading intent — a static array is fine, a lookup table computed from input is not, and no keyword distinguishes the two.

A fourth candidate, `exit-criteria-verifier` (running a phase's exit-criteria checklist against a live `docker compose up` + test results), was deliberately deferred — there's nothing to verify until Slice 1 (login) is actually implemented. It should be built once that exists.

## How these compose with the rest of the toolkit

- `phase-scope-guard` is the judgment-based complement to the hook `phase-dependency-package-guard` — the hook catches the easy case (a banned package reference), the agent catches the hard case (a stub that grew real behavior without adding any banned dependency at all).
- `fr-techdoc-testdoc-traceability-auditor` is broader than the skill `aris-rbac-matrix-sync`, which only handles the RBAC-matrix slice of traceability; the agent checks RBAC-matrix presence too, but as one of several traceability dimensions, not RBAC correctness itself.
- `auth-session-security-reviewer` overlaps in domain with the skill `aris-phi-safe-log-audit` (both can touch IdentityService code) but checks a disjoint set of concerns — token/session mechanics vs. PHI-shaped fields leaking into logs. A full review of an auth-related change may reasonably invoke both.

## Invocation model

Agents are reached through the `Agent` tool, which takes a `subagent_type` naming one of these (or a built-in type like `Explore`/`general-purpose`):

- **Explicit** — the user directly asks for a named agent ("run auth-session-security-reviewer on this," "get a second opinion from phase-scope-guard"), or asks in terms that clearly mean one of these ("review this for scope creep" → `phase-scope-guard"). The assistant then calls `Agent` with that `subagent_type`.
- **Implicit** — the assistant decides on its own, without being asked, that a task matches one of these agents' descriptions closely enough to spawn it as part of finishing the work. This is a judgment call the assistant makes from the agent's description, the same way it decides whether to invoke a skill implicitly — nothing forces it the way a hook's tool-call matcher does. `auth-session-security-reviewer`'s own description says "Use proactively whenever a change touches authentication..." — that specific wording is a stronger signal that it should be spawned autonomously, not just when explicitly requested. The other two don't use the word "proactively" but can still be invoked implicitly whenever their description's trigger condition is clearly met; see each agent's own doc for its exact wording.

A spawned agent can also be resumed with full context via `SendMessage` to its id/name, rather than starting a fresh one — useful for a multi-round review rather than launching a new instance each time.

## When to invoke which

Use the agent whose description matches the change, not the one that sounds most senior — each is scoped narrowly on purpose, and none of them do general code review or security review (that's what the built-in `code-review` and `security-review` skills are for). Invoking more than one on the same diff is expected and fine when a change touches more than one concern (e.g., a new endpoint that both adds a role check and needs FR traceability).
