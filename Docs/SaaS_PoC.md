Below is a concrete starting plan for a **minimal demo-grade PoC** that proves the core loop: **“edge captures results locally → cloud ingests → UI shows results → export CSV”**, with mocked data and **no security**. 

## PoC scope (keep it demo-simple)

### In-scope (v0)

* **Edge “agent”** (console app)

  * Simulate 1–2 device types (generate deterministic results)
  * **Disk-backed queue** (JSON lines file or LiteDB) to demo offline buffering
  * Upload results to Cloud API with retry
* **Cloud API** (ASP.NET Core Minimal API)

  * Ingest results (idempotent key)
  * Store in a local DB (SQLite) for the demo
* **Web UI** (Razor Pages)

  * Results list + detail view
  * Devices page (simple “online/offline” simulated)
  * CSV export for a date range
* **Demo “offline mode”**

  * Stop Cloud API, run Edge to queue results
  * Start Cloud API, watch Edge flush backlog

### Explicitly out-of-scope (for PoC)

* AuthN/AuthZ, MFA, roles
* Azure IoT Hub / device twins
* Immutable storage, retention policies, audit packet
* Real device protocol timing (HL7/ASTM/POCT1A)

This matches the spec’s v0 intent: end-to-end viability, not compliance/security. 

---

## Proposed solution layout (one repo, 3 projects)

```
SaaSLite.Poc.sln
/src
  /SaaSLite.CloudApi        (ASP.NET Core Minimal API, .NET 10)
  /SaaSLite.Web             (Razor Pages, .NET 10)
  /SaaSLite.EdgeAgent       (Console app, .NET 10)
/shared
  /SaaSLite.Contracts       (DTOs + shared models)
/dev
  docker-compose.yml        (optional: local DB + app)
```

### Why Razor Pages for PoC

Fast page-based UI for “boring by design” screens (Results / Devices / Exports) without SPA overhead. 

---

## Minimal data model (PoC)

### Entities (SQLite)

* `Device`

  * `DeviceId` (string)
  * `SiteId` (string) (hardcode `site-001`)
  * `DisplayName`
  * `DeviceType` (enum/string)
  * `LastSeenUtc`
  * `Status` (“Online/Offline”)
* `Result`

  * `ResultId` (**idempotency key**; deterministic)
  * `SiteId`, `DeviceId`, `EdgeAgentId`
  * `CollectedAtUtc`, `ReceivedAtUtc`, `IngestedAtUtc`
  * `TestCode`
  * `PatientId` (mock string)
  * `OperatorId` (mock string)
  * `NormalizedJson` (string)
  * `RawPayloadJson` (string)

This aligns with the spec’s “canonical Result” concept but kept minimal for demo. 

---

## API endpoints (CloudApi)

* `POST /api/results`

  * Body: `ResultIngestRequest`
  * Behavior:

    * If `ResultId` already exists → return `200 OK` (idempotent)
    * Else insert and return `201 Created`
* `GET /api/results?fromUtc=&toUtc=&deviceId=`
* `GET /api/results/{resultId}`
* `GET /api/devices`
* `POST /api/devices/{deviceId}/heartbeat`

  * Edge calls this periodically to update `LastSeenUtc`

---

## EdgeAgent behavior (demo-first)

### Components

* `DeviceSimulator`

  * Emits a “result” every N seconds
  * Has a fixed `DeviceId` and `DeviceType`
* `LocalQueue`

  * Disk-backed queue: simplest is **JSONL file** with “pending/acked” markers, or LiteDB
* `Uploader`

  * Reads queue, posts to `/api/results`
  * On success marks as acked
  * Retry/backoff when API is down
* `Heartbeat`

  * Calls `/api/devices/{deviceId}/heartbeat` every ~10 seconds

### Demo script

1. Start CloudApi + Web
2. Start EdgeAgent: observe results show up in UI
3. Stop CloudApi: EdgeAgent continues generating results (queue grows)
4. Start CloudApi: EdgeAgent flushes backlog

This demonstrates the spec’s offline tolerance + eventual upload principle without Azure complexity. 

---

## UI pages (Web)

Navigation (keep exactly “boring”):

* **Results**

  * Table: time, device, test, patient, status (always “Received” for PoC)
  * Simple filter: date range + device
* **Devices**

  * Table: device, type, status, last seen
* **Exports**

  * Date range + device filter → download CSV

---

## Step-by-step: implement in this order (fastest path)

1. **Create solution + projects**

   * Contracts shared DTOs
2. **CloudApi**

   * SQLite + EF Core (or Dapper)
   * Implement `/api/results` with idempotency
3. **Web**

   * Results list page pulling from CloudApi
4. **EdgeAgent**

   * Generate results
   * Queue to disk
   * Upload loop + retry
5. **Devices page**

   * Heartbeat updates status
6. **Export**

   * CSV endpoint in CloudApi + UI button

---

## Initial “Definition of Done” for the PoC

* EdgeAgent can run on a laptop and **simulate two devices**
* You can demonstrate **offline buffering** by stopping CloudApi
* Web UI shows results and device status
* CSV export downloads the same data shown in Results

---

## If you want code generation next

I can generate the full starter repo skeleton (solution, projects, DTOs, API endpoints, Razor Pages, and the edge queue/uploader) in one pass, targeting **.NET 10** and SQLite, matching the structure above. 
