# Service Docs index

As-built implementation logs and architecture guides, maintained by the `aris-implementation-log` Claude Code skill. Each is a living document: an always-current overview at the top, and a newest-first ticket log underneath. This is not the design spec — for intended design see `Documentations/Phases/` and `Documentations/Holy Grail/`; these docs explain how the code that already exists actually works and connects.

| Doc | Covers | Last updated |
|---|---|---|
| [IdentityService.md](./IdentityService.md) | Backend auth API (login/refresh/logout), Clean Architecture layers, JWT/refresh-token mechanics | TARIS-013 (uncommitted) |
| [IdentityService-UI.md](./IdentityService-UI.md) | Angular login/session/shell slice consuming IdentityService's endpoints | TARIS-012 |
| [IdentityService-Tests.md](./IdentityService-Tests.md) | What every backend (xUnit) and frontend (Angular spec) test for the Identity slice actually verifies — living, no ticket log | TARIS-013 (uncommitted) |

API and UI docs for the same service are always paired and cross-linked (never merged) — see each doc's §1 for the pointer to its counterpart. The Tests doc is combined across both sides (one file, not paired) and is living-only — it has no per-ticket history, just a current, reconciled-each-run inventory.
