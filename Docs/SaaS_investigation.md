# SaaS Lite / POCT Operations Product Investigation Summary

## 1. Market Reality (From Field Research)

Small clinics, physician-owned labs (POLs), urgent care centers, and outpatient practices globally exhibit **consistent, well-documented pain points**:

* Severe **staff overload**; no spare capacity for new tools
* Little to no **dedicated IT support**
* High **regulatory anxiety** (CLIA, audits, QC, retention)
* Deep **frustration with EHR usability**, but no appetite to replace EHRs
* POCT adoption constrained by:

  * Manual result entry
  * Workflow disruption
  * Compliance complexity
  * Cost uncertainty
* Strong **aversion to configurability**, customization, and multi-step onboarding
* **Tight budgets**, favoring low, predictable OpEx over CapEx
* Preference for **solutions that remove work**, not add features

Critically, this segment is **not asking for more power or flexibility**.
They want fewer moving parts and fewer failure modes.

---

## 2. Viable Product Opportunity Identified

The only product category with a realistic chance of adoption is:

> **A managed POCT operations layer that makes point-of-care testing reliable, compliant, and invisible.**

This is:

* Not a LIS
* Not an EHR
* Not middleware in the traditional sense
* Not a configurable platform

It is a **POCT utility service** whose value is defined by *what clinics no longer have to think about*.

---

## 3. Product Purpose (Core Intent)

**Enable small clinics to run POCT devices without owning or managing:**

* IT infrastructure
* Device connectivity logic
* QC enforcement logic
* Compliance interpretation
* Result retention strategy
* Audit preparation workflows

The product succeeds if clinics can:

* Plug in supported devices
* Run tests
* Retrieve results
* Pass audits
  …without configuration, training, or IT involvement.

---

## 4. Alignment with Abbott Capabilities

This opportunity aligns directly with Abbott’s strengths:

* Deep control and knowledge of device behavior and protocols
* Regulatory credibility and QA culture
* Experience with timing-sensitive device communication
* Ability to deliver validated, locked-down solutions
* Existing trust relationships with clinical customers

It avoids areas where Abbott is structurally disadvantaged:

* EHR competition
* Custom integration projects
* Workflow engines
* Analytics platforms

---

## 5. Non-Negotiable Product Capabilities

The product must provide:

1. Local, timing-safe device communication
2. Zero-configuration device onboarding
3. Automatic result capture
4. Immutable, audit-ready storage
5. Built-in compliance enforcement
6. Offline tolerance with guaranteed delivery
7. Simple one-way exports
8. Minimal operational visibility
9. Self-service onboarding
10. Predictable, low monthly pricing

If any of these are compromised, the product loses viability.

---

## 6. Explicit Scope Guardrails

The product must explicitly refuse to:

* Become a LIS or EHR
* Support customization, scripting, or plugins
* Offer real-time cloud device control
* Enable per-customer protocol behavior
* Provide advanced analytics or dashboards
* Support enterprise hierarchies or parity
* Require professional services
* Accept “temporary” exceptions

Scope discipline is essential to survival.

---

## 7. Clinic-First Value Proposition

> **“We make point-of-care testing run quietly in the background.”**
>
> Devices work.
> Results are captured.
> Compliance is enforced.
> Audits are painless.
>
> No servers.
> No IT.
> No configuration.

---

## 8. Strategic Implication

This product will:

* Appear intentionally “underpowered” to enterprise stakeholders
* Conflict with internal pressures for reuse and parity
* Succeed only if protected from feature creep

Its strength is **boring reliability**, not extensibility.

---

## 9. Role of This Summary

This document is intended to:

* Serve as the **foundation for a formal design specification**
* Act as a **scope and intent anchor** during design and review
* Prevent reintroduction of failed assumptions from prior products
* Provide a reference point for future architectural and UX decisions
