# service-db-isolation-check

**Script:** `.claude/hooks/service-db-isolation-check.sh`
**Event:** `PostToolUse`, matcher `Edit|Write`
**Enforcement:** warn only (`systemMessage`, non-blocking)

## What it does

After a `.csproj` or `appsettings*.json` file under `src/Services/<Service>/...` is created or edited, flags two possible database-isolation violations:

1. An `appsettings*.json` declaring **more than one** entry under `ConnectionStrings`.
2. A `.csproj`'s `<ProjectReference>` pointing into a **different** service's folder under `src/Services/` than the one that owns the file being checked.

## How it does it

1. Reads the tool-call JSON; resolves the file path.
2. Derives the "owning service" from the path itself — the segment immediately after `src/Services/` (e.g. `src/Services/PatientService/...` → `PatientService`). Exits (no-op) if the path doesn't contain `src/Services/` at all.
3. Branches on file type:
   - `appsettings*.json`: uses `jq` to count the keys under `.ConnectionStrings`; flags if the count is greater than 1.
   - `.csproj`: extracts every `<ProjectReference Include="...">` path, and for each one, looks for a `Services/<name>/` segment in that (relative) path. If a match is found and `<name>` differs from the owning service, it's flagged as a cross-service reference. (`BuildingBlocks` references never match this pattern, since that shared library lives under `src/BuildingBlocks/`, not `src/Services/` — so referencing it is never flagged.)
4. Collects all issues into one `systemMessage`; emits `{}` if none.

## Why it exists

Phase 1 Technical Documentation §111 (echoed at §1.2): "no service reaches into another service's database, ever" — sync HTTP or async events only for cross-service communication. This is one of the project's structural non-negotiables (CLAUDE.md: "Service ownership. Each service owns its own database exclusively"). Two different code smells both violate it in practice, which is why this one hook checks both:

- A service's config holding more than one connection string is a strong signal it's about to (or already does) talk directly to another service's database instead of going through that service's API.
- A service's project file referencing another service's `Domain`/`Application`/`Infrastructure` project is the .NET-level equivalent — code from one service directly using another service's persistence/domain types, bypassing the API boundary entirely, is exactly how one service ends up implicitly coupled to another's schema.

Both are easy to introduce by accident in a solo, fast-moving build (e.g., adding a "just this once" reference to save writing an HTTP client) and hard to unwind later, which is why this gets checked automatically rather than relying on remembering the rule during a busy scaffold.

## When it fires

Any `Edit` or `Write` to a `.csproj` or `appsettings*.json` file located anywhere under `src/Services/<Service>/`. It does not fire on files outside `src/Services/` (e.g., `BuildingBlocks` or `Gateway` project files) — those aren't "a service" in the sense this rule is about.

## Invocation

No explicit form — see the hooks README's "Invocation model" section. Fires automatically, after the fact, on every `Edit`/`Write` to a `.csproj` or `appsettings*.json` under `src/Services/<Service>/`. Like `compose-healthcheck-lint`, it only warns, so "invocation" just surfaces a `systemMessage` for the assistant to weigh — a cross-service reference that's genuinely transient mid-refactor will still trigger the warning every time the file is saved in that state, since the script has no memory of previous runs or any notion of "in progress."

## Other details

- **Warns, never blocks.** A cross-service reference or a second connection string might exist transiently mid-refactor (e.g., while migrating a piece of logic between services) before being cleaned up in the same working session — blocking immediately would be more disruptive than the rule is worth for what's fundamentally a review-time concern, not a security boundary like the two hard-blocking hooks.
- **The connection-string check only understands the `ConnectionStrings` JSON shape** used by ASP.NET Core configuration conventions — a connection string supplied purely via environment variable (as Phase 1's actual deployment design uses, per Technical Documentation §7.2 — `IDENTITY_DB_CONNECTION`, `PATIENT_DB_CONNECTION`) won't be caught by this check at all, since there's no `appsettings.json` entry to count. This hook is most useful as a backstop against a config entry being added *in addition to* the env-var-based approach, or against `appsettings.json` being used directly instead of environment variables during early scaffolding.
- **The cross-service reference check is path-based, not semantic** — it only looks at where a `<ProjectReference>` points, not what the referenced project is actually used for in code. A reference could be flagged even if the code never ends up touching persistence at all (e.g., referencing another service's `Domain` project just to reuse a shared enum) — the hook can't distinguish "borrowing a type" from "reaching into another service's data," so it flags the reference itself and leaves the judgment call to whoever reads the warning.
- **Relies on the `src/Services/<Service>/...` path convention from §1.3** — like `solution-structure-reference-lint`, if a file doesn't follow that path shape, this hook has nothing to key off of and silently allows it.