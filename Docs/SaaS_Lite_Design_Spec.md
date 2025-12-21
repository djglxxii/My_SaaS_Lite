# SaaS Lite — Design Specification (from Investigation Summary)

**Platform constraint:** Microsoft ecosystem; use the latest .NET LTS (**.NET 10 LTS**, released November 11, 2025). ([Microsoft][1])
**Source basis:** SaaS Lite / POCT Operations Product Investigation Summary. 

---

## 1) Product definition

### 1.1 One-sentence purpose

A **managed POCT operations layer** that makes point-of-care testing **reliable, compliant, and “invisible”** for small clinics—without requiring IT, servers, configuration, or training. 

### 1.2 Target customer

**Included**

* Small clinics, POLs, urgent care, outpatient practices
* Typical fleet: **1–10** devices
* Minimal/no IT support and low tolerance for setup burden 

**Explicitly excluded**

* Enterprise POCT management parity requirements
* Customers demanding customization, scripting, plugins, or bespoke device behavior 

### 1.3 Non-goals (scope guardrails)

The product must refuse to become:

* LIS or EHR
* Configurable “platform”
* Real-time cloud device control plane
* Per-customer protocol behavior / “one-off” device timing tweaks
* Analytics-heavy dashboard product
* Professional-services-dependent solution 

---

## 2) Core principles (non-negotiable)

1. **All timing-sensitive, bidirectional device communication remains local.** 
2. **No customer customization:** fewer moving parts, fewer failure modes. 
3. **Opinionated defaults everywhere:** configuration is treated as risk. 
4. **Compliance is automatic:** enforced by the system, not “configured” by staff. 
5. **Offline tolerant with guaranteed delivery:** local buffering + eventual upload. 

---

## 3) Success criteria and measurable outcomes

### 3.1 Outcomes that define success

* **Time-to-first-result:** from signup to first captured result **< 4 hours**
* **Self-onboarding rate:** no phone call / no IT required
* **Device onboarding w/out engineering:** repeatable, deterministic
* **Low support burden:** low ticket volume per site per month
* **Audit readiness:** exportable, immutable, time-stamped evidence on demand 

### 3.2 Outcomes that do *not* define success

* Feature count / “parity” with enterprise products
* Complex dashboards or broad workflow engines 

---

## 4) System architecture

### 4.1 High-level topology

* **Devices ↔ Local Edge Agent ↔ Cloud SaaS**
* Devices never talk directly to cloud; cloud never participates in protocol timing. 

### 4.2 Components

#### A) Local Edge Agent (mandatory)

**Responsibilities**

* Device protocol handling (timing-safe, bidirectional)
* Session state + acknowledgements
* Local buffering (disk-backed queue)
* Normalization to canonical “Result” model
* Secure outbound delivery to cloud
* Minimal local “status only” UI (or none; LEDs/logs acceptable)

**Constraints**

* No scripting/plugins/custom drivers per customer
* Central version control (agent + drivers), remote update
* Operates safely offline; auto-resume delivery 

#### B) Cloud SaaS

**Responsibilities**

* Tenant/site/user identity and access control
* Result ingestion + immutable storage + retention enforcement
* Audit trails (append-only)
* Simple retrieval UI and simple exports
* Asynchronous configuration distribution to edge
* Monitoring and alerting (edge online/offline, delivery backlog, errors) 

---

## 5) Functional requirements (must-have)

### 5.1 Device onboarding (zero-configuration)

* Site admin selects from a **curated supported device list**
* Edge agent auto-registers to tenant/site
* “First device connected” wizard must be minimal: network + device type + confirm
* Hard “Supported / Not supported” stance; no “generic driver” claims 

### 5.2 Result capture and delivery

* Capture results automatically with required metadata (timestamps, device identity, operator if available)
* Persist locally immediately (before cloud ack)
* Upload asynchronously with retry/backoff
* Deduplicate uploads (idempotent ingestion keys)
* Guaranteed delivery semantics:

  * At-least-once from edge → cloud
  * Cloud ingestion must be idempotent

### 5.3 Immutable, audit-ready storage

* Stored results are tamper-evident / immutable
* Full audit log of:

  * logins, role changes
  * device enrollments
  * configuration changes pushed to edge
  * result ingestion, export events
* Retention presets (not freeform): e.g., 2/5/7/10 years (final presets to be defined with compliance) 

### 5.4 Compliance enforcement (built-in)

* QC and compliance checks enforced as product behavior, not user configuration
* System must produce “audit packet” export: results + audit log + retention evidence

### 5.5 Exports (simple one-way)

* Export to:

  * CSV
  * PDF report bundle
  * Simple outbound interface options (phase-based; see roadmap)
* Explicitly avoid “integration platform” scope in v1 

### 5.6 Minimal operational visibility (clinic UX)

* Clinic views:

  * “Devices online/offline”
  * “Last result received”
  * “Attention needed” (plain language)
  * “Exports” and “Audit packet”
* No “configuration matrices,” no multi-tab admin console mental model 

---

## 6) Non-functional requirements

### 6.1 Reliability

* Edge agent must recover automatically (service restart; watchdog)
* Local queue must survive power loss
* Cloud ingestion must be horizontally scalable

### 6.2 Security

* TLS everywhere
* Strong auth for users (MFA-capable)
* Strong device identity for edge agent
* Secrets stored in secure store (cloud + edge)
* Principle of least privilege per role

### 6.3 Privacy and compliance posture

* Clear data ownership boundaries per tenant
* Strict access controls and auditability
* Retention enforcement is automatic and verifiable 

### 6.4 Maintainability

* Versioned drivers and agent
* Backward-compatible ingestion contracts
* Central “supported device matrix” with explicit versions

---

## 7) UX specification

### 7.1 Roles

* **Clinic Admin:** can onboard site, add users, view exports/audit packet
* **Clinic Staff:** can search/view results, export basic reports
* **Support (internal):** can view fleet health, logs, and delivery status (no PHI by default; break-glass access requires explicit audit)

### 7.2 Onboarding flow (target: < 4 hours)

1. Create tenant/site (self-service)
2. Create first admin user
3. Provision edge agent (QR code / claim code)
4. Plug edge agent in; confirm “online”
5. Select device type; connect device
6. Run a test; confirm first result captured 

### 7.3 “Boring by design” UI rules

* Single primary navigation: Results / Devices / Exports / Audit Packet / Users
* Default views; minimal filters; no “advanced mode”

---

## 8) Data model (logical)

### 8.1 Core entities

* Tenant
* Site
* User (role-scoped to site)
* EdgeAgent (one per site recommended)
* Device (enrolled under site, linked to edge agent)
* Result (immutable record)
* AuditEvent (append-only)
* ExportJob (tracks what was exported and when)
* ConfigurationSnapshot (desired vs reported; versioned)

### 8.2 Canonical Result (minimum fields)

* ResultId (deterministic idempotency key)
* SiteId, DeviceId, EdgeAgentId
* CollectedAt (device time) + ReceivedAt (edge time) + IngestedAt (cloud time)
* TestCode / Assay / Panel
* Patient identifier (as allowed) + ordering context (if available)
* Operator identifier (if available)
* Raw payload reference (stored separately, immutable)
* Normalized analytes / qualitative outcomes
* QC flags / compliance tags

---

## 9) Microsoft platform specification (prototype-ready)

### 9.1 Runtime baseline

* **.NET 10 LTS** for cloud and edge workloads ([Microsoft][1])

### 9.2 Cloud (Azure)

* Web UI: ASP.NET Core (Razor Pages or Blazor Server)
* APIs: ASP.NET Core Web API
* Identity: Microsoft Entra ID (later: external identity option if needed)
* Data: Azure SQL Database for relational data
* Immutable storage: Azure Blob Storage with immutability/retention policy for result archives
* Messaging/config to edge: Azure IoT Hub (device identity + twins for desired/reported config)
* Monitoring: Application Insights + Azure Monitor + Log Analytics

*(These align with the “Microsoft Stack for SaaS Lite Prototype” document.)* 

### 9.3 Edge agent

* .NET 10 service on Linux appliance or container
* Device connectivity modules compiled and centrally versioned
* Disk-backed queue for offline buffering
* Secure connection to IoT Hub for telemetry + config sync 

---

## 10) Operational specification

### 10.1 Observability (must-have)

* Device online/offline events per site
* Queue depth / delivery backlog per edge
* Ingestion success/failure rates
* “Last known good upload” timestamp
* Alerting rules (internal):

  * edge offline > X minutes
  * backlog > threshold
  * repeated protocol errors

### 10.2 Support posture

* Fleet dashboard for internal ops
* Remote log bundle request (edge → cloud)
* Break-glass access with explicit audit trail

---

## 11) Release plan (scope-preserving)

### v0 Prototype (prove viability)

* 1–2 device types (curated)
* End-to-end: edge capture → cloud storage → results UI → CSV export
* Offline buffering demonstration
* Basic audit log + basic “audit packet” export

### v1 (sellable “POCT utility”)

* Expanded curated device list (still limited)
* Retention enforcement + immutable archives
* Strong onboarding experience + self-service provisioning
* Minimal support operations toolset

### vNext (only if demanded by validated adoption)

* Additional export destinations (still one-way)
* Additional compliance packets/templates
* Incremental device additions (no per-customer behavior)

---

## 12) Red-line change control (initiative protection)

Any request that introduces the following is treated as a “stop sign” requiring executive product review:

* “Just add this one customer exception”
* “Hide advanced features”
* “Make it configurable later”
* “Move protocol timing to cloud”
* “Professional services can handle onboarding” 

---

## 13) Open items (intentionally unresolved until you decide)

* Exact retention presets required for target markets
* Initial curated supported device list (business selection)
* Canonical Result schema details per device category (quant/qual, panels, units)
* Which export options are mandatory for v1 (CSV-only vs adding a single EHR-facing export)

---

[1]: https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core?utm_source=chatgpt.com "NET and .NET Core official support policy"
