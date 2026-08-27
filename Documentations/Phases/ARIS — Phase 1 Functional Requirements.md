# ARIS — Phase 1 Functional Requirements

**Document type:** Functional Requirements (what the system must do, from the user's perspective)
**Companion documents:** `ARIS — Phase 1 Technical Documentation.md` (how it gets built — architecture, data models, APIs, security, infrastructure), `ARIS — Phase 1 Detailed Plan.md` (execution plan — work breakdown, effort estimates, risks), `ARIS — Phase 1 Test Documentation.md` (how each requirement below is verified), `ARIS — Phase 1 UI Guidelines.md` (visual design system + mockup implementing these requirements)
**Source:** `ARIS — Complete Implementation and User Reference Documentation.md` v2.0, §5–§15 (personas), §90 (Definition of Done), §93–§96 (development model), §102 (Phase 1 scope)
**Status:** Draft — Phase 1 baseline

This document contains no implementation detail (no schemas, endpoints, token algorithms, or infrastructure). It defines only what an authorized user must be able to do by the end of Phase 1, and how success is verified.

---

## 1. Purpose and Scope

Phase 1 establishes the platform foundation: identity, authorization, application navigation, and basic patient lookup. It does **not** deliver any risk-adjustment intelligence — no HCC mapping, no gap detection, no RAF, no evidence search, no AI. Those capabilities are functionally out of scope until Phase 3 and Phase 4 (see §7).

The purpose of Phase 1, functionally, is to let an authorized user log into ARIS and find a patient. Everything else in this document exists to make that possible correctly and securely.

---

## 2. Actors in Scope

All six roles defined in the parent spec (§94) must exist and be distinguishable by the system in Phase 1, even though most roles have no role-specific functionality yet beyond access control:

| Role | Phase 1 functional relevance |
|---|---|
| Administrator | Owns user management — provisioning user accounts and assigning roles (minimally, via seeded/admin-created accounts in Phase 1) |
| Clinician | Can log in, search patients, view patient details |
| Coder | Can log in, search patients, view patient details |
| RiskAnalyst | Can log in, search patients, view patient details |
| Auditor | Can log in; view access is otherwise not yet defined (no audit UI exists until Phase 5) |
| Researcher | Can log in; no research-specific functionality exists yet (Phase 6) |

Role-specific workflows (pre-visit prep for Clinician, work queues for Coder, population analytics for RiskAnalyst, etc.) are out of scope until later phases — Phase 1 only needs the roles to exist and be enforced.

---

## 3. Functional Requirements

### 3.1 Authentication

| ID | Requirement | Actor | Acceptance Criteria | Priority |
|---|---|---|---|---|
| FR-1.1 | A user shall be able to log in with a username/identifier and password. | All roles | Given valid credentials, when submitted, then the user is authenticated and lands on the application shell. | Must |
| FR-1.2 | The system shall reject invalid credentials without revealing whether the username or password was incorrect. | All roles | Given an invalid username or wrong password, when login is attempted, then a generic "invalid credentials" message is shown — never "user not found" vs. "wrong password." | Must |
| FR-1.3 | An authenticated session shall expire automatically after a period of inactivity/time, requiring re-authentication. | All roles | Given an expired session, when the user takes an action, then they are returned to login without data loss they'd notice as a crash. | Must |
| FR-1.4 | A user shall be able to log out explicitly. | All roles | Given an active session, when the user selects "logout," then the session ends and protected pages/data become inaccessible. | Must |
| FR-1.5 | The system shall record each login success, login failure, and logout as an auditable event, even though no audit UI exists yet. | System | Given any login attempt or logout, then an event is recorded with actor (if known), outcome, and timestamp. | Should |

### 3.2 Authorization / Access Control

| ID | Requirement | Actor | Acceptance Criteria | Priority |
|---|---|---|---|---|
| FR-2.1 | The system shall restrict every page and every data request to authenticated users only. | All roles | Given no active session, when any protected page/URL is accessed directly, then the user is redirected to login, not shown data. | Must |
| FR-2.2 | The system shall restrict access to specific functionality based on the user's assigned role(s). | All roles | Given a user without the required role, when they attempt a restricted action, then access is denied. | Must |
| FR-2.3 | The system shall present a clear "not authorized" message when a logged-in user attempts an action their role doesn't permit — never a silent failure, a blank screen, or a generic crash. | All roles | Given insufficient role, when a restricted page is requested, then an explicit unauthorized page/message is shown. | Must |
| FR-2.4 | Authorization shall be enforced by the backend independently of what the UI shows or hides — a user must not be able to reach functionality by bypassing the UI. | All roles | Given direct API access without the required role, then the request is denied regardless of UI state. | Must |
| FR-2.5 | Administrators shall be able to assign one or more roles to a user. | Administrator | Given a user record, when an Administrator assigns a role, then that user's access reflects the new role going forward. | Should |

### 3.3 Application Shell & Navigation

| ID | Requirement | Actor | Acceptance Criteria | Priority |
|---|---|---|---|---|
| FR-3.1 | Once logged in, the user shall see a consistent application shell (navigation and identity of the logged-in user) on every screen. | All roles | Given an authenticated session, then the user's name/identity and a way to navigate/log out are visible on every page. | Must |
| FR-3.2 | The navigation shall only present options the user's role is permitted to use. | All roles | Given a role without access to a given area, then that area does not appear as a navigable option. | Should |
| FR-3.3 | The system shall present a placeholder dashboard landing page after login. | All roles | Given a successful login, then the user lands on a dashboard page (content beyond a landing placeholder is out of scope for Phase 1). | Must |
| FR-3.4 | The system shall show a clear "page not found" experience for invalid/unrecognized URLs within the application. | All roles | Given a URL that does not correspond to any screen, then a not-found page is shown instead of an error or blank page. | Should |

### 3.4 Patient Search

| ID | Requirement | Actor | Acceptance Criteria | Priority |
|---|---|---|---|---|
| FR-4.1 | An authorized user shall be able to search for a patient by identifying information (e.g., name or medical record number). | Clinician, Coder, RiskAnalyst, Administrator | Given a search term, when submitted, then matching patients are returned. | Must |
| FR-4.2 | Search results shall be presented in a way that remains usable regardless of result count (i.e., paginated, not one unbounded list). | Same as FR-4.1 | Given a result set larger than one page, then the user can navigate additional pages. | Must |
| FR-4.3 | The system shall clearly indicate when a search returns no matching patients, rather than showing an empty or ambiguous screen. | Same as FR-4.1 | Given a search with no matches, then an explicit "no results" state is shown. | Must |
| FR-4.4 | The system shall clearly indicate when a search is in progress. | Same as FR-4.1 | Given a search request in flight, then a loading indicator is shown until results or an error arrive. | Should |
| FR-4.5 | The system shall clearly communicate a failure to complete a search (e.g., service unavailable) without misrepresenting it as "no results." | Same as FR-4.1 | Given a backend failure during search, then an explicit error state is shown, distinct from "no results found." | Must |
| FR-4.6 | A user shall only be able to find patients they are authorized to access. | Same as FR-4.1 | Given the current Phase 1 authorization model (role-based, not yet patient-level), all patients are visible to any user holding an authorized role; patient-level restriction is out of scope until a later phase (see §7). | Must (as scoped) |

### 3.5 Patient Details

| ID | Requirement | Actor | Acceptance Criteria | Priority |
|---|---|---|---|---|
| FR-5.1 | An authorized user shall be able to open a specific patient's detail view from search results. | Clinician, Coder, RiskAnalyst, Administrator | Given a selected patient from search results, then their detail view opens. | Must |
| FR-5.2 | The patient detail view shall display core identifying and demographic information. | Same as FR-5.1 | Given a patient detail view, then identifier, name, date of birth, and sex (or equivalent core demographics) are visible. | Must |
| FR-5.3 | The patient detail view shall clearly indicate when requested patient data cannot be retrieved (e.g., service failure), rather than showing a blank or broken page. | Same as FR-5.1 | Given a failure to load patient details, then an explicit error state is shown. | Must |
| FR-5.4 | Clinical history, risk-adjustment opportunities, evidence, and related intelligence shall **not** appear in Phase 1 — the detail view is demographic-only at this stage. | Same as FR-5.1 | Given the patient detail view in Phase 1, then no gap, HCC, or evidence content is displayed (this is intentional and correct for this phase, not a defect). | Must (as scoped) |

---

## 4. User Stories (Phase 1)

- *As a Clinician*, I want to log in securely so that I can access ARIS with my own identity and permissions.
- *As a Clinician or Coder*, I want to search for a patient by name or MRN so that I can quickly locate the record I need.
- *As a Clinician or Coder*, I want to see a patient's basic demographics so that I can confirm I have the right patient before doing further review.
- *As any user*, I want to be clearly told when I'm not allowed to do something, rather than guessing why nothing happened.
- *As any user*, I want to be able to log out so that I can end my session, especially on a shared workstation.
- *As an Administrator*, I want to be able to create user accounts and assign roles so that staff can access ARIS appropriate to their job.

---

## 5. Business Rules

- A user has one or more of six defined roles: Administrator, Clinician, Coder, RiskAnalyst, Auditor, Researcher (§94).
- A user with no role assigned has no access to any protected functionality.
- Authentication failures never reveal whether the username exists.
- Patient-level access restriction (i.e., "which specific patients can this user see") does not exist yet in Phase 1 — access control in this phase is role-based only. This is a known, intentional scope boundary, not a gap to fix within Phase 1 (§95 introduces finer-grained authorization in a later phase).
- No clinical documentation, coding, or risk-adjustment decision can be made in Phase 1 — there is nothing yet to decide on. This phase is discovery/navigation only.

---

## 6. Non-Functional (User-Facing) Requirements

These are functional from the user's point of view (they describe an experience the user notices), even though they are validated with technical measurements:

| ID | Requirement | Target | Source |
|---|---|---|---|
| NFR-1 | Patient search shall return results promptly enough to feel interactive. | Consistent with §74's engineering target of sub-second response for typical queries | §74 |
| NFR-2 | Patient lookup by identifier shall be fast. | Consistent with §74's target of a few hundred milliseconds | §74 |
| NFR-3 | The system shall never display another user's or another patient's data as a result of a UI or navigation error. | No cross-account/cross-patient data leakage, ever | §50–§51 |
| NFR-4 | The system shall not expose sensitive technical detail (stack traces, internal identifiers, connection info) in any user-facing error. | Generic, safe error messaging only | §50–§51 |

Targets are stated qualitatively here; specific numeric thresholds and how they're measured belong in the technical/Detailed Plan document, not this one.

---

## 7. Out of Scope for Phase 1

Explicitly **not** part of Phase 1's functional requirements — these belong to later phases per the parent spec's roadmap, and should not be requested or implied by any Phase 1 acceptance test:

- Risk-adjustment gap identification or display (Phase 3)
- HCC mapping display (Phase 3)
- RAF score calculation or display (Phase 3)
- Clinical evidence search, keyword or semantic (Phase 2 / Phase 4)
- Encounter, diagnosis, procedure, or note history for a patient (Phase 2)
- "Ask ARIS" / AI assistant / Explain-Gap experience (Phase 4)
- Accept/reject/defer decisions, feedback capture (Phase 5)
- Persona-specific workflows: pre-visit prep, coder work queue, population analytics, audit reconstruction (Phase 5)
- Patient-level (as opposed to role-level) access control (later phase)
- Self-service registration or password reset (not required for any Phase 1 exit criterion)
- Any AI/LLM-generated content of any kind

---

## 8. Phase 1 Functional Acceptance (Definition of Done)

Phase 1 is functionally complete when an authorized user can, without needing any explanation or workaround:

1. Log in with valid credentials and reach the application shell.
2. Be refused login with invalid credentials, via a generic message.
3. See navigation appropriate to their role.
4. Attempt a restricted action and receive a clear "not authorized" response, not a crash or silent no-op.
5. Search for a patient and get correct results, a correct empty state, or a correct error state — never an ambiguous one.
6. Open a patient and see accurate core demographics.
7. Log out and be unable to access protected content or data afterward.

This list is a subset of the platform-wide Definition of Done in the parent spec (§90); the remaining items in §90 (risk opportunities, evidence, AI answers, decisions, audit, ground-truth evaluation) become achievable only in later phases.

---

## 9. Traceability

| Requirement group | Parent spec reference | Personas referenced |
|---|---|---|
| Authentication (§3.1) | §94, §96 | All |
| Authorization (§3.2) | §95 | All |
| App shell/navigation (§3.3) | §60, §96 | All |
| Patient search (§3.4) | §60, §97 | Clinician (§6.1), Coder (§7), RiskAnalyst (§8) |
| Patient details (§3.5) | §60, §98 (timeline is Phase 2, demographics only here) | Clinician (§6.1), Coder (§7) |
| Definition of Done subset (§8) | §90 | All |
