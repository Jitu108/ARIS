# ARIS — Technical Documentation

**Document type:** Technical Documentation / Software Architecture Document (whole-project) — how the system is built, across all six phases
**Companion documents (in `Documentations/Holy Grail/`):**
- `ARIS — Complete Implementation and User Reference Documentation.md` — the functional specification and source of truth for scope
- `ARIS — Project Plan.md` — phase sequencing, milestones, effort estimates
**Companion documents (in `Documentations/Phases/`, phase-scoped detail):**
- `ARIS — Phase 1 Functional Requirements.md` / `ARIS — Phase 1 Technical Documentation.md` — the fully detailed design for what exists today; future phases get their own equivalent pair as they're started
**Source:** `ARIS — Complete Implementation and User Reference Documentation.md` v2.0, primarily §16–§59, §71–§79, §108–§114
**Status:** Draft — whole-project architecture baseline. This document describes the **target end-state architecture**; components are tagged with the phase that introduces them, and nothing here should be built ahead of its tagged phase.

This document is the single technical reference for the entire ARIS platform. Phase-specific technical documents (like the Phase 1 one) go deeper on a given phase's exact schemas/endpoints; this document's job is to keep the cross-phase architecture coherent so no phase is designed in isolation from the others.

---

## 1. Architectural Principles

These hold across every phase and every service (§93, §108–§115):

1. **Vertical slices.** Every backend capability ships with its Angular UI in the same phase — never backend-only with UI deferred.
2. **Deterministic before generative.** The rules-based Gap Engine and HCC mapping (Phase 3) must be trustworthy before RAG/agentic layers (Phase 4) are added on top.
3. **Graceful degradation.** AI failure must never remove core clinical/risk-adjustment functionality (§78–§79) — see §11.
4. **Service ownership.** Each service owns its own persistence boundary; no service reaches into another's database (§111).
5. **Evidence-first, human-authoritative.** No AI conclusion is presented without evidence and without a human retaining final decision authority (§37, §81).
6. **Everything material is versioned.** API, schema, HCC model, rule, prompt, LLM, embedding model, retrieval config, agent config, UI (§71) — see §13.
7. **Docker-first.** Every phase must remain runnable via Docker Compose, not only from an IDE (§108).

---

## 2. End-to-End Architecture

```text
                                ┌────────────┐
                                │ Angular UI │
                                └─────┬──────┘
                                      ▼
                              ┌────────────────┐
                              │ Ocelot Gateway │
                              └────────┬───────┘
                ┌──────────────────────┼──────────────────────┐
                ▼                      ▼                      ▼
       ┌─────────────────┐    ┌────────────────┐    ┌──────────────────┐
       │ IdentityService │    │ PatientService │    │ GapEngineService │
       └─────────────────┘    └───────┬────────┘    └────────┬─────────┘
                                       │                      │
                                       ▼                      ▼
                                ┌────────────┐      ┌───────────────────┐
                                │ SQL Server │      │ HccMappingService │
                                └─────┬──────┘      └───────────────────┘
                                      │                        │
                                      │                        ▼
                                      │              ┌──────────────────────┐
                                      │              │ RafCalculationService│
                                      │              └──────────────────────┘
                                      ▼
                                 ┌──────────┐
                                 │ RabbitMQ │  (Outbox pattern)
                                 └────┬─────┘
                     ┌─────────────────┼─────────────────┐
                     ▼                 ▼                 ▼
               ┌──────────┐      ┌──────────┐      ┌─────────────┐
               │ Indexer  │      │Embedding │      │ Analytics / │
               │ Worker   │      │ Worker   │      │   Audit     │
               └────┬─────┘      └────┬─────┘      └─────────────┘
                    ▼                 ▼
              ┌────────────┐    ┌────────────┐
              │ OpenSearch │    │   Qdrant   │
              └──────┬─────┘    └─────┬──────┘
                     └────────┬───────┘
                               ▼
                     ┌──────────────────┐
                     │ Agent Orchestrator│──→ LLM (provider-abstracted)
                     └─────────┬────────┘
                               ▼
                   Evidence-Grounded Explanation
                               ▼
                        Human Reviewer → Feedback → Evaluation Layer
```

This is the full target architecture (§114). Phase 1 delivers only the top third (Angular, Ocelot, IdentityService, PatientService, and stub HccMappingService/GapEngineService with no logic yet).

---

## 3. Service Catalog

| Service | Responsibility | Owned data | Introduced | Matured |
|---|---|---|---|---|
| IdentityService | Authentication, roles/claims, token issuance, auth audit | `IdentityDb` | Phase 1 | — |
| PatientService | Patient demographic and canonical record storage | `PatientDb` | Phase 1 (demographics) | Phase 2 (full canonical model) |
| Ocelot Gateway | Single entry point, routing, header/correlation forwarding | none | Phase 1 | Phase 6 (WAF/LB in front) |
| HccMappingService | Version-aware ICD→HCC mapping | `HccMappingDb` | Phase 1 (stub) | Phase 3 (real mappings) |
| GapEngineService | Deterministic risk-adjustment gap detection | `GapEngineDb` | Phase 1 (stub) | Phase 3 (real logic) |
| DataIngestService | API/file ingestion, validation, provenance | writes to `PatientDb` via API, not direct DB access | Phase 2 | — |
| Indexer Worker | Consumes ingestion/domain events, writes to OpenSearch | OpenSearch indices (derived, reconstructible) | Phase 2 | — |
| RafCalculationService | Versioned patient-level RAF calculation | `RafCalculationDb` | Phase 3 | — |
| Embedding Worker | Chunks clinical text, generates embeddings | Qdrant collections (derived, reconstructible) | Phase 4 | — |
| Agent Orchestrator | Multi-step agentic reasoning over registered tools | none (calls other services' APIs) | Phase 4 | Phase 5 (workflow agents) |
| Analytics service(s) | Population-level metrics | read models, possibly derived | Phase 5 | Phase 6 (research analytics) |
| Audit Processor | Consumes review/decision events into an audit store | `AuditDb` | Phase 5 | — |

Every service independently validates JWTs and enforces its own authorization — the gateway is never the sole security boundary (§95), a rule already fixed in the Phase 1 Technical Documentation and binding for every service added afterward.

---

## 4. Canonical Data Model

The target canonical patient model (§21), fully realized by end of Phase 2/3:

```text
Patient
 ├── Provider relationships
 ├── Encounters
 │    ├── Diagnoses
 │    ├── Procedures
 │    ├── Medications
 │    └── Notes
 ├── Conditions
 ├── Claims
 └── Evidence (derived — see §7)
```

Each service owns only the slice of this model relevant to its responsibility (§111):

- `PatientService` — Patient, Provider, Encounter, Diagnosis, Procedure, Medication, Note (canonical structured/semi-structured storage).
- `GapEngineService` — Gap records referencing Patient/Diagnosis IDs by reference, not by owning that data.
- `RafCalculationService` — RAF calculation results and provenance, referencing validated HCCs by ID.
- `HccMappingService` — ICD↔HCC mapping tables, independent of any specific patient.

Every ingested record retains provenance (§20): source system, source record ID, ingestion timestamp, original timestamp, data version, transformation version, ingestion job ID. This is enforced at `DataIngestService`'s validation stage (Phase 2) and is non-negotiable — invalid or unprovenanced data must not silently enter canonical storage (§19).

---

## 5. API Gateway & Communication

### 5.1 Ocelot route table (target, all phases)

```text
/identity/*   → IdentityService
/patients/*   → PatientService
/hcc/*        → HccMappingService
/gaps/*       → GapEngineService
/raf/*        → RafCalculationService
/ingest/*     → DataIngestService
/search/*     → (routes to Indexer/OpenSearch-backed read API)
/agent/*      → Agent Orchestrator
```

Angular never has knowledge of internal container addresses or ports (§55) — it only ever calls the gateway base URL.

### 5.2 Communication modes (§110)

- **Synchronous (HTTP/REST via Ocelot or direct internal call):** used when an immediate response is required — e.g., `Angular → Ocelot → PatientService`, `GapEngineService → HccMappingService`.
- **Asynchronous (RabbitMQ):** used when processing can be decoupled — e.g., ingestion fan-out to Indexer/Embedding/Analytics/Audit consumers.

A service must never call another service's database directly under either mode (§111).

---

## 6. Event-Driven Architecture (Phase 2+)

### 6.1 Reliability pattern

Every event-publishing write uses the Outbox Pattern (§57) so business data and its corresponding event commit atomically:

```text
Business Data + Outbox Event → Atomic SQL Transaction → Outbox Processor → RabbitMQ
```

This prevents data/event divergence — a write can never succeed while its event silently fails to publish, or vice versa.

### 6.2 Event catalog

| Event | Publisher | Consumers | Introduced |
|---|---|---|---|
| `PatientIngested` / `PatientCreated` | DataIngestService | Indexer Worker | Phase 2 |
| `EncounterCreated` | DataIngestService | Indexer Worker | Phase 2 |
| `DiagnosisCreated` / `DiagnosisIngested` | DataIngestService | Indexer Worker, Embedding Worker, Analytics, Audit Processor | Phase 2 |
| `ClinicalNoteCreated` | DataIngestService | Indexer Worker, Embedding Worker | Phase 2 |
| `EmbeddingCreated` | Embedding Worker | Analytics | Phase 4 |
| `GapDetected` | GapEngineService | Analytics, Audit Processor | Phase 3 |
| `GapReviewed` | (review workflow, Phase 5) | Analytics, Audit Processor | Phase 5 |

Event contracts (payload schema) must be independently versioned per §13 — a consumer must be able to tell which schema version an event was published under.

### 6.3 Scaling

RabbitMQ allows ingestion and downstream consumers to scale independently of the services that publish events — e.g., Indexer Worker instances can scale under load without PatientService or DataIngestService needing to scale in lockstep (§75).

---

## 7. Search & Retrieval Architecture (Phase 2 keyword, Phase 4 semantic)

### 7.1 Two complementary mechanisms (§29–§30)

- **Keyword/structured search (OpenSearch):** patient/ICD/HCC/provider/date filtering, exact-terminology note search.
- **Semantic search (Qdrant):** vector retrieval for meaning-based queries (e.g., "find evidence suggesting chronic kidney disease") that keyword search alone would miss.

### 7.2 Hybrid retrieval flow

```text
User Question → [Keyword Search] + [Vector Search] → Evidence Fusion → Ranking/Filtering → Evidence Set
```

### 7.3 Embedding pipeline (Phase 4)

```text
Document → Chunking → PHI/authorization validation → Embedding model → Vector → Qdrant
```

Vector metadata must carry patient, date, encounter, document type, and source so retrieval can be filtered, not just ranked (§59).

### 7.4 Index metadata (OpenSearch)

Every indexed document preserves: patient ID, encounter ID, diagnosis ID, source, date, ICD, HCC, text, provenance, and index version (§58) — the index version field is what makes index rebuilds and A/B index comparisons possible later.

### 7.5 Reconstructibility

Both OpenSearch and Qdrant are **derived, reconstructible stores** — canonical clinical data in SQL Server remains the single source of truth. A search or vector index can always be rebuilt from canonical data plus the event log; it is never itself authoritative (§77).

---

## 8. Risk Intelligence Architecture (Phase 3)

This is the deterministic core the rest of the system depends on (§113 — it is the designated research baseline).

### 8.1 HccMappingService

```text
ICD → HCC Model Version → HCC → HCC Description
```

Never assume a mapping is universally valid across model versions (§22). Every mapping query must specify a model version explicitly; there is no "default" mapping.

### 8.2 GapEngineService

Answers: *"Based on structured data and configured rules, which risk-adjustment opportunities should be considered for this patient?"* (§24)

Gap categories the engine must distinguish (§25): historical gap, documentation gap, coding gap, evidence gap, contradiction gap, recapture gap, suspected condition.

**Temporal reasoning** is central — absence of current-year documentation is not treated as evidence of resolution. The engine evaluates last known documentation, documentation frequency, current evidence, explicit resolution, contradictory evidence, time elapsed, and applicable risk-model rules (§26).

**Evidence model** — every gap carries an evidence graph (§27):
```text
Gap → ICD Evidence, Encounter Evidence, Clinical Note Evidence, Procedure Evidence,
      Laboratory Evidence, Medication Evidence, Historical Evidence, Contradictory Evidence
```

**Evidence strength** is classified, not binary (§28) — explicit provider diagnosis and Assessment/Plan entries rank very high; NLP inference ranks low unless corroborated by another source.

**Gap lifecycle** (§43):
```text
Detected → Prioritized → Reviewed → Evidence Evaluated → Human Decision → {Resolved | Rejected | Deferred}
```
A gap must never simply disappear without a recorded reason.

**Contradiction detection** (§44) — the engine surfaces conflicting evidence (e.g., a diagnosis in one note, an explicit denial in another) rather than silently picking one side.

### 8.3 RafCalculationService

A **separate service from GapEngineService**, with a distinct question (§707–§952):

> GapEngineService asks: *which opportunities should be reviewed?*
> RafCalculationService asks: *given validated demographic and HCC factors under a specific model/version, what RAF score results?*

```text
Patient Clinical Data
   → Diagnosis/Evidence Validation
   → ICD-10-CM → HCC Mapping
   → HCC Eligibility
   → HCC Hierarchy / Interaction Rules
   → Demographic Factors
   → Disease/HCC Coefficients
   → Interactions
   → RAF Calculation
   → Patient RAF Score
   → RAF Change/Delta
   → Explanation & Evidence
```

Every RAF result must carry a full calculation context so it is reproducible: Risk Model, Model Version, Payment Year, Segment, Coefficient Version, Rule Version, Patient Data Snapshot, Calculation Version, Calculation Timestamp. **A RAF score must never be presented without its model/version.** The exact coefficients and equations come from the authoritative risk-adjustment model being implemented — they are configuration, never hardcoded as universal rules.

RAF opportunity analysis (potential RAF impact of an unresolved gap) must be explicitly labeled as a **potential model-based impact**, never as a guaranteed financial outcome, and must distinguish current calculated RAF from potential RAF pending human validation.

---

## 9. Agentic AI Architecture (Phase 4)

### 9.1 Agent Orchestrator

The agent never accesses databases directly — it calls a registered, permission-controlled tool set only (§31):

```text
get_patient() · get_diagnoses() · get_encounters() · get_gaps() · map_icd_to_hcc()
search_evidence() · semantic_search() / retrieve_semantic_evidence() · get_hcc_details()
evaluate_evidence() · validate_evidence() · generate_explanation()
```

### 9.2 Planning pattern

A complex question decomposes into a bounded plan (§32–§33), e.g.:

```text
Question → Plan → Retrieve patient context → Retrieve gaps → Retrieve HCC mapping
→ Search evidence (keyword + semantic) → Evaluate evidence → Check contradictions
→ Generate response → Validate response
```

The agent must not execute arbitrary or unregistered actions.

### 9.3 Structured agent output contract

Every agent response conforms to a fixed shape — free-form unsupported conclusions are not permitted (§34):

```json
{
  "conclusion": "...",
  "confidence": 0.86,
  "supportingEvidence": [],
  "contradictoryEvidence": [],
  "reasoningSummary": "...",
  "recommendedHumanAction": "...",
  "limitations": []
}
```

### 9.4 AI response authority hierarchy

```text
Patient Data → Verified Evidence → Deterministic Rules → Clinical/Risk Model Metadata → LLM Reasoning
```

LLM reasoning never overrides authoritative structured information without explicitly stating the conflict (§53).

### 9.5 Guardrails (§52)

- **Input:** prompt-injection detection, malicious-instruction detection, PHI handling controls, input validation.
- **Retrieval:** patient authorization filtering, source validation, evidence provenance checks.
- **Output:** unsupported-diagnosis detection, unsupported-coding detection, hallucination detection, citation validation, PHI-leakage detection.

Guardrails are exit-criteria-relevant for Phase 4, not optional polish layered on afterward.

### 9.6 Confidence model

Confidence is not raw LLM confidence — it combines evidence quality, evidence quantity, temporal consistency, coding consistency, source reliability, rule confidence, retrieval relevance, a contradiction penalty, and LLM reasoning confidence (§36). The exact weighting is a research-tunable parameter (see §14).

---

## 10. Security Architecture

### 10.1 Progression across phases

| Concern | Phase 1 | Later phase |
|---|---|---|
| Authentication | JWT, RS256, refresh rotation | Phase 6: OIDC/OAuth2 external IdP |
| Authorization | RBAC (role-based) | Phase 6: ABAC, patient-level, organization-level |
| Secrets | Env vars / Docker secrets | Phase 6: AWS Secrets Manager, KMS |
| Encryption | TLS in transit (dev-grade) | Phase 6: full at-rest encryption, managed key rotation |
| Audit | Auth events only | Phase 5: full decision/AI-reasoning audit trail |

### 10.2 Constant across all phases

- The Angular application is never the security boundary — every backend service independently validates tokens and enforces authorization (§95).
- PHI is excluded from logs, exception messages, metrics, and trace attributes by convention from Phase 1 onward (§51) — this is a habit, not a Phase 6 retrofit.
- AI prompts carry only the minimum information necessary for the task (§51).
- Least privilege and data isolation apply to every service's database credentials, not only to end-user roles.

### 10.3 Full authorization model (target state, §95)

```text
Clinician    → access authorized patients
Coder        → access authorized coding population
RiskAnalyst  → access authorized population analytics
Auditor      → access audit information
Researcher   → access approved de-identified datasets
```

Patient-level and organization-level scoping (deciding *which specific patients* a given user may see, not just *which features*) is explicitly deferred past Phase 1 and should be layered onto the existing RBAC checks as ABAC policies, not as a parallel authorization system.

---

## 11. Reliability & Graceful Degradation

### 11.1 Layered functionality (§79)

```text
Level 1: Canonical Data
Level 2: Deterministic Risk Rules
Level 3: Keyword Evidence
Level 4: Semantic Evidence
Level 5: LLM Explanation
Level 6: Agentic Reasoning
```

Failure at Level 6 must never invalidate Levels 1–5. This is enforced architecturally, not just as a design aspiration — each layer's API must be callable/usable independently of the layers above it.

### 11.2 Specific failure behaviors (§78)

| Failure | Required behavior |
|---|---|
| PatientService unavailable | Gap request fails clearly — never fabricate patient data |
| HccMappingService unavailable | Gap processing indicates mapping unavailable, does not guess a mapping |
| RabbitMQ unavailable | Outbox events remain pending (not lost) until the broker recovers |
| OpenSearch unavailable | Canonical clinical data remains available; evidence search reports degraded functionality |
| LLM unavailable | Deterministic gap functionality remains fully operational |

### 11.3 Disaster recovery (§77)

- Database backup (canonical data — authoritative, must be backed up properly)
- Event replay (rebuild downstream state from the event log)
- Search-index rebuild, vector-index rebuild (both derived/reconstructible per §7.5 — do not treat their backups with the same rigor as canonical DB backups, but do ensure they *can* be rebuilt)
- Configuration and audit preservation

---

## 12. Observability Architecture

### 12.1 Tracing

Every important transaction should be reconstructible as a distributed trace (§73):

```text
User Request → Ocelot → GapEngine → PatientService → HccMappingService → Search → Agent → LLM
```

The correlation-ID propagation convention established in Phase 1 (generated at the gateway, forwarded through every downstream call) is what OpenTelemetry instrumentation in Phase 6 attaches to — it must not be introduced retroactively.

### 12.2 Stack (Phase 6, instrumented incrementally)

OpenTelemetry, structured logs, metrics, dashboards, alerts (§107).

### 12.3 Performance targets (engineering targets, not clinical guarantees — §74)

| Operation | Target |
|---|---:|
| Patient lookup | < 300 ms |
| Gap retrieval | < 1 sec |
| Evidence search | < 1 sec |
| Agent explanation | < 10 sec |
| Bulk ingestion | configurable records/sec |

---

## 13. Versioning & Configuration Management

### 13.1 Independently versioned dimensions (§71)

```text
API Version · Schema Version · HCC Model · Mapping Version · Rule Version
Prompt Version · LLM Version · Embedding Model · Retrieval Configuration
Agent Configuration · UI Version
```

Every AI conclusion and every RAF/gap result must be traceable back to the specific values of the dimensions above that produced it (§49, §68).

### 13.2 Feature flags (§72)

Used to control experimental features under controlled rollout: new gap rule, new HCC model, new LLM, new embedding model, new retrieval strategy, new agent, new UI workflow. Introduce the feature-flag mechanism alongside the first experimental component (likely Phase 3's rule engine or Phase 4's retrieval strategy) — not bolted on retroactively in Phase 6.

---

## 14. Research & Evaluation Infrastructure (Phase 6, seeded earlier)

The platform is explicitly designed to double as a research environment (§62–§70, §112–§113), which has technical implications from earlier phases:

- **Baselines the architecture must support comparing:** rules-only, LLM-only, LLM+keyword, LLM+vector, hybrid RAG, agentic hybrid RAG, multi-agent hybrid RAG (§65, §112). This is why GapEngineService (Phase 3) must be independently callable without the Phase 4 AI layer — it's Baseline A.
- **Ablation studies** (§66) require each capability (vector search, keyword search, temporal reasoning, contradiction detection, HCC rules, agent planning, evidence reranking, human feedback) to be independently toggleable — reinforcing the feature-flag requirement in §13.2.
- **Ground-truth dataset** — patient, clinical evidence, expected condition, expected HCC, gap status, evidence relevance, expert rationale, with support for multiple expert annotations (§68).
- **Model evaluation registry** — records dataset version, model, prompt version, embedding model, retrieval configuration, rule version, HCC version, agent configuration, and evaluation metrics per experiment (§70) — this is a direct consumer of the versioning dimensions in §13.1.
- **Synthetic data** — because real PHI must not be used for experimentation, synthetic patients/encounters/diagnoses/notes/claims must be supportable end-to-end through the same ingestion pipeline as real data (§67).

---

## 15. Deployment Architecture

### 15.1 Environment progression (§76)

| Environment | Platform |
|---|---|
| Development | Docker Compose |
| Test | Docker / Kubernetes |
| Production | AWS |

### 15.2 Production topology (Phase 6)

```text
Angular → WAF → Load Balancer/Gateway → Ocelot → Microservices
```

with:

```text
RDS SQL Server · RabbitMQ/Amazon MQ (RabbitMQ-compatible contracts retained)
OpenSearch · Qdrant · Object Storage (S3) · Secrets Manager · KMS · CloudWatch
```

### 15.3 Image workflow (§109)

```text
Source Code → Docker Build → Image → Local Integration Test → Docker Hub → Deployment Environment
```

Production deployment must use immutable version-tagged images (e.g., `aris/patient-service:1.0.0` or `aris/patient-service:git-<commit>`), never a mutable `latest` tag.

### 15.4 Scalability (§75)

Each service scales independently according to its own load profile:

```text
PatientService    → horizontal scaling
GapEngineService  → horizontal scaling
Indexer           → consumer scaling
Embedding Worker  → worker scaling
Agent             → model-dependent scaling
```

---

## 16. Cross-Phase Traceability

| Architectural concern | First appears | Fully matures |
|---|---|---|
| Identity, RBAC, gateway routing | Phase 1 | Phase 6 (OIDC, ABAC) |
| Canonical data model, ingestion, provenance | Phase 2 | Phase 2 |
| Event-driven architecture, Outbox, search indexing | Phase 2 | Phase 2 |
| HCC mapping, Gap Engine, RAF calculation | Phase 3 | Phase 3 |
| Hybrid retrieval, embeddings, agent orchestration, guardrails | Phase 4 | Phase 4 |
| Complete persona workflows, feedback, audit | Phase 5 | Phase 5 |
| AWS deployment, advanced security, observability, research infra | Phase 6 | Phase 6 (ongoing) |

Cross-cutting concerns that should be seeded earlier than their "owning" phase (per the Project Plan's cross-phase workstreams): audit-event logging (start Phase 1), versioning discipline (start with each dimension as it's introduced), PHI-safe logging (Phase 1), feature flags (Phase 3).

---

## 17. What This Document Deliberately Omits

This is architecture, not requirements or scheduling:

- Functional behavior and acceptance criteria — see the functional specification and the phase-specific Functional Requirements documents.
- Effort estimates, task sequencing, and risk registers — see the Project Plan and phase-specific Detailed Plan documents.
- Exact field-by-field schemas and endpoint contracts for services not yet started — those are authored in that phase's own Technical Documentation when the phase begins, following the pattern set by `ARIS — Phase 1 Technical Documentation.md`.
