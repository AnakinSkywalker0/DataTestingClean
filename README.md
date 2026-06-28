# DataTesting Setup

## Requirements
- Unity 2022.3.62f3
- Emotiv Cortex app running on localhost:6868
- Python 3.10 with bleak installed (pip install bleak)
- SteamVR or VIVE software for eye tracking
- Polar HR device + USB dongle

## Before hitting Play
1. Open Emotiv Cortex app
2. Run polar_hr_bridge.py manually or set Python path in PolarHRLauncher Inspector
3. Connect VIVE headset via SteamVR
4. Enter clientId and clientSecret in EEGManager Inspector

## Data output location
Project/Data/{ParticipantName}_{ParticipantID}/

---

# VR Pedestrian Safety Research Simulator — User Manual

Unity-based VR experiment for studying pedestrian road-crossing behaviour.
Records eye gaze, EEG performance metrics, heart rate, and movement simultaneously.

---

## Hardware Required

| Device | Purpose |
|---|---|
| VIVE Focus Vision | VR headset with built-in eye tracking |
| Emotiv EPOC X | EEG headset for neural performance metrics |
| Polar H10 / H9 | Chest strap heart rate monitor |
| Research PC (Windows 11) | Runs Unity, Emotiv Cortex, Python bridge |

---

## Software Required

| Software | Version | Notes |
|---|---|---|
| Unity | 2022.x (URP) | Open project from this folder |
| Emotiv Cortex | Latest | Must be running before pressing Play |
| Python | 3.10 | Located at `D:\Python 3.10\python.exe` |
| bleak (Python package) | 3.0.2+ | Already installed |

---

## First-Time Setup

### 1. Emotiv Cortex
- Install and launch Emotiv Cortex on the PC
- Log in with your Emotiv account
- In `Assets/EEGManager` GameObject → Inspector: fill in **Client ID** and **Client Secret** from your Emotiv app at emotiv.com/my-account

### 2. Polar Heart Rate Sensor
- Wet the strap electrodes before wearing
- Pair the sensor once in **Windows Settings → Bluetooth & devices → Add device**
- Confirm Bluetooth is ON before each session

### 3. Python Bridge (automatic)
The bridge launches automatically when Unity Play is pressed.
If it fails, check the Unity Console for `[PolarHRLauncher]` errors.
Manual test:
```
cmd /k ""D:\Python 3.10\python.exe" "D:\Final_Objective1_Validation\Assets\PolarHR\Python\polar_hr_bridge.py""
```

---

## Scene Overview

Open **Assets/Scenes/Park.unity** for the main experiment scene.

### Key GameObjects

| GameObject | Components | Role |
|---|---|---|
| EEGManager | `EEGManager` | Connects to Emotiv Cortex, manages EEG session |
| PolarHR | `PolarHRLauncher`, `PolarHRReceiver`, `PolarHRLogger` | Launches Python bridge, receives BPM over UDP, writes HR CSV |
| TestInputController | `TestInput` | Keyboard controls for the experimenter |
| ObjectDetecter | `ObjectDetectedByEyeGazeValue` | Raycasts gaze, fires EEG markers, logs eye data |
| TestCube | — | Tag: `EEGMarker`, used to test gaze marker injection |
| LOgger | `DataLogger` | Writes EyeTracking + Movement CSVs |

---

## Running an Experiment Session

### Pre-session checklist
- [ ] Emotiv Cortex running and logged in
- [ ] EPOC X headset on participant, good contact (check Cortex signal quality)
- [ ] Polar strap worn, Bluetooth ON, sensor LED blinking
- [ ] VIVE Focus Vision on participant, eye tracking calibrated
- [ ] Unity project open, Park scene loaded

### Step-by-step

**1. Press Play in Unity**
- EEGManager connects to Cortex and begins headset discovery (takes ~10–30 s)
- Python bridge launches automatically and scans for Polar device
- Console shows `[EEGManager] Session ready` when EEG is connected
- Console shows `[PolarHRLauncher] Started bridge PID=...`

**2. Press S — Start Recording**
- Starts EEG session recording
- Starts HR CSV logging simultaneously
- Resets gaze episode counter to 0
- Session is named automatically: `Session_NNN_ddMMMyyyy`

**3. During the experiment**
- Participant walks through the VR street scene
- Eye gaze is logged every 0.1 s to CSV
- Movement is logged every 0.1 s to CSV
- Heart rate streams to Unity via UDP and is written to HR CSV
- Press **Space** to inject a manual EEG stimulus marker (`stimulus_onset`)

**4. Press E — Stop Recording**
- Stops HR logging, flushes and closes HR CSV
- Stops EEG recording
- Waits for Emotiv post-processing (WarningCode 30, up to 30 s)
- Exports EEG data automatically to Desktop

**5. Press Stop in Unity**
- Python bridge process is killed automatically

---

## Keyboard Controls (Experimenter)

| Key | Action |
|---|---|
| S | Start EEG + HR recording |
| E | Stop EEG + HR recording, trigger EEG export |
| Space | Inject manual EEG stimulus marker |
| H | Start / stop HR-only logging (no EEG required — for standalone testing) |

---

## Data Outputs

All files are written automatically. No manual saving needed.

| File | Location | Trigger | Contents |
|---|---|---|---|
| `EyeTracking_task0.csv` | `Dataset/EyeTrackingData/{id}/` | Every 0.1 s during Play | Gaze position, AOI, pupil diameter, saccade speed, blink, head angles |
| `Movement_task0.csv` | `Dataset/MovementData/{id}/` | Every 0.1 s during Play | Participant position and heading |
| `Session_NNN_ddMMMyyyy_HR.csv` | Desktop | S key → E key | Timestamp, Unix time, BPM |
| `Session_NNN_ddMMMyyyy_PM.csv` | Desktop | After E key + post-processing | Emotiv Performance Metrics (engagement, workload, etc.) |
| `Session_NNN_ddMMMyyyy_MOTION.csv` | Desktop | After E key + post-processing | Emotiv motion data |

---

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `[EEGManager] session not ready` on S | Cortex not connected or session not created yet | Wait for `Session ready` in Console, then press S |
| No HR CSV on Desktop | S key fired before EEG session was ready | Use H key to test HR independently |
| Python bridge window closes instantly | Bluetooth off | Turn on Bluetooth in Windows Settings |
| `No Polar device found` | Sensor not awake or not paired | Wet strap, tap sensor, pair in Windows Bluetooth |
| 180+ eye tracking errors in Console | No HMD connected (editor mode) | Fixed — `XRSettings.isDeviceActive` guard suppresses calls when headset is absent |
| `[PolarHRLauncher] Failed to start bridge` | Wrong Python path | Check Inspector field on PolarHR GameObject → `D:\Python 3.10\python.exe` |
| EEG export never arrives on Desktop | Post-processing timeout | Wait up to 60 s after pressing E; check Cortex is still running |
| Session counter keeps incrementing | `PlayerPrefs` persists between runs | Reset via Edit → Clear All PlayerPrefs, or note the counter increments on every S press |

---

## Branch Structure

| Branch | Contents |
|---|---|
| `main` | Base Unity project |
| `feature/eye-tracking-eeg-markers` | EEG + gaze marker system |
| `stable_integrated_version` | Full integration: EEG + eye tracking + heart rate |

---

## Project File Map

```
Assets/
├── EEGManager.cs               — Emotiv Cortex session management
├── TestInput.cs                — S / E / Space / H keyboard controller
├── PolarHR/
│   ├── Python/polar_hr_bridge.py   — BLE → UDP heart rate bridge
│   └── Scripts/
│       ├── PolarHRLauncher.cs      — Spawns Python subprocess
│       ├── PolarHRReceiver.cs      — UDP listener, fires OnHRReceived event
│       └── PolarHRLogger.cs        — Writes HR CSV
├── VIVE/EyeTracker/Scripts/
│   ├── ObjectDetectedByEyeGazeValue.cs  — Gaze raycast + EEG marker injection
│   └── GazeMarkerEvents.cs              — Static event bridge between assemblies
Dataset/
├── EyeTrackingData/            — Per-participant eye tracking CSVs
└── MovementData/               — Per-participant movement CSVs
```
