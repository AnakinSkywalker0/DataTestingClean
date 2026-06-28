# Polar HR Integration — Setup Guide

## Requirements

```
pip install bleak
```

Python 3.8+ required. Tested with Polar H10 and H9.

## Scene Setup

Add three GameObjects to the Park scene (or use a single one):

| GameObject | Component |
|---|---|
| PolarHRLauncher | `PolarHRLauncher` |
| PolarHRReceiver | `PolarHRReceiver` |
| PolarHRLogger | `PolarHRLogger` |

`PolarHRLauncher` Inspector field **Python Executable**: set to `python` (Windows) or `python3` (Mac/Linux). If using a venv, set the full path to the interpreter.

## Usage

1. Wear the Polar sensor and confirm it is active (LED blinking).
2. Press **Play** in Unity — the bridge script launches automatically and scans for 10 s.
3. Press **S** to start EEG recording. HR logging begins simultaneously.
4. Press **E** to stop. HR CSV is flushed and closed, then EEG export runs.

Output file: `Desktop/Session_NNN_ddMMMyyyy_HR.csv`

Columns: `Timestamp, UnixTime, HeartRate_BPM`

## Troubleshooting

| Symptom | Fix |
|---|---|
| "No Polar device found" | Sensor not awake — wet strap contacts, tap sensor, re-run |
| `ModuleNotFoundError: bleak` | Run `pip install bleak` in the Python used by Unity |
| Wrong `python` path | Set full interpreter path in `PolarHRLauncher` Inspector field |
| No UDP data in Unity | Check firewall — port 12345 UDP localhost must be unblocked |
| HR CSV empty | `PolarHRLogger` GameObject missing from scene, or `OnEnable` not called |
| BLE scan finds nothing on Windows | Enable Bluetooth in Windows Settings; run Unity as Administrator if needed |

## How It Works

```
Polar H10 (BLE)
    └─ GATT HR characteristic 0x2A37
         └─ polar_hr_bridge.py  (bleak, async)
              └─ UDP 127.0.0.1:12345  (plain int string, e.g. "72")
                   └─ PolarHRReceiver.cs  (background thread → main-thread Queue)
                        └─ PolarHRLogger.cs  (CSV row per sample)
                        └─ EEGManager.cs  (StartLogging / StopLogging calls)
```
