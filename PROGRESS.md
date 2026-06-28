# Project Progress Summary

## Project
Unity VR Pedestrian Safety Research Simulator  
Repo: https://github.com/AnakinSkywalker0/Pedestrian-behaviour-Analysis  
Active branch: `feature/eye-tracking-eeg-markers`

---

## What Was Built

### 1. EEG Integration (`Assets/EEGManager.cs`)
- Singleton MonoBehaviour with `DontDestroyOnLoad`
- Connects to Emotiv Cortex via WebSocket (`wss://localhost:6868`)
- Auto-discovers and connects to EPOC X headset
- **Session title**: auto-generated as `Session_001_23May2026` using `PlayerPrefs` counter + `DateTime.Now`
- **S key** → `StartRecording()` — starts EEG session, resets gaze episode counter
- **E key** → `StopRecording()` — stops recording, waits for post-processing (WarningCode 30), exports PM + MOTION CSVs to Desktop
- **Space key** → `InjectInstanceMarker("stimulus_onset", 1)` — manual stimulus marker
- Guards on `IsSessionReady && IsRecording` before injecting any marker

### 2. Gaze Marker System

#### `Assets/VIVE/EyeTracker/Scripts/GazeMarkerEvents.cs`
- Static event bridge decoupling the VIVE assembly from EEGManager assembly
- `public static event Action<string, int> OnGazeMarker`
- `public static void Invoke(string label, int value)`
- EEGManager subscribes in `Awake`, unsubscribes in `OnDestroy`

#### `Assets/VIVE/EyeTracker/Scripts/ObjectDetectedByEyeGazeValue.cs`
- Raycasts from left/right eye transforms every 0.1s against `visible` LayerMask
- Detects any object; fires EEG markers only for objects tagged `EEGMarker`
- **`start_gaze_N`** (value 1) — fires immediately when gaze arrives on EEGMarker object
- **`end_gaze_N`** (value 0) — fires after 0.15s exit grace period (suppresses raycast flicker)
- `_gazeEpisode` counter increments per start, resets to 0 on `ResetGazeEpisode()`
- Debug line drawn from eye midpoint: green = normal gaze, red = on EEGMarker object
- Logs every 0.1s to `DataSet/EyeTrackingData/{id}/EyeTracking_task0.csv`

#### `Assets/TestInput.cs`
- Keyboard controller: S / Space / E
- Attached to a dedicated GameObject in the Park scene

### 3. Eye Tracking Fixes
- **`EyeTrackerTest.cs`**, **`EyeGazeObjectTimer.cs`**, **`ObjectDetectedByEyeGazeValue.cs`**: Added null/length guards on `GetEyeGazeData`, `GetEyePupilData`, `GetEyeGeometricData` — fixes NullReferenceException spam when XR session is lost
- **`EyeTrackingStatusLogger.cs`**: Attach to any scene to log whether eye tracking is active on Play

### 4. Debug Log Cleanup
- Removed per-tick `Debug.Log` from `KatVRDataLogger.cs` (car detection spam)
- Removed per-tick `Debug.Log` from `NearestCarFinder.cs`
- Gaze log now only fires on object change (not every 0.1s)

### 5. Git Setup
- Initialized fresh repo, proper `.gitignore` (excludes `/Library/`, `/Temp/` etc.)
- `.gitattributes` with Git LFS for large binaries (FBX, OBJ, TGA, DLL, etc.)
- `main` branch: base project
- `feature/eye-tracking-eeg-markers`: all EEG + eye tracking work

---

## Scene Setup (Park Scene)

| GameObject | Component | Notes |
|---|---|---|
| EEGManager | `EEGManager` | DontDestroyOnLoad, fill clientId/clientSecret in Inspector |
| TestInputController | `TestInput` | S/Space/E keyboard triggers |
| ObjectDetecter | `ObjectDetectedByEyeGazeValue` | leftEye, rightEye assigned; Visible = Default + Car_; Logger assigned |
| TestCube | `BoxCollider` | Tag: EEGMarker, Layer: Default |
| LOgger | `DataLogger` | Writes EyeTracking + Movement CSVs |

---

## Data Outputs

| File | Location | Trigger |
|---|---|---|
| `EyeTracking_task0.csv` | `DataSet/EyeTrackingData/{id}/` | Every 0.1s while eye tracking active |
| `Movement_task0.csv` | `DataSet/MovementData/{id}/` | Every 0.1s |
| `Session_NNN_ddMMMyyyy_PM.csv` | Desktop | After pressing E + export completes |
| `Session_NNN_ddMMMyyyy_MOTION.csv` | Desktop | After pressing E + export completes |

---

## Known Limitations / Pending

- **INTERSECTION and MidBlock scenes** have no XR Origin — not tested with VR
- **SUMO bridge** (`sumo_bridge_server.py`) is a separate repo/file, not yet integrated
- Eye tracking requires calibration on VIVE Focus Vision headset before gaze data flows
- EEG export requires Emotiv Cortex running on PC with EPOC X headset connected
