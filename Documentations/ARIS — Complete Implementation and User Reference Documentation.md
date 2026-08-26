# ARIS — Agentic Risk Intelligence System

## Comprehensive Functional Specification, User Requirements, Functional Architecture, and Research Framework

**Document Version:** 2.0  
**Status:** Functional Baseline + Implementation Reference  
**System:** Agentic Risk Intelligence System (ARIS)  
**Domain:** Healthcare / Medicare Advantage / Risk Adjustment / Clinical Documentation Intelligence  
**Architecture:** Microservices + Event-Driven + Retrieval-Augmented Agentic AI  
**Primary UI:** Angular  
**Primary Backend:** ASP.NET Core / .NET  
**AI Layer:** Agentic AI / LLM / RAG  
**Deployment Model:** Docker-first, Docker Hub, AWS-ready
**Development Model:** Vertical-slice development — Angular UI and backend developed together from Phase 1
**Identity:** IdentityService introduced as a foundational Phase-1 service

---

# 1. Executive Summary

ARIS — Agentic Risk Intelligence System — is an intelligent healthcare platform designed to help healthcare organizations identify, validate, explain, prioritize, and manage risk-adjustment opportunities using a combination of:

- Structured clinical data
- Claims and encounter information
- Diagnosis history
- Clinical documentation
- ICD-10-CM and HCC mappings
- Temporal reasoning
- Deterministic clinical/risk-adjustment rules
- Keyword and semantic search
- Retrieval-Augmented Generation (RAG)
- Large Language Models
- Agentic workflows
- Human feedback
- Explainability and evidence tracing

The fundamental problem addressed by ARIS is that healthcare risk adjustment information is distributed across multiple sources and is difficult to interpret consistently.

A patient may have:

- A historical diagnosis that is relevant to risk adjustment.
- Supporting evidence buried inside clinical notes.
- A condition documented in one encounter but absent in another.
- A diagnosis that maps differently across HCC model versions.
- A potential documentation gap.
- Conflicting evidence.
- A clinically relevant condition that requires provider confirmation.
- Existing evidence that is difficult for a coder or clinician to discover.

Traditional systems generally solve only portions of this problem.

ARIS attempts to create an integrated intelligence layer that answers:

> **What conditions and risk-adjustment opportunities exist for this patient, what evidence supports them, what evidence contradicts them, what is missing, how confident are we, why did the system reach this conclusion, and what should the human reviewer do next?**

The system is intentionally designed as **human-in-the-loop intelligence**, rather than autonomous clinical decision-making.

---

# 2. Problem Statement

## 2.1 Healthcare Risk Adjustment Problem

Risk adjustment depends heavily on accurate identification and documentation of patient conditions.

However, relevant information may exist across:

- Electronic Health Records
- Claims
- Encounter records
- Diagnosis tables
- Problem lists
- Clinical notes
- Laboratory results
- Procedures
- Medication records
- Hospitalization records
- Historical records
- External clinical systems

These sources may not agree.

A condition may:

1. Appear in historical records but not current documentation.
2. Appear as a diagnosis code without sufficient supporting evidence.
3. Appear in clinical text without a corresponding diagnosis code.
4. Be documented under multiple ICD codes.
5. Be documented at different levels of specificity.
6. Be incorrectly carried forward.
7. Be clinically resolved but remain historically present.
8. Be present in evidence but difficult to discover.
9. Map differently under different HCC model versions.

The problem therefore is not simply:

> "Find ICD codes."

It is:

> **Establish an evidence-based understanding of the patient's risk-adjustment-relevant clinical state over time.**

---

# 3. ARIS's Primary Objective

ARIS's objective is to create an intelligent evidence-analysis platform that helps authorized users:

1. Identify potential risk-adjustment gaps.
2. Understand why a gap was identified.
3. Find supporting clinical evidence.
4. Find contradictory or weakening evidence.
5. Understand temporal history.
6. Determine whether the condition appears active, historical, uncertain, or unsupported.
7. Understand the relationship between clinical evidence and HCC models.
8. Prioritize gaps according to configurable criteria.
9. Prepare for patient encounters.
10. Support compliant documentation.
11. Review coding opportunities.
12. Capture human feedback.
13. Continuously evaluate and improve system performance.

---

# 4. What ARIS Is Not

ARIS must explicitly avoid several classes of behavior.

## 4.1 Not an Autonomous Diagnostic System

ARIS must not independently diagnose a patient.

For example:

> "Patient definitely has CKD."

is not an acceptable system conclusion unless the underlying authorized clinical evidence supports an already documented diagnosis and the statement is clearly attributed.

Instead:

> "The record contains evidence associated with CKD. Provider confirmation is required."

is appropriate.

---

## 4.2 Not an Autonomous Coding Authority

ARIS may identify a potential coding opportunity but must not represent an unsupported code as clinically established.

The system should distinguish:

- Documented diagnosis
- Supported diagnosis
- Suspected condition
- Historical condition
- Conflicting evidence
- Insufficient evidence

---

## 4.3 Not a Replacement for Clinicians

The system supports:

- Clinicians
- Coders
- Risk-adjustment teams
- Quality teams
- Auditors
- Analysts

Human users remain responsible for final clinical and coding decisions.

---

# 5. Target Users

ARIS is designed for multiple user personas.

## 5.1 Clinician

Examples:

- Physician
- Nurse practitioner
- Physician assistant
- Other authorized provider

### Primary objective

Understand the patient's relevant clinical history and identify conditions requiring clinical review or documentation.

### Typical questions

- What risk-adjustment conditions are relevant to this patient?
- Why is this condition being surfaced?
- What evidence exists?
- When was it last documented?
- Is there contradictory evidence?
- What should I verify during today's encounter?

---

# 6. Clinician Functional Requirements

## 6.1 Patient Overview

The clinician should be able to search for and open an authorized patient.

The patient overview should display:

- Patient identifier
- Demographics
- Relevant encounter history
- Major chronic conditions
- Recent diagnoses
- Current risk-adjustment opportunities
- Open gaps
- Recently closed gaps
- Evidence availability
- Risk-adjustment summary

---

## 6.2 Gap Summary

Each gap should contain:

- Gap identifier
- Patient identifier
- HCC
- Relevant ICD codes
- Description
- Last documented date
- Current-year documentation status
- Evidence count
- Confidence
- Priority
- Gap status

Possible statuses:

- Open
- Under Review
- Supported
- Resolved
- Rejected
- False Positive
- Insufficient Evidence
- Deferred

---

## 6.3 Explain This Gap

The clinician should be able to select:

> Explain this gap

ARIS should produce:

### Why it was identified

Example:

> The patient's record contains a diabetes-related diagnosis documented in the previous benefit year. No equivalent current-year diagnosis was identified.

### Supporting evidence

- Encounter
- Date
- Provider
- Diagnosis
- Clinical note
- Relevant procedure
- Relevant laboratory result where available

### Contradictory evidence

The system should actively search for evidence that weakens the hypothesis.

For example:

- Condition marked resolved
- Explicit denial
- Contradictory diagnosis
- Evidence suggesting an alternative explanation

### Recommended human action

Example:

> Verify whether the condition remains clinically relevant and document the assessment and plan when appropriate.

The system must not instruct the clinician to manufacture documentation.

---

# 7. Coder / Risk Adjustment Reviewer

The coder is a central ARIS persona.

## 7.1 Primary Objectives

The coder should be able to:

- Review potential HCC gaps.
- Examine supporting evidence.
- Verify documentation.
- Compare historical and current-year records.
- Understand ICD → HCC mappings.
- Review model-version differences.
- Accept or reject system suggestions.
- Record rationale.
- Identify false positives.

---

## 7.2 Coding Review Workflow

Typical workflow:

```text
Patient
   ↓
Open Gaps
   ↓
Select Gap
   ↓
Review Evidence
   ↓
Review Temporal History
   ↓
Review HCC Mapping
   ↓
Review Supporting/Contradictory Evidence
   ↓
Human Decision
   ↓
Accept / Reject / Defer / Insufficient Evidence
   ↓
Record Feedback
```

---

# 8. Risk Adjustment Analyst

Risk-adjustment analysts operate at a population level.

## 8.1 Objectives

The analyst should be able to:

- Analyze gap volumes.
- Analyze gap categories.
- Compare populations.
- Identify high-priority cohorts.
- Analyze closure rates.
- Analyze false-positive rates.
- Measure evidence sufficiency.
- Compare model versions.
- Evaluate system performance.

---

# 9. Population-Level Analytics

ARIS should eventually provide:

### Member metrics

- Total members
- Members with open gaps
- Members with multiple gaps
- Members without current-year documentation

### Gap metrics

- Total gaps
- Open gaps
- Closed gaps
- Rejected gaps
- False-positive gaps
- Average gap age

### HCC metrics

- HCC frequency
- HCC gap frequency
- HCC closure rate
- HCC evidence sufficiency

### Operational metrics

- Review volume
- Average review time
- Acceptance rate
- Rejection rate
- Deferral rate

---

# 10. Clinical Documentation Specialist

A documentation specialist uses ARIS to identify opportunities where the clinical record may require clarification.

The system should answer:

> "What documented information may require clarification or additional clinical assessment?"

It should not answer:

> "What diagnosis should the provider add?"

---

# 11. Documentation Assistance

ARIS may generate:

- Documentation prompts
- Evidence summaries
- Encounter preparation summaries
- Questions for clinical consideration
- Structured documentation suggestions

Example:

> Historical documentation indicates diabetes with renal involvement. Current-year documentation does not clearly establish the status of the condition. Consider verifying the current clinical status during the encounter.

This preserves clinical judgment.

---

# 12. Auditor

Auditors require transparency.

The auditor should be able to reconstruct:

1. Input data
2. Data timestamp
3. Rule version
4. HCC model version
5. Evidence retrieved
6. Search results
7. Agent actions
8. LLM model/version
9. Prompt/template version
10. Generated conclusion
11. Human decision
12. Final disposition

ARIS should therefore maintain an **explainability and audit trail**.

---

# 13. Administrator

Administrators manage:

- Users
- Roles
- Permissions
- Organizations
- Configuration
- Model versions
- Feature flags
- Rule versions
- Agent configuration
- System health

---

# 14. Data Engineer

Data engineers are responsible for:

- Data ingestion
- Source mapping
- Data quality
- Pipeline monitoring
- Event processing
- Search indexing
- Embedding pipelines
- Data reconciliation

---

# 15. Researcher

The researcher is a particularly important ARIS persona because the system is designed as a research platform.

Researchers should be able to study:

- Gap detection accuracy
- LLM reasoning
- Retrieval quality
- Agent planning
- Human-AI collaboration
- False positives
- Hallucination rates
- Temporal reasoning
- Evidence ranking
- Model drift

The platform should therefore retain sufficient metadata to reproduce experiments.

---

# 16. Core Functional Workflow

The central ARIS workflow is:

```text
DATA
 ↓
INGESTION
 ↓
NORMALIZATION
 ↓
CANONICAL STORAGE
 ↓
EVENT PUBLICATION
 ↓
SEARCH INDEXING
 ↓
HCC MAPPING
 ↓
DETERMINISTIC GAP DETECTION
 ↓
EVIDENCE RETRIEVAL
 ↓
SEMANTIC RETRIEVAL
 ↓
AGENTIC REASONING
 ↓
EXPLANATION
 ↓
HUMAN REVIEW
 ↓
DECISION
 ↓
FEEDBACK
 ↓
EVALUATION / IMPROVEMENT
```

---

# 17. Data Sources

ARIS should support multiple source types.

## Structured

- Patient
- Provider
- Encounter
- Diagnosis
- Procedure
- Medication
- Laboratory
- Claims
- Admission/discharge
- Problem list

## Semi-structured

- FHIR
- HL7-derived data
- JSON
- CSV
- XML

## Unstructured

- Clinical notes
- Progress notes
- Discharge summaries
- Specialist notes
- Assessment/plan
- Historical documentation

---

# 18. Data Ingestion

DataIngestService should support:

### API ingestion

```text
POST /api/ingest/patient
POST /api/ingest/encounter
POST /api/ingest/diagnosis
```

### File ingestion

Support:

- CSV
- JSON
- XML
- FHIR bundles

### Future ingestion

- SFTP
- Object storage
- FHIR endpoints
- Claims feeds
- Event streams

---

# 19. Data Validation

Every ingestion request should undergo:

1. Schema validation
2. Required-field validation
3. Referential integrity validation
4. Code validation
5. Date validation
6. Duplicate detection
7. Source identification
8. Provenance recording

Invalid data should not silently enter the canonical store.

---

# 20. Data Provenance

Every clinically relevant record should ideally retain:

- Source system
- Source record identifier
- Ingestion timestamp
- Original timestamp
- Data version
- Transformation version
- Ingestion job identifier

This is critical for auditability.

---

# 21. Canonical Patient Model

ARIS should establish a canonical representation of:

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
 └── Evidence
```

---

# 22. HCC Mapping Service

HccMappingService provides version-aware mapping.

The service must support:

```text
ICD
 ↓
HCC Model Version
 ↓
HCC
 ↓
HCC Description
```

The system must never assume that an HCC mapping is universally valid across model versions.

---

# 23. HCC Model Versioning

Every risk-adjustment analysis should identify:

- HCC model
- Model version
- Effective date
- Mapping version
- Rule version

This allows historical reproducibility.

---


## RAF Calculation as a First-Class ARIS Capability

RAF (Risk Adjustment Factor) calculation is a core ARIS capability and must not be treated merely as an implicit result of HCC identification.

ARIS should provide a versioned, reproducible patient-level RAF calculation capability that combines applicable demographic factors, validated disease/HCC factors, interaction rules, and the coefficients/rules for the selected risk-adjustment model and payment year.

The conceptual flow is:

```text
Patient Clinical Data
        ↓
Diagnosis / Evidence Validation
        ↓
ICD-10-CM → HCC Mapping
        ↓
HCC Eligibility
        ↓
HCC Hierarchy / Interaction Rules
        ↓
Demographic Factors
        ↓
Disease / HCC Coefficients
        ↓
Interactions
        ↓
RAF Calculation
        ↓
Patient RAF Score
        ↓
RAF Change / Delta
        ↓
Explanation & Evidence
```

The selected model, payment year, segment, coefficients, normalization/factors, and other model-specific rules must be explicitly versioned. A RAF score must never be presented without identifying the model/version under which it was calculated.

### RafCalculationService

ARIS should introduce a dedicated `RafCalculationService`.

This service should be separate from `GapEngineService`.

**GapEngineService** answers:

> Which potential risk-adjustment opportunities or gaps should be reviewed?

**RafCalculationService** answers:

> Given the applicable patient demographic and validated disease/HCC factors under a specific risk-adjustment model/version, what RAF score is calculated?

Conceptually:

```text
PatientService
     │
     ↓
GapEngineService ──────→ HccMappingService
     │
     │ validated/current HCCs
     ↓
RafCalculationService
     │
     ├── Demographic factors
     ├── HCC factors
     ├── Hierarchies
     ├── Interactions
     ├── Model coefficients
     └── Model/version rules
     │
     ↓
Patient RAF Score
     │
     ├── RAF explanation
     ├── Component contributions
     ├── Previous RAF
     └── RAF delta
     ↓
Angular UI
```

### RAF Functional Requirements

RafCalculationService should support, as applicable to the selected model:

- Patient demographic factors
- Eligible HCCs
- HCC coefficients
- HCC hierarchies
- Disease interactions
- Demographic interactions
- Model-specific rules
- Payment-year selection
- Segment-specific calculation
- Normalization/factors where applicable
- Patient-level RAF calculation
- Component-level contribution
- Previous-period RAF
- RAF delta
- Calculation provenance
- Calculation reproducibility
- Model/version traceability

The exact factors and equations must come from the authoritative risk-adjustment model/version being implemented and must not be hard-coded as universal healthcare rules.

### RAF Calculation Output

A patient RAF result should conceptually contain:

```text
Patient Risk Profile

Model: [Applicable Risk Model]
Payment Year: [YYYY]
Model Version: [Version]

Demographic Factors
-------------------
Age / applicable factor       ...
Other applicable factors      ...

Disease Factors
---------------
HCC A                          ...
HCC B                          ...
HCC C                          ...

Interactions
------------
Applicable interaction        ...

--------------------------------
Patient RAF Score              X.XXXX

Previous RAF                   X.XXXX
RAF Change                     +X.XXXX
```

The exact fields and values depend on the applicable model.

### RAF Component Explainability

ARIS should explain which components contributed to a patient's RAF.

For example:

```text
RAF Score: 1.842

Contributors
────────────────────────────
Demographic factor       0.312
HCC 18                   0.287
HCC 85                   0.451
HCC 96                   0.372
Interaction              0.420
────────────────────────────
Total                    1.842
```

The example values are illustrative only. Production calculations must use authoritative model coefficients and rules.

Each contribution should be traceable to:

- Applicable model/version
- Source factor
- Underlying HCC
- Diagnosis/evidence supporting that HCC where applicable
- Coefficient/rule used
- Calculation timestamp
- Calculation version

### RAF Opportunity Analysis

ARIS should eventually answer:

> What is the potential RAF impact of this gap?

For example:

```text
Current RAF                 1.842

Potential HCC               HCC XX

Potential RAF contribution  +0.231

Potential RAF               2.073

Potential change            +0.231
```

Such a result must be explicitly described as a **potential model-based impact** and not as a guaranteed payment, reimbursement, or financial outcome.

ARIS should distinguish:

- Current calculated RAF
- Potential RAF if an eligible factor is subsequently validated
- Potential RAF delta
- Evidence supporting the potential factor
- Human validation status

The system must not represent a potential gap as an established condition merely because it could affect RAF.

### RAF Calculation Lifecycle

```text
Patient Snapshot
      ↓
Selected Model / Payment Year
      ↓
Validated Demographics
      ↓
Validated HCC Factors
      ↓
Hierarchy Processing
      ↓
Interaction Processing
      ↓
Coefficient Application
      ↓
RAF Calculation
      ↓
RAF Result + Component Breakdown
      ↓
Audit / Provenance
```

Every calculated RAF should retain enough metadata to reproduce the result.

### RAF Versioning

At minimum, the calculation context should identify:

```text
Risk Model
Model Version
Payment Year
Segment
Coefficient Version
Rule Version
Patient Data Snapshot
Calculation Version
Calculation Timestamp
```

This is critical because risk-adjustment methodologies and coefficients may change over time.


# 24. Gap Engine

GapEngineService is the deterministic reasoning layer.

Its responsibility is to answer:

> "Based on structured data and configured rules, which risk-adjustment opportunities should be considered for this patient?"

---

# 25. Gap Categories

ARIS should eventually distinguish:

### Historical gap

Condition existed historically but has no current-year evidence.

### Documentation gap

Clinical evidence exists but current documentation is insufficient.

### Coding gap

Potential clinical documentation exists but corresponding coding information is absent.

### Evidence gap

A potential condition exists but supporting evidence is insufficient.

### Contradiction gap

Different records provide conflicting information.

### Recapture gap

A previously documented chronic condition has not been represented in the current period.

### Suspected condition

Evidence suggests a condition may exist but provider confirmation is required.

---

# 26. Temporal Reasoning

Temporal reasoning is fundamental.

ARIS must understand:

```text
2023 → condition documented
2024 → condition documented
2025 → condition absent
2026 → current review
```

The system should not simply interpret absence as disappearance.

Instead it should determine:

- Last known documentation
- Documentation frequency
- Current evidence
- Explicit resolution
- Contradictory evidence
- Time elapsed
- Applicable risk model rules

---

# 27. Evidence Model

Every gap should have an evidence graph.

Example:

```text
Gap
 |
 +-- ICD Evidence
 |
 +-- Encounter Evidence
 |
 +-- Clinical Note Evidence
 |
 +-- Procedure Evidence
 |
 +-- Laboratory Evidence
 |
 +-- Medication Evidence
 |
 +-- Historical Evidence
 |
 +-- Contradictory Evidence
```

---

# 28. Evidence Strength

ARIS should classify evidence.

Example:

| Evidence | Strength |
|---|---|
| Explicit provider diagnosis | Very High |
| Assessment/Plan | Very High |
| Problem list | High |
| Specialist documentation | High |
| Repeated historical diagnosis | Medium-High |
| Procedure associated with condition | Medium |
| Medication association | Medium |
| Laboratory indication | Low-Medium |
| NLP inference | Low unless corroborated |

The exact scoring methodology becomes a research topic.

---

# 29. Search

ARIS requires two complementary search mechanisms.

## Keyword / structured search

OpenSearch should support:

- Patient filtering
- ICD filtering
- HCC filtering
- Provider filtering
- Encounter date filtering
- Note search
- Exact terminology

## Semantic search

Vector retrieval should support queries such as:

> "Find evidence suggesting chronic kidney disease."

This should retrieve semantically relevant text even when the exact phrase does not occur.

---

# 30. Hybrid Retrieval

The preferred retrieval model is:

```text
User Question
      |
      +--------------------+
      |                    |
      ↓                    ↓
Keyword Search       Vector Search
      |                    |
      +---------+----------+
                ↓
         Evidence Fusion
                ↓
        Ranking / Filtering
                ↓
          Evidence Set
```

This allows ARIS to combine exact and semantic retrieval.

---

# 31. Agentic AI

The Agent Orchestrator is responsible for higher-order reasoning.

The agent should not directly access databases.

Instead, it should use controlled tools.

Example:

```text
Agent
 |
 +-- get_patient()
 |
 +-- get_diagnoses()
 |
 +-- get_gaps()
 |
 +-- map_icd_to_hcc()
 |
 +-- search_evidence()
 |
 +-- retrieve_semantic_evidence()
 |
 +-- evaluate_evidence()
 |
 +-- generate_explanation()
```

---

# 32. Agent Planning

A complex question may result in:

```text
Question
 ↓
Plan
 ↓
Retrieve patient context
 ↓
Retrieve gaps
 ↓
Retrieve HCC mapping
 ↓
Search evidence
 ↓
Search semantic evidence
 ↓
Evaluate evidence
 ↓
Check contradictions
 ↓
Generate response
 ↓
Validate response
```

The agent should not execute arbitrary actions.

All tools must be explicitly registered and permission-controlled.

---

# 33. Example Agent Scenario

User asks:

> "Why is diabetes with complications showing as a gap for this patient?"

Agent workflow:

1. Retrieve patient.
2. Retrieve diagnoses.
3. Retrieve gap.
4. Identify ICD.
5. Retrieve HCC mapping.
6. Search historical evidence.
7. Search current-year evidence.
8. Search contradictory evidence.
9. Compare dates.
10. Evaluate evidence.
11. Generate explanation.
12. Attach citations.
13. Return confidence and limitations.

---

# 34. Agent Output Requirements

Agent responses should be structured.

Example:

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

The system should avoid returning unsupported free-form conclusions without evidence.

---

# 35. Explainability

Explainability is a core ARIS requirement.

For every AI-generated result, the user should be able to determine:

- What was considered?
- What evidence was used?
- What rules were applied?
- What model was used?
- What information was unavailable?
- Why was the conclusion reached?
- What uncertainty remains?

---

# 36. Confidence

Confidence should not simply represent LLM confidence.

ARIS should eventually calculate confidence from multiple signals:

```text
Evidence quality
+
Evidence quantity
+
Temporal consistency
+
Coding consistency
+
Source reliability
+
Rule confidence
+
Retrieval relevance
+
Contradiction penalty
+
LLM reasoning confidence
```

This provides a potential research contribution.

---

# 37. Human-in-the-Loop

ARIS must explicitly support human intervention.

Possible decisions:

```text
Accept
Reject
False Positive
Needs Review
Insufficient Evidence
Deferred
Resolved
```

Each decision should optionally capture:

- Reviewer
- Timestamp
- Reason
- Comments
- Evidence selected

---

# 38. Feedback Loop

Human feedback should become structured research data.

Example:

```text
ARIS Prediction
      ↓
Human Decision
      ↓
Correct / Incorrect
      ↓
Reason
      ↓
Feedback Dataset
      ↓
Evaluation
      ↓
Rule / Retrieval / Agent Improvement
```

This enables continuous evaluation without blindly retraining the model.

---

# 39. Risk Gap Prioritization

Not all gaps are equally important.

ARIS should eventually rank gaps using:

- Clinical relevance
- Evidence strength
- Recency
- HCC importance
- Historical persistence
- Probability of valid documentation
- Contradiction level
- Review effort
- Patient encounter proximity

Example:

```text
Priority Score =
Evidence Strength
× Temporal Relevance
× HCC Importance
× Confidence
× Actionability
```

The precise mathematical model can become an experimental research component.

---

# 40. Patient Risk Profile

ARIS should provide a longitudinal patient risk profile.

Example:

```text
Patient
 |
 +-- Current Conditions
 |
 +-- Historical Conditions
 |
 +-- HCCs
 |
 +-- Open Gaps
 |
 +-- Closed Gaps
 |
 +-- Evidence
 |
 +-- Contradictions
 |
 +-- Recent Encounters
 |
 +-- Risk Trend
```

---

# 41. Pre-Visit Workflow

A clinician could start the day with:

> "Prepare my upcoming patient charts."

ARIS can generate:

- Patient summary
- Relevant historical conditions
- Open risk-adjustment opportunities
- Evidence requiring review
- Recent changes
- Suggested areas for clinical assessment

This should be a **preparation aid**, not an autonomous clinical recommendation system.

---

# 42. Post-Encounter Workflow

After an encounter:

1. New data enters ARIS.
2. Data is indexed.
3. Gap engine recalculates.
4. Existing gaps are re-evaluated.
5. Closed gaps are identified.
6. New gaps may emerge.
7. Evidence is updated.
8. Audit trail records changes.

---

# 43. Gap Lifecycle

Every gap should have a lifecycle.

```text
Detected
   ↓
Prioritized
   ↓
Reviewed
   ↓
Evidence Evaluated
   ↓
Human Decision
   ↓
 ┌───────────────┬───────────────┐
 ↓               ↓               ↓
Resolved       Rejected       Deferred
```

A gap should never simply disappear without a reason.

---

# 44. Contradiction Detection

A major research capability should be identification of conflicting evidence.

Example:

```text
2024:
Provider: Diabetes

2025:
Provider: Diabetes

2025:
Another note: "No history of diabetes"
```

ARIS should surface the contradiction instead of selecting one statement silently.

---

# 45. Data Quality Intelligence

ARIS should detect:

- Duplicate patients
- Duplicate encounters
- Impossible dates
- Invalid ICD codes
- Missing providers
- Missing encounter relationships
- Conflicting demographic information
- Duplicate diagnoses
- Source inconsistencies

---

# 46. Clinical NLP

Later versions may extract structured concepts from notes:

```text
Clinical Note
     ↓
NLP
     ↓
Entities
     ↓
Conditions
     ↓
Temporal Expressions
     ↓
Negation
     ↓
Evidence
```

The NLP system must distinguish:

- Positive diagnosis
- Negated diagnosis
- Historical diagnosis
- Family history
- Suspected diagnosis
- Rule-out diagnosis

This is particularly important for avoiding false positives.

---

# 47. Negation Handling

Consider:

> "Patient denies history of CHF."

ARIS must not treat "CHF" as positive evidence.

Similarly:

> "No evidence of CKD."

must reduce evidence strength rather than increase it.

---

# 48. Temporal NLP

The system should distinguish:

> "Patient had CHF in 2019."

from:

> "Patient has CHF."

and:

> "Patient was diagnosed with CHF last month."

This temporal distinction is essential for longitudinal reasoning.

---

# 49. Provenance

Every AI conclusion should be traceable to:

- Source record
- Source system
- Source timestamp
- Extraction process
- Retrieval method
- Rule version
- Model version

---

# 50. Security

ARIS handles highly sensitive healthcare information.

Security requirements include:

- Authentication
- Authorization
- Role-based access
- Attribute-based access
- Least privilege
- Encryption in transit
- Encryption at rest
- Secret management
- Audit logging
- Data isolation
- PHI-aware logging

---

# 51. PHI Protection

ARIS should avoid putting unnecessary PHI into:

- Application logs
- Exception messages
- Metrics
- Trace attributes
- Debug output

AI prompts should contain only the minimum necessary information.

---

# 52. AI Guardrails

The AI layer should implement:

### Input guardrails

- Prompt injection detection
- Malicious instruction detection
- PHI handling controls
- Input validation

### Retrieval guardrails

- Patient authorization filtering
- Source validation
- Evidence provenance

### Output guardrails

- Unsupported diagnosis detection
- Unsupported coding detection
- Hallucination detection
- Citation validation
- PHI leakage detection

---

# 53. AI Response Policy

The agent should follow a hierarchy:

```text
Patient Data
     ↓
Verified Evidence
     ↓
Deterministic Rules
     ↓
Clinical/Risk Model Metadata
     ↓
LLM Reasoning
```

LLM reasoning should never override authoritative structured information without explicitly stating the conflict.

---

# 54. Auditability

ARIS should record:

```text
User
Action
Timestamp
Patient
Request
Tools Called
Data Sources
Evidence Retrieved
Rule Version
Model Version
AI Output
Human Decision
```

This allows retrospective investigation.

---

# 55. API Gateway

Ocelot provides the application entry point.

Typical routes:

```text
/patients/*
/gaps/*
/hcc/*
/ingest/*
/agent/*
/search/*
```

The frontend should not need knowledge of internal container addresses.

---

# 56. Event-Driven Architecture

RabbitMQ decouples services.

Example:

```text
DataIngestService
      ↓
PatientIngested
      ↓
RabbitMQ
      ↓
Indexer
      ↓
OpenSearch
```

Another pipeline:

```text
DiagnosisIngested
      ↓
RabbitMQ
      ├── Indexer
      ├── Embedding Worker
      ├── Analytics
      └── Audit Processor
```

---

# 57. Reliability

Event publication should use the Outbox Pattern.

The transaction becomes:

```text
Business Data
      +
Outbox Event
      ↓
Atomic SQL Transaction
      ↓
Outbox Processor
      ↓
RabbitMQ
```

This prevents data/event divergence.

---

# 58. Search Indexing

The indexing pipeline should preserve:

- Patient ID
- Encounter ID
- Diagnosis ID
- Source
- Date
- ICD
- HCC
- Text
- Provenance
- Index version

---

# 59. Embedding Pipeline

For appropriate clinical text:

```text
Document
 ↓
Chunking
 ↓
PHI / authorization validation
 ↓
Embedding model
 ↓
Vector
 ↓
Qdrant
```

Metadata should accompany vectors so retrieval can be filtered by:

- Patient
- Date
- Encounter
- Document type
- Source

---

# 60. Angular User Experience

The UI should eventually provide:

## Dashboard

- Population metrics
- Gap counts
- High-priority patients
- Recent activity

## Patient Search

- Search by MRN
- Name
- Identifier
- Other authorized attributes

## Patient Summary

- Demographics
- Conditions
- Encounters
- Gaps
- Evidence

## Gap Review

- Gap explanation
- Evidence
- Contradictions
- HCC mapping
- History
- Human decision

## AI Assistant

Natural-language interaction with ARIS.

---

# 61. "Ask ARIS"

Users may ask questions such as:

> "What are the unresolved risk-adjustment opportunities for this patient?"

> "Why is this gap open?"

> "Show me the evidence from the last two years."

> "What changed since the previous encounter?"

> "Which evidence contradicts this gap?"

The system should return evidence-backed responses.

---

# 62. Research Evaluation Framework

ARIS should be designed so its effectiveness can be scientifically evaluated.

Important metrics include:

## Gap Detection

- Precision
- Recall
- F1
- Sensitivity
- Specificity

## Evidence Retrieval

- Precision@K
- Recall@K
- MRR
- NDCG
- Evidence coverage

## LLM

- Groundedness
- Citation accuracy
- Hallucination rate
- Completeness
- Clinical factuality

## Agent

- Task completion rate
- Tool-selection accuracy
- Planning efficiency
- Number of unnecessary tool calls
- Failure recovery rate

## Human-AI Collaboration

- Review time
- Decision accuracy
- User acceptance
- Override rate
- False-positive workload

---

# 63. Core Research Questions

ARIS can support several PhD-level research questions.

### RQ1 — Hybrid Intelligence

> Does combining deterministic risk-adjustment rules with retrieval-augmented LLM reasoning improve risk-gap identification compared with deterministic rules alone?

---

### RQ2 — Hybrid Retrieval

> Does combining keyword retrieval with semantic retrieval improve evidence discovery compared with either approach independently?

---

### RQ3 — Agentic Reasoning

> Can an agentic architecture improve the completeness and accuracy of multi-source clinical evidence analysis compared with a single-pass LLM?

---

### RQ4 — Temporal Reasoning

> Can longitudinal temporal reasoning reduce false-positive risk-adjustment gaps?

---

### RQ5 — Explainability

> Does evidence-grounded explanation improve human reviewer confidence and decision accuracy?

---

### RQ6 — Human-AI Collaboration

> Can ARIS reduce reviewer workload without reducing coding-review accuracy?

---

### RQ7 — Feedback

> Can structured human feedback improve gap-ranking and evidence-retrieval performance?

---

# 64. Experimental Architecture

ARIS should support controlled experiments.

Example:

```text
               ARIS Evaluation Framework
                         |
       +-----------------+-----------------+
       |                 |                 |
 Rule-only          RAG-only          Agentic
       |                 |                 |
       +-----------------+-----------------+
                         |
                  Compare Results
                         |
       +-----------------+-----------------+
       |                 |                 |
 Accuracy           Explainability      Efficiency
```

This makes the platform more than an application; it becomes a research environment.

---

# 65. Baseline Models

Research should establish multiple baselines.

### Baseline A

Deterministic rules only.

### Baseline B

LLM without retrieval.

### Baseline C

LLM + keyword retrieval.

### Baseline D

LLM + vector retrieval.

### Baseline E

Hybrid retrieval + LLM.

### Baseline F

Agentic hybrid architecture.

The objective is to measure whether increasing architectural complexity actually improves outcomes.

---

# 66. Ablation Studies

Potential experiments:

```text
Full System

- Remove vector search
- Remove keyword search
- Remove temporal reasoning
- Remove contradiction detection
- Remove HCC rules
- Remove agent planning
- Remove evidence reranking
- Remove human feedback
```

Compare:

- Accuracy
- Hallucination
- Evidence quality
- Latency
- Cost

This is highly valuable for a research publication.

---

# 67. Synthetic Data

Because real healthcare data is highly sensitive, ARIS should support synthetic datasets.

Synthetic data can represent:

- Patients
- Encounters
- Diagnoses
- Notes
- HCCs
- Claims
- Longitudinal histories

Research experiments can therefore be reproduced without exposing PHI.

---

# 68. Ground Truth

A research-grade ARIS implementation should maintain a curated ground-truth dataset.

Each example should contain:

```text
Patient
Clinical evidence
Expected condition
Expected HCC
Gap status
Evidence relevance
Expert rationale
```

Multiple expert annotations should ideally be supported.

---

# 69. Inter-Rater Agreement

Where human experts label data, ARIS can measure:

- Cohen's Kappa
- Fleiss' Kappa
- Agreement rate

This helps distinguish:

> Model error

from:

> Genuine clinical/coding ambiguity.

---

# 70. Model Evaluation Registry

Every experiment should record:

- Dataset version
- Model
- Prompt version
- Embedding model
- Retrieval configuration
- Rule version
- HCC version
- Agent configuration
- Evaluation metrics

This enables reproducibility.

---

# 71. Versioning

The following must be independently versioned:

```text
API Version
Schema Version
HCC Model
Mapping Version
Rule Version
Prompt Version
LLM Version
Embedding Model
Retrieval Configuration
Agent Configuration
UI Version
```

---

# 72. Feature Flags

Feature flags should control experimental features such as:

```text
New Gap Rule
New HCC Model
New LLM
New Embedding Model
New Retrieval Strategy
New Agent
New UI Workflow
```

This allows controlled experimentation.

---

# 73. Observability

Every important transaction should be traceable.

Example:

```text
User Request
 ↓
Ocelot
 ↓
GapEngine
 ↓
PatientService
 ↓
HccMappingService
 ↓
Search
 ↓
Agent
 ↓
LLM
```

A distributed trace should allow the entire journey to be reconstructed.

---

# 74. Performance Requirements

Initial target requirements should be configurable and experimentally validated.

Example targets:

| Operation | Initial Target |
|---|---:|
| Patient lookup | < 300 ms |
| Gap retrieval | < 1 sec |
| Evidence search | < 1 sec |
| Agent explanation | < 10 sec |
| Bulk ingestion | > configurable records/sec |

These are **engineering targets**, not clinical guarantees.

---

# 75. Scalability

ARIS should scale independently.

For example:

```text
PatientService    → horizontal scaling
GapEngineService  → horizontal scaling
Indexer           → consumer scaling
Embedding Worker  → worker scaling
Agent             → model-dependent scaling
```

RabbitMQ allows ingestion consumers to scale independently.

---

# 76. Deployment Model

## Development

Docker Compose.

## Test

Docker/Kubernetes.

## Production

AWS-compatible architecture:

```text
Angular
   ↓
WAF
   ↓
Load Balancer / Gateway
   ↓
Ocelot
   ↓
Microservices
```

with:

```text
RDS SQL Server
RabbitMQ / Amazon MQ
OpenSearch
Qdrant
Object Storage
Secrets Manager
KMS
Observability
```

---

# 77. Disaster Recovery

The system should eventually support:

- Database backup
- Event replay
- Search-index rebuild
- Vector-index rebuild
- Configuration backup
- Audit preservation

Search and vector stores should be treated as **reconstructible derived stores**, while canonical clinical data remains authoritative.

---

# 78. Failure Handling

ARIS should gracefully handle:

### PatientService unavailable

Gap request should fail clearly rather than fabricate information.

### HCC service unavailable

Gap processing should indicate mapping unavailable.

### RabbitMQ unavailable

Outbox events remain pending.

### OpenSearch unavailable

Clinical canonical data remains available; evidence search reports degraded functionality.

### LLM unavailable

Deterministic gap functionality remains operational.

This is an important architectural principle:

> **AI failure must not cause loss of core clinical/risk-adjustment functionality.**

---

# 79. Graceful Degradation

ARIS should operate in layers:

```text
Level 1:
Canonical Data

Level 2:
Deterministic Risk Rules

Level 3:
Keyword Evidence

Level 4:
Semantic Evidence

Level 5:
LLM Explanation

Level 6:
Agentic Reasoning
```

Failure at Level 6 should not invalidate Levels 1–5.

---

# 80. Ethical Requirements

ARIS should explicitly address:

- Bias
- Automation bias
- Hallucination
- Over-reliance on AI
- Incomplete documentation
- False positives
- False negatives
- Explainability
- Human accountability

Users should understand that:

> AI-generated suggestions are recommendations for review, not authoritative clinical conclusions.

---

# 81. Safety Principles

ARIS should follow:

### Evidence first

No evidence → no strong conclusion.

### Attribution

Distinguish documented facts from inferred information.

### Uncertainty

Expose uncertainty.

### Contradiction awareness

Do not hide conflicting evidence.

### Human authority

Human reviewers retain final decision authority.

### Minimum necessary data

Use only information required for the task.

---

# 82. Core Functional Modules

The complete ARIS platform can be viewed as these modules:

```text
1. Identity & Access
2. Patient Management
3. Clinical Data
4. Data Ingestion
5. Data Quality
6. HCC Mapping
7. Risk Gap Engine
8. RAF Calculation
9. Evidence Search
9. Semantic Retrieval
10. Agent Orchestration
11. AI Guardrails
12. Human Review
13. Feedback
14. Analytics
15. Audit
16. Experimentation
17. Administration
18. Observability
```

---

# 83. End-to-End Example

Consider a patient:

```text
Patient: P1001
```

Historical record:

```text
2024
E11.22
Diabetes with renal involvement
```

Current year:

```text
2025
No corresponding diagnosis
```

ARIS processes:

```text
PatientService
   ↓
Diagnosis History
   ↓
GapEngine
   ↓
HccMappingService
   ↓
HCC mapping
   ↓
Open Gap
   ↓
OpenSearch
   ↓
Retrieve notes
   ↓
Qdrant
   ↓
Semantic evidence
   ↓
Agent
   ↓
Evaluate evidence
   ↓
Check contradictions
   ↓
Generate explanation
   ↓
Clinician/Coder
```

The final result could state:

> A potential risk-adjustment gap was identified because the condition was documented in the previous period and current-year documentation does not contain equivalent evidence. Historical supporting evidence is available from specified encounters. Current clinical status should be verified by an authorized clinician.

The system should provide the underlying evidence rather than asking the user to trust the AI.

---

# 84. What Makes ARIS Different

ARIS is not merely:

- An HCC lookup system
- A chatbot
- A claims analytics system
- A clinical NLP system
- A RAG application
- A coding application

It combines these capabilities into a single evidence-driven workflow.

The central innovation is:

> **Combining symbolic risk-adjustment reasoning, longitudinal clinical reasoning, hybrid information retrieval, and agentic AI within a human-supervised architecture.**

---

# 85. Research Contribution Opportunities

Potential research contributions include:

## Contribution 0 — Explainable RAF Intelligence

A version-aware RAF calculation and explanation framework that connects patient-level risk scores to validated HCC factors, underlying clinical evidence, and potential gap-driven RAF changes.

A corresponding research question is:

> Can evidence-grounded, longitudinal AI assistance improve identification and explanation of RAF-relevant opportunities without increasing unsupported recommendations?

## Contribution 1

A hybrid symbolic + neural architecture for risk-adjustment gap detection.

## Contribution 2

A temporal evidence model for longitudinal risk-adjustment reasoning.

## Contribution 3

Hybrid lexical-semantic retrieval for clinical evidence discovery.

## Contribution 4

An evidence-grounded agentic workflow for risk-adjustment review.

## Contribution 5

A confidence model combining deterministic and neural evidence.

## Contribution 6

A human-feedback-driven gap-ranking framework.

## Contribution 7

A benchmark for evaluating healthcare risk-adjustment agents.

---

# 86. Proposed Research Hypothesis

A central hypothesis could be:

> **A hybrid architecture combining deterministic risk-adjustment rules, longitudinal temporal reasoning, hybrid evidence retrieval, and agentic LLM reasoning will identify and explain clinically relevant risk-adjustment opportunities more accurately and efficiently than deterministic rules or standalone LLM approaches.**

---

# 87. Research Evaluation Dimensions

The system should ultimately be evaluated across four dimensions:

### Accuracy

Does ARIS identify the correct opportunities?

### Evidence

Can ARIS support its conclusions with appropriate evidence?

### Safety

Does ARIS avoid unsupported or hallucinated conclusions?

### Efficiency

Does ARIS reduce human review effort?

A fifth dimension is particularly important:

### Explainability

Can a human understand why ARIS reached its conclusion?

---

# 88. ARIS Functional Maturity Model

## Level 1 — Data Platform

Structured healthcare data is available.

## Level 2 — Deterministic Intelligence

HCC mapping and gap rules operate.

## Level 3 — Evidence Intelligence

Search and evidence retrieval operate.

## Level 4 — Generative Intelligence

LLM-generated explanations operate.

## Level 5 — Agentic Intelligence

Agents perform multi-step evidence analysis.

## Level 6 — Adaptive Intelligence

Human feedback and evaluation improve ranking and reasoning.

---

# 89. Six Development Phases

## Phase 1 — Platform Foundation

Deliver:

- Clean Architecture
- .NET microservices
- SQL Server
- Docker
- Ocelot
- Basic APIs
- Health checks
- Swagger

**Outcome:** Platform skeleton.

---

## Phase 2 — Data & Search Foundation

Deliver:

- Data ingestion
- RabbitMQ
- Outbox
- OpenSearch
- Indexing
- Canonical clinical data

**Outcome:** Searchable clinical data platform.

---

## Phase 3 — Deterministic Risk Intelligence

Deliver:

- HCC mappings
- HCC versions
- Temporal reasoning
- Gap rules
- Evidence-aware gap candidates

**Outcome:** Real risk-adjustment intelligence.

---

## Phase 4 — AI & Agentic Intelligence

Deliver:

- Embeddings
- Qdrant
- Hybrid retrieval
- Agent Orchestrator
- LLM
- Guardrails
- Explainability

**Outcome:** Evidence-grounded AI reasoning.

---

## Phase 5 — Human Workflow

Deliver:

- Angular UI
- Patient dashboard
- Gap review
- Evidence review
- AI assistant
- Reviewer feedback

**Outcome:** Usable clinician/coder platform.

---

## Phase 6 — Enterprise & Research Platform

Deliver:

- Authentication
- Authorization
- Observability
- AWS deployment
- CI/CD
- Experiment framework
- Evaluation datasets
- Agent monitoring
- Advanced multi-agent workflows

**Outcome:** Production-grade research platform.

---

# 90. Definition of Done for ARIS

ARIS should be considered functionally mature when an authorized user can:

1. Search for a patient.
2. Open the patient's longitudinal history.
3. View risk-adjustment opportunities.
4. Understand why an opportunity exists.
5. See supporting evidence.
6. See contradictory evidence.
7. See temporal history.
8. See applicable HCC mapping/version.
9. Ask ARIS a natural-language question.
10. Receive an evidence-grounded answer.
11. Inspect citations.
12. Accept/reject/defer a recommendation.
13. Provide feedback.
14. See the decision recorded.
15. Audit how the result was generated.
16. Measure system performance against expert-reviewed ground truth.

---

# 91. Ultimate Vision

The ultimate goal of ARIS is not to automate the healthcare professional.

It is to create an intelligent layer between **massive healthcare data** and **human decision-making**.

The intended relationship is:

```text
Healthcare Data
       ↓
     ARIS
       ↓
Evidence + Reasoning + Context
       ↓
Human Reviewer
       ↓
Clinical / Coding Decision
```

The most important design principle is therefore:

> **ARIS should make the right information easier to find, the reasoning easier to understand, and the human decision easier to make — without replacing the human decision.**

---

# 92. Final Functional Definition

ARIS can be formally defined as:

> **An evidence-grounded, longitudinal, human-in-the-loop healthcare intelligence platform that integrates structured clinical data, deterministic risk-adjustment models, temporal reasoning, hybrid information retrieval, and agentic large-language-model workflows to identify, explain, prioritize, and manage potential risk-adjustment opportunities while maintaining provenance, uncertainty, auditability, and human decision authority.**

This definition can serve as the functional foundation for the subsequent:

- System Requirements Specification
- Software Architecture Document
- API specification
- Database design
- Agent design
- UI/UX specification
- Security model
- Research methodology
- Experimental design
- PhD thesis proposal
- Evaluation framework
---

# 93. Implementation-Aligned Development Model

This section supersedes any earlier sequencing that treated the UI as a later-stage activity.

ARIS will be developed using **vertical slices**. Angular UI development and backend development begin together in Phase 1 and continue together throughout all subsequent phases.

The objective is to validate the complete user journey continuously:

```text
Functional Requirement
        ↓
UX / Workflow Design
        ↓
API Contract
        ↓
Backend Implementation
        ↓
Database / Messaging / Search
        ↓
Angular UI
        ↓
Authentication / Authorization
        ↓
Docker
        ↓
Integration Testing
        ↓
User Validation
        ↓
Feedback
```

This approach is preferred over completing the backend first and building the UI later because it exposes API, authorization, data-model, pagination, search, workflow, and usability problems early.

---

# 94. IdentityService as a Foundational Service

IdentityService is introduced in **Phase 1**, not Phase 5.

Authentication and authorization are cross-cutting concerns. Retrofitting them after the application is already implemented creates avoidable coupling and security risk.

The initial architecture is:

```text
Angular
   ↓
IdentityService
   ↓
JWT / Access Token
   ↓
Angular
   ↓
Ocelot Gateway
   ↓
Protected Microservices
```

IdentityService is responsible for identity-related capabilities such as:

- Authentication
- User lifecycle
- Password management where ARIS owns credentials
- Token issuance
- Roles
- Claims
- User profile
- Authentication audit

Initial roles:

```text
Administrator
Clinician
Coder
RiskAnalyst
Auditor
Researcher
```

The architecture should remain compatible with OIDC/OAuth2 so that a future deployment can integrate with an enterprise identity provider without redesigning every application service.

---

# 95. Authorization Model

Authentication answers:

> Who is the user?

Authorization answers:

> What is the user allowed to do?

Phase 1 establishes the RBAC foundation.

Later phases can introduce ABAC and more granular resource-level authorization.

Examples:

```text
Clinician
 └── Access authorized patients

Coder
 └── Access authorized coding population

Risk Analyst
 └── Access authorized population analytics

Auditor
 └── Access audit information

Researcher
 └── Access approved de-identified datasets
```

The Angular application must never be considered the ultimate security boundary. Backend services must enforce authorization independently.

---

# 96. First Vertical Slice

The first complete end-to-end slice is:

```text
Angular Login
      ↓
IdentityService
      ↓
JWT
      ↓
Angular Auth State
      ↓
Ocelot
      ↓
Protected API
      ↓
Authenticated Application Shell
```

This validates the foundational identity, routing, token, gateway, and authorization architecture before significant clinical functionality is built.

---

# 97. Second Vertical Slice

The second slice is:

```text
Angular Patient Search
      ↓
Ocelot
      ↓
PatientService
      ↓
SQL Server
      ↓
Patient Results
      ↓
Angular Patient List
```

It validates:

- API contracts
- DTOs
- Authentication
- Authorization
- Database access
- Pagination
- Error handling
- UI state
- Loading states
- Empty states

---

# 98. Third Vertical Slice

```text
Patient Details UI
      ↓
PatientService
      ↓
Encounters / Diagnoses
      ↓
SQL Server
      ↓
Patient Timeline
```

The goal is to establish a usable longitudinal patient view before risk intelligence is introduced.

---

# 99. Fourth Vertical Slice

```text
Risk Dashboard
      ↓
GapEngineService
      ↓
PatientService
      ↓
HccMappingService
      ↓
Gap Results
      ↓
Angular Risk / Gap UI
```

This creates the first complete risk-adjustment workflow.

---

# 100. Fifth Vertical Slice

```text
Explain Gap
      ↓
Agent Orchestrator
      ↓
OpenSearch
      ↓
Qdrant
      ↓
LLM
      ↓
Evidence-Grounded Explanation
      ↓
Angular Evidence / AI UI
```

This introduces ARIS's agentic intelligence without breaking the deterministic baseline.

---

# 101. Revised Six-Phase Roadmap

The six phases are revised as follows.

| Phase | Primary Focus | Angular | Backend | AI |
|---|---|---|---|---|
| **1** | Platform, Identity & UI Foundation | Application shell, login, patient workflows | IdentityService, PatientService, HccMappingService, GapEngineService, Ocelot | — |
| **2** | Clinical Data, Ingestion & Search | Search, filters, timeline, evidence | DataIngestService, RabbitMQ, Outbox, OpenSearch, Indexer | — |
| **3** | Deterministic Risk Intelligence | Risk dashboard, gap review | HCC mapping, temporal rules, gap engine | — |
| **4** | RAG & Agentic Intelligence | AI assistant, explain-gap experience | Qdrant, embeddings, agent orchestrator, guardrails | LLM/RAG/agents |
| **5** | Clinical/Coding Workflow | Complete persona workflows | Review, assignment, feedback, audit | Advanced workflow agents |
| **6** | Enterprise, Scale & Research | Analytics/admin/research UI | AWS, observability, advanced security, scale | Multi-agent research |

---

# 102. Phase 1 — Platform, Identity & UI Foundation

## Objective

Create a working ARIS platform rather than only a backend skeleton.

### Backend

Implement:

- IdentityService
- PatientService
- HccMappingService
- GapEngineService
- Ocelot Gateway
- BuildingBlocks
- SQL Server integration
- Dockerfiles
- Health checks
- OpenAPI

### Angular

Implement:

- Login
- Application shell
- Header
- Sidebar
- Route guards
- HTTP authentication interceptor
- Dashboard shell
- Patient search
- Patient details
- Unauthorized page
- Not-found page

### Infrastructure

Implement:

- Docker Compose
- Environment configuration
- Service networking
- SQL Server container
- Health checks
- Docker Hub image workflow

### Exit Criteria

An authorized user can:

1. Open Angular.
2. Authenticate through IdentityService.
3. Receive and use an access token.
4. Navigate protected routes.
5. Search for a patient.
6. View patient details.
7. Logout.
8. Receive appropriate unauthorized responses.

---

# 103. Phase 2 — Clinical Data, Ingestion & Search

## Objective

Create the clinical data pipeline and searchable evidence foundation.

### Backend

Implement:

- DataIngestService
- RabbitMQ
- Outbox
- Event contracts
- Indexer Worker
- OpenSearch

### Data

Support initial canonical entities:

- Patient
- Encounter
- Diagnosis
- Procedure
- Clinical Note
- Provider

### Angular

Implement:

- Patient search
- Advanced filters
- Patient timeline
- Diagnosis history
- Encounter history
- Evidence search

### End-to-End Data Flow

```text
Source
 ↓
DataIngestService
 ↓
SQL Server
 ↓
Outbox
 ↓
RabbitMQ
 ↓
Indexer Worker
 ↓
OpenSearch
 ↓
Patient Search / Evidence UI
```

### Exit Criteria

A clinical record can move through the full ingestion-to-search pipeline and become visible through the Angular application.

---

# 104. Phase 3 — Deterministic Risk Intelligence


### RAF Calculation

Introduce `RafCalculationService` after HCC mapping and deterministic gap logic are sufficiently established.

Implement:

- Versioned RAF model configuration
- Demographic factor handling
- Validated HCC factor handling
- HCC hierarchy processing
- Interaction processing
- Coefficient application
- Patient RAF calculation
- RAF component breakdown
- Previous RAF comparison
- RAF delta
- Calculation provenance
- Reproducible calculation context

### Angular RAF Experience

Implement:

- Current RAF score
- Previous RAF
- RAF delta
- Factor breakdown
- HCC contribution
- Interaction contribution
- Model/version information
- Evidence drill-down
- Potential RAF impact for reviewable gaps



## Objective

Build the first authoritative risk-intelligence layer before introducing generative AI.

### HccMappingService

Implement:

- ICD-to-HCC mappings
- Model versions
- Effective dates
- Mapping metadata
- Version-aware APIs

### GapEngineService

Implement:

- Historical gap detection
- Recapture logic
- Temporal reasoning
- Evidence association
- Gap status
- Gap priority
- Contradiction indicators

### Angular

Implement:

- Risk dashboard
- Patient risk profile
- Open gaps
- Gap detail
- Evidence view
- HCC mapping view
- Gap status actions

### Exit Criteria

A user can:

```text
Open Patient
    ↓
View Risk Profile
    ↓
View Gap
    ↓
Understand Why It Exists
    ↓
Review Evidence
    ↓
Make a Human Decision
```

---

# 105. Phase 4 — RAG & Agentic Intelligence

## Objective

Introduce evidence-grounded generative intelligence.

### Backend

Implement:

- Embedding Worker
- Qdrant
- Hybrid retrieval
- Agent Orchestrator
- LLM provider abstraction
- Guardrails
- AI audit
- Evidence ranking

### Agent Tools

Initial tools should include:

```text
get_patient()
get_diagnoses()
get_encounters()
get_gaps()
map_icd_to_hcc()
search_evidence()
semantic_search()
get_hcc_details()
validate_evidence()
```

### Angular

Implement:

- Ask ARIS
- Explain Gap
- Evidence cards
- AI reasoning summary
- Citations
- Confidence
- Limitations
- Recommended human review

### Exit Criteria

A user can ask:

> "Why is this gap showing?"

and receive a response grounded in retrievable patient-specific evidence.

---

# 106. Phase 5 — Complete Clinical, Coding & Review Workflows

## Objective

Turn individual capabilities into complete user workflows.

### Clinician

Implement:

- Pre-visit summary
- Risk opportunities
- Evidence review
- AI assistance
- Encounter-oriented review

### Coder

Implement:

- Work queue
- Gap prioritization
- Evidence review
- HCC mapping
- Decision capture

### Risk Analyst

Implement:

- Population dashboard
- HCC analytics
- Gap analytics
- Trends
- Workload analysis

### Auditor

Implement:

- Evidence audit
- AI reasoning audit
- Human decision audit
- Provenance reconstruction

### Backend

Implement:

- Review workflows
- Assignments
- Comments
- Review events
- Feedback
- Audit APIs

### Exit Criteria

Each primary persona can complete their core workflow end-to-end.

---

# 107. Phase 6 — Enterprise, Scale & Research

## Objective

Turn ARIS into a production-grade and research-grade platform.

### Security

Implement:

- OIDC
- OAuth2
- Advanced RBAC
- ABAC
- MFA integration
- Organization-level authorization
- Patient-level authorization
- Secrets management
- Encryption

### AWS

Prepare deployment for:

- EKS or ECS
- RDS for SQL Server
- AWS networking
- Load Balancers
- WAF
- KMS
- Secrets Manager
- S3
- CloudWatch
- OpenSearch
- Amazon MQ where appropriate, while retaining RabbitMQ-compatible application contracts

### Observability

Implement:

- OpenTelemetry
- Distributed traces
- Structured logs
- Metrics
- Dashboards
- Alerts

### Research

Implement:

- Experiment registry
- Dataset versioning
- Ground-truth datasets
- Model comparison
- Retrieval comparison
- Agent evaluation
- Ablation studies
- Human feedback evaluation

---

# 108. Docker-First Development Standard

Docker is part of the implementation architecture, not merely a deployment convenience.

The local environment should ultimately be capable of running the ARIS platform through Docker Compose.

Conceptually:

```text
Docker Compose
 ├── Angular
 ├── Ocelot
 ├── IdentityService
 ├── PatientService
 ├── HccMappingService
 ├── GapEngineService
 ├── DataIngestService
 ├── SQL Server
 ├── RabbitMQ
 ├── OpenSearch
 └── Qdrant
```

Services may still be run directly from the IDE during development, but Docker must remain a supported and continuously tested execution path.

---

# 109. Docker Hub Workflow

The standard image workflow is:

```text
Source Code
     ↓
Docker Build
     ↓
Image
     ↓
Local Integration Test
     ↓
Docker Hub
     ↓
Deployment Environment
```

Image tags should eventually include immutable version identifiers, for example:

```text
aris/patient-service:1.0.0
aris/patient-service:git-<commit>
```

Production deployment should avoid relying solely on mutable `latest` tags.

---

# 110. Service Communication Principles

ARIS will use two primary communication mechanisms.

## Synchronous

Use HTTP/REST through Ocelot or controlled internal service APIs when an immediate response is required.

Examples:

```text
Angular → Ocelot → PatientService
GapEngineService → HccMappingService
```

## Asynchronous

Use RabbitMQ when processing can be decoupled.

Examples:

```text
PatientCreated
DiagnosisCreated
EncounterCreated
ClinicalNoteCreated
GapDetected
GapReviewed
EmbeddingCreated
```

---

# 111. Service Ownership

Each service owns its business capability and persistence boundary.

For example:

```text
PatientService
 └── Patient data

HccMappingService
 └── HCC mappings

GapEngineService
 └── Gap state and deterministic risk logic

IdentityService
 └── Identity and authorization data
```

A service must not directly modify another service's database.

---

# 112. Research-Grade Architecture

ARIS's architecture should deliberately support comparison of:

```text
Rules Only
    ↓
LLM Only
    ↓
LLM + Keyword Search
    ↓
LLM + Vector Search
    ↓
Hybrid RAG
    ↓
Agentic Hybrid RAG
    ↓
Multi-Agent Hybrid RAG
```

This enables the same platform to serve both production workflows and academic experimentation.

---

# 113. Recommended Research Baseline

The deterministic GapEngine should be treated as the first authoritative baseline.

This gives research experiments a stable comparison point:

```text
Deterministic Baseline
        ↓
RAG Improvement
        ↓
Agentic Improvement
        ↓
Human Feedback Improvement
```

The AI layer therefore augments the deterministic system rather than replacing it.

---

# 114. Complete End-to-End ARIS Architecture

```text
                                ┌────────────┐
                                │ Angular UI │
                                └────────────┘
                                       │
                                       ▼
                              ┌────────────────┐
                              │ Ocelot Gateway │
                              └────────────────┘
                                       │
                ┌──────────────────────┼──────────────────────┐
                │                      │                      │
                ▼                      ▼                      ▼
       ┌─────────────────┐    ┌────────────────┐    ┌──────────────────┐
       │ IdentityService │    │ PatientService │    │ GapEngineService │
       └─────────────────┘    └────────────────┘    └──────────────────┘
                                       │                      │
                                       ▼                      ▼
                                ┌────────────┐      ┌───────────────────┐
                                │ SQL Server │      │ HccMappingService │
                                └────────────┘      └───────────────────┘
                                       │
                                       ▼
                                  ┌──────────┐
                                  │ RabbitMQ │
                                  └────┬─────┘
                                       │
                     ┌─────────────────┼─────────────────┐
                     ▼                 ▼                 ▼
               ┌──────────┐      ┌──────────┐      ┌─────────────┐
               │ Indexer  │      │Embedding │      │   Analytics │
               │ Worker   │      │ Worker   │      │             │
               └────┬─────┘      └────┬─────┘      └─────────────┘
                    │                 │
                    ▼                 ▼
              ┌────────────┐    ┌────────────┐
              │ OpenSearch │    │   Qdrant   │
              └──────┬─────┘    └─────┬──────┘
                     │                │
                     └────────┬───────┘
                              ▼
                     ┌──────────────────┐
                     │ Agent Orchestrator│
                     └─────────┬────────┘
                               │
                               ▼
                           ┌───────┐
                           │  LLM  │
                           └───────┘
                               │
                               ▼
                      Evidence-Grounded
                          Explanation
                               │
                               ▼
                        Human Reviewer
                               │
                               ▼
                            Feedback
                               │
                               ▼
                        Evaluation Layer
```

---

# 115. ARIS Implementation Principle

The implementation should follow:

> **Build the simplest deterministic version first, expose it through a complete UI workflow, then progressively add retrieval, generative AI, agents, and adaptive capabilities.**

This avoids building an impressive AI layer on top of an unstable clinical data and workflow foundation.

---

# 116. Final Product Definition

ARIS is an:

> **Evidence-grounded, longitudinal, human-in-the-loop healthcare intelligence platform that integrates structured clinical data, deterministic risk-adjustment models, temporal reasoning, hybrid information retrieval, and agentic large-language-model workflows to identify, explain, prioritize, and manage potential risk-adjustment opportunities while maintaining provenance, uncertainty, auditability, security, and human decision authority.**

The platform is designed around one central objective:

> **Make the right information easier to find, make the reasoning easier to understand, and make the human decision easier to make.**

It should accomplish this without replacing clinical judgment or coding authority.

---

# 117. Implementation Reference Checklist

## Foundation

- [ ] Repository structure
- [ ] .NET solution structure
- [ ] Clean Architecture
- [ ] BuildingBlocks
- [ ] Dockerfiles
- [ ] Docker Compose
- [ ] SQL Server
- [ ] Health checks
- [ ] OpenAPI

## Identity

- [ ] IdentityService
- [ ] JWT
- [ ] Authentication
- [ ] RBAC
- [ ] Claims
- [ ] Angular route guards
- [ ] HTTP interceptor

## Clinical Data

- [ ] Patient
- [ ] Encounter
- [ ] Diagnosis
- [ ] Procedure
- [ ] Clinical Note
- [ ] Provider
- [ ] Data ingestion
- [ ] Data validation
- [ ] Provenance

## Messaging

- [ ] RabbitMQ
- [ ] Event contracts
- [ ] Outbox
- [ ] Consumers
- [ ] Retry
- [ ] Dead-letter handling

## Risk Intelligence

- [ ] HCC mappings
- [ ] Versioning
- [ ] Gap engine
- [ ] Temporal reasoning
- [ ] Evidence
- [ ] Contradiction handling
- [ ] Gap lifecycle
- [ ] Prioritization
- [ ] RafCalculationService
- [ ] RAF model/version configuration
- [ ] Demographic factors
- [ ] HCC factors
- [ ] HCC hierarchies
- [ ] Interaction rules
- [ ] Coefficients
- [ ] Patient RAF calculation
- [ ] RAF component breakdown
- [ ] RAF delta
- [ ] RAF provenance and reproducibility

## Search

- [ ] OpenSearch
- [ ] Keyword search
- [ ] Filters
- [ ] Indexing
- [ ] Qdrant
- [ ] Embeddings
- [ ] Semantic search
- [ ] Hybrid retrieval

## AI

- [ ] LLM abstraction
- [ ] Prompt management
- [ ] Agent orchestrator
- [ ] Tool registry
- [ ] Guardrails
- [ ] Evidence validation
- [ ] Citation generation
- [ ] AI audit

## UI

- [ ] Login
- [ ] Dashboard
- [ ] Patient search
- [ ] Patient details
- [ ] Patient timeline
- [ ] Risk dashboard
- [ ] Gap review
- [ ] Evidence
- [ ] Explain Gap
- [ ] Ask ARIS
- [ ] Reviewer workflow
- [ ] Analytics

## Security

- [ ] Authentication
- [ ] Authorization
- [ ] RBAC
- [ ] ABAC
- [ ] Secrets
- [ ] Encryption
- [ ] PHI-safe logging
- [ ] Audit

## Research

- [ ] Ground truth
- [ ] Dataset versioning
- [ ] Experiment registry
- [ ] Baselines
- [ ] Ablation studies
- [ ] Retrieval evaluation
- [ ] Agent evaluation
- [ ] Human-AI evaluation

## Operations

- [ ] OpenTelemetry
- [ ] Logs
- [ ] Metrics
- [ ] Traces
- [ ] Alerts
- [ ] Docker Hub
- [ ] CI/CD
- [ ] AWS deployment
- [ ] Backup
- [ ] Disaster recovery

---

# 118. Document Governance

This document should be treated as the **functional and implementation reference baseline** for ARIS.

Future documents should derive from it rather than independently redefining the product.

Recommended downstream documents:

1. System Requirements Specification (SRS)
2. Software Architecture Document (SAD)
3. Architecture Decision Records (ADR)
4. Domain Model
5. Database Design
6. API/OpenAPI Specification
7. Event Contract Specification
8. Angular UI/UX Specification
9. Security Architecture
10. AI/Agent Architecture
11. RAG Specification
12. Test Strategy
13. Deployment Architecture
14. Research Methodology
15. Experiment Plan
16. PhD Research Proposal

Changes to core behavior, service boundaries, security model, AI behavior, or research methodology should be documented as controlled changes rather than silently changing the baseline.
