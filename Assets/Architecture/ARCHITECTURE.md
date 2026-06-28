# TestEEG — Technical Architecture

## Repository layout

```
Assets/
├── EEGManager.cs          — singleton MonoBehaviour, owns the full session lifecycle
├── TestInput.cs           — keyboard input bridge, calls EEGManager public API
├── unity-plugin/          — Emotiv Unity Plugin (read-only, do not modify)
│   └── Src/
│       ├── EmotivUnityItf.cs      — public facade for the entire plugin
│       ├── DataStreamManager.cs   — headset connection and stream subscription
│       ├── DataStreamProcess.cs   — WebSocket auth flow and device connect
│       ├── RecordManager.cs       — start/stop record, inject markers, export
│       ├── Authorizer.cs          — OAuth-style Cortex token acquisition
│       ├── WebsocketCortexClient.cs — WebSocket4Net wrapper for Cortex API
│       ├── Config.cs              — static configuration (URL, credentials, timers)
│       └── Types.cs               — Record, Headset, ConnectToCortexStates, etc.
```

---

## Runtime transport

The Emotiv Cortex Service exposes a **local WebSocket API** at `wss://localhost:6868`
(TLS 1.2, self-signed cert trusted by the Emotiv Launcher installer).

All plugin communication is async: requests are sent as JSON-RPC messages on the main
thread; responses arrive on a **background WebSocket thread** and fire C# events.
`EmotivUnityItf` properties (`IsAuthorizedOK`, `IsRecording`, `MessageLog`, etc.) are
written on that background thread and read on Unity's main thread without explicit
locking — safe on x86/x64 due to atomic 32-bit reads, but worth noting.

---

## State machine

### Phase 1 — Auth (automatic, runs on Play)

```
Service_connecting
  → Login_waiting        (WebSocket opened, getUserLogin sent)
  → Authorizing          (user found, hasAccessRight → true → authorize sent)
  → Authorized           (cortex token acquired, license validated)
```

Detected in `EEGManager.Update()` via `EmotivUnityItf.Instance.GetConnectToCortexState()`.

### Phase 2 — Headset + Session (coroutine: AuthAndConnectRoutine)

```
QueryHeadsets()          (triggers HeadsetFinder scan)
  → wait 3 s
StartDataStream(["met"], "")
  → StartConnectToDevice()   if headset.Status != "connected"
    → Warning 104 fires      (HeadsetConnected)
    → OnHeadsetConnectNotify → CreateSession()
  → CreateSession() directly if headset already connected
  → SessionActivatedOK fires → IsSessionCreated = true → IsSessionReady = true
```

Key constraint: `StartDataStream` is called **once per 33-second cycle** (3 s query
wait + 30 s session poll). Calling it a second time while `CreateSession` is in flight
issues a duplicate request that silently aborts the pending session.

### Phase 3 — Recording (user-driven)

```
StartRecording(title)
  → StartRecord(title)          Cortex creates record
    → OnInformStartRecordResult  IsRecording = true, RecentRecord.Uuid set

InjectInstanceMarker(label, value)
  → InjectMarker(label, value.ToString())   timestamped on Cortex side

StopRecording()
  → StopRecord()
    → OnInformStopRecordResult   IsRecording = false, RecentRecord updated
  → StopAndExportRoutine (coroutine):
      wait IsRecording == false  (up to 12 s)
      capture _pendingExportUuid from RecentRecord.Uuid
      wait _dataProcessingFinished == true  (up to 30 s)
        ← set by Update() when MessageLog = "Data post processing finished..."
        ← this is WarningCode 30 (DataPostProcessingFinished)
      ExportSession()
```

### Phase 4 — Export

```
ExportRecord(
  records:   [_pendingExportUuid],
  folder:    Desktop,
  streams:   ["PM", "MOTION"],
  format:    "CSV",
  version:   "V2",
  includeMarkerExtraInfos: true    ← produces separate markers CSV
)
  → OnExportRecordsFinished → MessageLog updated ("successfully" / "failed")
```

---

## EEGManager public API

| Method | Guard | Effect |
|--------|-------|--------|
| `StartRecording(string title)` | `IsSessionReady && !IsRecording` | Sends createRecord to Cortex |
| `InjectInstanceMarker(string label, int value)` | `IsRecording` | Stamps current time with label+value |
| `StopRecording()` | `IsRecording` | Stops record, launches export coroutine |
| `IsSessionReady` (bool, read-only) | — | True once Cortex session is active |

All guards use `EmotivUnityItf.Instance.IsRecording` (plugin's authoritative state),
not a local mirror, to avoid stale-flag bugs.

---

## Key plugin method signatures

```csharp
// EmotivUnityItf.cs
void Init(string clientId, string clientSecret, string appName,
          bool allowSaveLogToFile = true, bool isDataBufferUsing = true,
          string appUrl = "", string providerName = "", string emotivAppsPath = "")

void Start(object context = null)
void QueryHeadsets(string headsetId = "")
void StartDataStream(List<string> streamNameList, string headsetId)
void StartRecord(string title, string description = null,
                 string subjectName = null, List<string> tags = null)
void StopRecord()
void InjectMarker(string markerLabel, string markerValue)   // value is string
void ExportRecord(List<string> records, string folderPath,
                  List<string> streamTypes, string format, string version = null,
                  List<string> licenseIds = null, bool includeDemographics = false,
                  bool includeMarkerExtraInfos = false, bool includeSurvey = false,
                  bool includeDeprecatedPM = false)

// Observable properties
bool   IsAuthorizedOK
bool   IsSessionCreated
bool   IsRecording
string WorkingHeadsetId
string MessageLog          // updated on background thread; polled in Update()
Record RecentRecord        // Uuid, Title, StartDateTime, EndDateTime
ConnectToCortexStates GetConnectToCortexState()
```

---

## Export stream types vs subscription stream names

These are **different namespaces** — the subscription name used in `StartDataStream`
is NOT the same as the export type passed to `ExportRecord`:

| Data | Subscription name | Export stream type |
|------|-------------------|--------------------|
| Performance Metrics | `"met"` | `"PM"` |
| Motion | `"mot"` | `"MOTION"` |
| EEG raw | `"eeg"` | `"EEG"` |
| Band Power | `"pow"` | `"BP"` |

Passing the wrong value (e.g. `"MET"` instead of `"PM"`) returns Cortex error `-32602`
(Invalid Parameters).

---

## WarningCode reference (relevant codes)

| Code | Constant | Meaning |
|------|----------|---------|
| 18 | `StreamWritingClosed` | Data stream writer closed |
| 23 | `CortexIsReady` | Cortex service fully initialised |
| 30 | `DataPostProcessingFinished` | Record post-processing done; safe to export |
| 104 | `HeadsetConnected` | Physical headset connection confirmed |
| 142 | `HeadsetScanFinished` | One full BLE/USB scan cycle completed |

WarningCode 30 is detected in `EEGManager.Update()` via the `MessageLog` string
`"Data post processing finished for record: <title>"` set by
`EmotivUnityItf.OnDataPostProcessingFinished`.

---

## Known constraints and gotchas

- **Do not call `StartDataStream` repeatedly** while a `CreateSession` is in flight —
  duplicate calls silently abort the pending session and restart the scan loop.
- **`InjectMarker` second parameter is `string`** — callers must convert int to string.
- **Cortex URL must be passed explicitly** — `Config.AppUrl` has no hardcoded default;
  passing `appUrl = ""` (the default) results in `new WebSocket("")` failing silently
  with no Console output.
- **Export requires post-processing to finish first** (WarningCode 30, ~4 s after stop).
  Calling `ExportRecord` before this fires returns "Export record ... failed" from Cortex.
- **Marker CSV requires `includeMarkerExtraInfos: true`** — without it, markers are
  written only to a JSON sidecar, not the CSV.
