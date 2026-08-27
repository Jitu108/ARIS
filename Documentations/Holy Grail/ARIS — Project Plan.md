# ARIS — Project Plan

**Derived from:** `ARIS — Complete Implementation and User Reference Documentation.md` (v2.0)
**Assumptions:** Solo developer, vertical-slice delivery, Docker-first local environment
**Status:** Draft — durations are estimates and should be revisited after Phase 1

---

## 1. Purpose

This plan translates the ARIS functional specification into an executable, phase-by-phase build sequence. It does not redefine scope — it sequences the roadmap already fixed in the source document (§93–§117) into concrete milestones, exit criteria, and a checklist a solo developer can work through.

## 2. Guiding Principles (non-negotiable, per source doc)

- **Vertical slices only.** Every backend capability ships with its Angular UI in the same slice. No backend-only phases with UI deferred (§93).
- **Identity first.** IdentityService + its UI is the first thing built, not a Phase 5/6 afterthought (§94, §96).
- **Deterministic before generative.** HCC mapping and the Gap Engine (rules-based) must work and be trustworthy *before* RAG/LLM/agentic layers are added (§104 before §105, §113).
- **AI failure must never break core function.** Graceful degradation: canonical data → deterministic rules → keyword search → semantic search → LLM explanation → agentic reasoning (§78–§79). Each layer must survive the layers above it failing.
- **Evidence-first, human-in-the-loop.** No unsupported conclusions; humans retain final decision authority throughout (§37, §81).
- **Docker-first.** Every slice should be runnable via Docker Compose, not just from the IDE (§108).

---

## 3. Roadmap at a Glance

| Phase | Focus | Key Backend | Key Angular | AI | Est. Duration (solo) |
|---|---|---|---|---|---:|
| 1 | Platform, Identity & UI Foundation | IdentityService, PatientService, HccMappingService (stub), GapEngineService (stub), Ocelot | Login, shell, patient search/details | — | 5–7 weeks |
| 2 | Clinical Data, Ingestion & Search | DataIngestService, RabbitMQ, Outbox, Indexer, OpenSearch | Advanced search, timeline, evidence view | — | 5–7 weeks |
| 3 | Deterministic Risk Intelligence | HccMappingService (full), GapEngineService (full), RafCalculationService | Risk dashboard, gap review, RAF breakdown | — | 7–9 weeks |
| 4 | RAG & Agentic Intelligence | Embedding Worker, Qdrant, Agent Orchestrator, guardrails | Ask ARIS, Explain Gap, evidence/citations UI | LLM/RAG/agents | 7–9 weeks |
| 5 | Complete Clinical/Coding Workflows | Review, assignment, feedback, audit APIs | Persona workflows (clinician/coder/analyst/auditor) | Advanced workflow agents | 6–8 weeks |
| 6 | Enterprise, Scale & Research | AWS readiness, observability, advanced security | Analytics/admin/research UI | Multi-agent research eval | 8–10 weeks (open-ended) |

**Total to end of Phase 5 (production-usable platform): ~30–40 weeks.**
Phase 6 is ongoing/iterative rather than a hard finish line — treat it as a continuous track once Phase 5 exit criteria are met.

These are full-time-equivalent estimates for one developer covering backend, frontend, infra, and AI integration. Part-time pace should scale proportionally. Re-baseline after Phase 1 once actual velocity is known.

---

## 4. Vertical-Slice Sequence (Phase 1–4 detail, per §96–§100)

Build in this exact order — each slice must work end-to-end before the next begins:

1. **Identity slice** — Angular login → IdentityService → JWT → auth state → Ocelot → protected route.
2. **Patient search slice** — Angular search → Ocelot → PatientService → SQL Server → results list (validates DTOs, pagination, loading/empty states).
3. **Patient timeline slice** — Patient details UI → encounters/diagnoses → SQL Server → longitudinal view.
4. **Risk dashboard slice** — Risk dashboard → GapEngineService → PatientService → HccMappingService → gap results UI.
5. **Explain-gap slice** — Explain Gap UI → Agent Orchestrator → OpenSearch → Qdrant → LLM → evidence-grounded explanation UI.

Slices 1–3 fall in Phase 1–2; slice 4 anchors Phase 3; slice 5 anchors Phase 4.

---

## 5. Phase-by-Phase Plan

### Phase 1 — Platform, Identity & UI Foundation (§102)
**Goal:** A working, authenticated, deployable shell — not just a backend skeleton.

- Backend: IdentityService (JWT, roles: Administrator/Clinician/Coder/RiskAnalyst/Auditor/Researcher), PatientService, HccMappingService and GapEngineService as thin stubs, Ocelot gateway, BuildingBlocks, health checks, OpenAPI.
- Angular: login, app shell (header/sidebar), route guards, HTTP auth interceptor, dashboard shell, patient search, patient details, unauthorized/not-found pages.
- Infra: Docker Compose, SQL Server container, service networking, Docker Hub image workflow.
- **Exit criteria (§102):** authenticate, get/use a token, navigate protected routes, search + view a patient, logout, get correct unauthorized responses.
- **Risk:** underestimating RBAC/JWT plumbing time — treat auth as its own mini-project, not a quick add-on.

### Phase 2 — Clinical Data, Ingestion & Search (§103)
**Goal:** Real clinical data flowing end-to-end into searchable evidence.

- Backend: DataIngestService (API + file ingestion: CSV/JSON/XML/FHIR bundles), RabbitMQ, Outbox pattern, Indexer Worker, OpenSearch.
- Data: Patient, Encounter, Diagnosis, Procedure, Clinical Note, Provider as canonical entities, with validation (schema, referential integrity, code/date checks, duplicate detection) and provenance capture (§19–§20).
- Angular: patient search with advanced filters, timeline, diagnosis/encounter history, evidence search.
- **Exit criteria:** a record moves Source → DataIngestService → SQL Server → Outbox → RabbitMQ → Indexer → OpenSearch → visible in Angular.
- **Risk:** Outbox/event-contract correctness — get this right here since Phases 3–6 all depend on reliable event flow.

### Phase 3 — Deterministic Risk Intelligence (§104)
**Goal:** The authoritative, trustworthy rules-based baseline — built *before* any generative AI.

- HccMappingService: full ICD→HCC mappings, model versions, effective dates, version-aware APIs.
- GapEngineService: historical gap detection, recapture logic, temporal reasoning, evidence association, gap status/priority, contradiction indicators.
- RafCalculationService (new, separate from GapEngineService per §707–§952): versioned RAF model config, demographic + validated HCC factors, hierarchies, interactions, coefficients, patient RAF calculation, component breakdown, previous-RAF comparison, delta, provenance/reproducibility.
- Angular: risk dashboard, patient risk profile, open gaps, gap detail, evidence view, HCC mapping view, gap status actions, RAF score/breakdown/delta UI, potential-RAF-impact view for gaps.
- **Exit criteria:** Open Patient → View Risk Profile → View Gap → Understand Why → Review Evidence → Make a Human Decision, entirely without LLM involvement.
- **Risk:** this is the largest phase — HCC versioning + RAF correctness is where most domain complexity lives. This is also the system's designated "research baseline" (§113), so get it stable before moving on.

### Phase 4 — RAG & Agentic Intelligence (§105)
**Goal:** Evidence-grounded generative intelligence layered on top of (not replacing) Phase 3.

- Backend: Embedding Worker, Qdrant, hybrid (keyword + vector) retrieval, Agent Orchestrator, LLM provider abstraction, guardrails (input/retrieval/output), AI audit trail, evidence ranking.
- Agent tools (registered, permission-controlled, no direct DB access): `get_patient`, `get_diagnoses`, `get_encounters`, `get_gaps`, `map_icd_to_hcc`, `search_evidence`, `semantic_search`, `get_hcc_details`, `validate_evidence`.
- Angular: Ask ARIS, Explain Gap, evidence cards, AI reasoning summary, citations, confidence, limitations, recommended human action.
- **Exit criteria:** user asks "Why is this gap showing?" and gets a response grounded in retrievable, citable, patient-specific evidence.
- **Risk:** guardrails and hallucination control are easy to under-scope — budget real time for output validation (unsupported-diagnosis detection, citation validation, PHI leakage checks), not just the happy-path RAG pipeline.

### Phase 5 — Complete Clinical, Coding & Review Workflows (§106)
**Goal:** Turn individual capabilities into complete, persona-specific end-to-end workflows.

- Clinician: pre-visit summary, risk opportunities, evidence review, AI assistance, encounter-oriented review.
- Coder: work queue, gap prioritization, evidence review, HCC mapping, decision capture.
- Risk Analyst: population dashboard, HCC analytics, gap analytics, trends, workload analysis.
- Auditor: evidence audit, AI reasoning audit, human decision audit, provenance reconstruction.
- Backend: review workflows, assignments, comments, review events, feedback capture, audit APIs.
- **Exit criteria:** each primary persona (Clinician, Coder, Risk Analyst, Auditor) can complete their core workflow end-to-end (cross-check against §90 Definition of Done, items 1–16).

### Phase 6 — Enterprise, Scale & Research (§107)
**Goal:** Production-grade + research-grade platform. Treat as an ongoing track, not a fixed sprint.

- Security: OIDC/OAuth2, advanced RBAC, ABAC, MFA integration, org-level and patient-level authorization, secrets management, encryption.
- AWS: EKS/ECS, RDS SQL Server, networking, load balancers, WAF, KMS, Secrets Manager, S3, CloudWatch, OpenSearch, Amazon MQ (RabbitMQ-compatible contracts retained).
- Observability: OpenTelemetry, distributed traces, structured logs, metrics, dashboards, alerts.
- Research: experiment registry, dataset versioning, ground-truth datasets, model/retrieval/agent comparison, ablation studies, human-feedback evaluation (§62–§70 give the full metric set to implement here).

---

## 6. Cross-Phase Workstreams (don't defer entirely to Phase 6)

Some concerns are listed under Phase 6 in the source doc but should be seeded much earlier or they become expensive to retrofit:

- **Audit trail (§54, §12):** start recording user/action/timestamp/tools-called/evidence/rule-version/model-version/decision from Phase 1 onward, even in a minimal form.
- **Versioning discipline (§71):** API, schema, HCC model, rule, prompt, LLM, embedding-model, and UI versions should be tracked from the moment each concept is introduced (HCC/rule versions in Phase 3, prompt/LLM/embedding versions in Phase 4).
- **PHI-safe logging (§51):** apply from Phase 1, since logging habits set early are hard to walk back.
- **Feature flags (§72):** introduce alongside the first experimental component (likely Phase 3's rule engine or Phase 4's retrieval strategy), not bolted on later.

---

## 7. Milestone Summary (cumulative, solo dev)

| Milestone | End of | Cumulative Elapsed (est.) |
|---|---|---:|
| M1 — Authenticated shell + patient search live | Phase 1 | ~6 weeks |
| M2 — Full ingestion → search pipeline live | Phase 2 | ~12 weeks |
| M3 — Deterministic gap engine + RAF live (research baseline established) | Phase 3 | ~20 weeks |
| M4 — Agentic "Ask ARIS" / Explain Gap live | Phase 4 | ~28 weeks |
| M5 — All personas have complete workflows (Definition of Done met, §90) | Phase 5 | ~35 weeks |
| M6 — Enterprise/AWS-ready + research framework operational | Phase 6 | ongoing |

---

## 8. Key Risks (solo-developer specific)

| Risk | Mitigation |
|---|---|
| Context-switching cost across backend/UI/infra/AI in one slice | Keep slices small; finish one full vertical slice before starting the next rather than parallelizing partial work |
| Phase 3 (HCC/RAF) complexity underestimated | Treat RafCalculationService as its own sub-project; validate against a small hand-built ground-truth set before wiring to UI |
| Phase 4 guardrails skipped under time pressure | Guardrails are exit-criteria-relevant, not optional polish — don't ship "Ask ARIS" without output validation |
| Scope creep from Phase 6 items (AWS/OIDC/observability) pulled forward | Only pull forward the cross-phase workstream items in §6 above; defer the rest |
| No dedicated QA | Add integration tests at each vertical-slice boundary as the substitute for a QA function |

---

## 9. Definition of Done (platform-level, §90)

ARIS is functionally mature when an authorized user can: search a patient → open longitudinal history → view risk opportunities → understand why → see supporting/contradictory evidence → see temporal history and HCC mapping/version → ask ARIS a natural-language question → get an evidence-grounded, citable answer → accept/reject/defer → give feedback → see the decision recorded → audit how the result was generated → measure system performance against expert-reviewed ground truth. This should be the acceptance checklist at the end of Phase 5.

---

## 10. Immediate Next Steps (Phase 1 kickoff)

1. Set up repo/solution structure: .NET solution with Clean Architecture + BuildingBlocks, Angular workspace, Docker Compose skeleton.
2. Stand up SQL Server + Ocelot + health checks in Docker Compose.
3. Build IdentityService (auth, JWT issuance, roles/claims) end-to-end with its Angular login/auth-state/guard/interceptor slice — this is Slice 1 and blocks everything else.
4. Only after Slice 1 is fully working (login → protected route → logout), start PatientService + patient search (Slice 2).

---

*This plan should be revisited at the end of each phase to re-baseline estimates and confirm exit criteria were actually met before proceeding — do not start Phase N+1 work while Phase N exit criteria are unmet, per the vertical-slice principle.*
