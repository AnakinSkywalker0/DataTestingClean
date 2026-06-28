# TestEEG — Project Overview

## What this project does

TestEEG is a Unity application that connects to an Emotiv EPOC X EEG headset, records
brain and body data during a session, marks specific moments in time, and exports the
recorded data as CSV files for analysis.

Think of it like a stopwatch and notepad for your brain: you press a key to start
recording, press another key each time something interesting happens (like a stimulus
appearing on screen), then press a key to stop. The app packages everything up — the
brain data, the body movement data, and the timestamps of your marked events — into
spreadsheet files on your Desktop.

---

## The hardware

**Emotiv EPOC X** — a wireless EEG headset worn on the head. It measures:
- **Performance Metrics (PM)** — derived scores for focus, stress, engagement, excitement,
  relaxation, and interest, updated every second.
- **Motion (MOTION)** — accelerometer and gyroscope data from the headset itself.

The headset connects to the computer via a USB dongle.

---

## The software stack

```
Your Keyboard
     ↓
  TestInput.cs   (reads S / Space / E keys)
     ↓
  EEGManager.cs  (the brain of the app — manages the full lifecycle)
     ↓
  Emotiv Unity Plugin  (communicates with Cortex Service)
     ↓
  Cortex Service  (Emotiv's background app, runs locally on your PC)
     ↓
  EPOC X Headset  (via USB dongle)
```

---

## What each key does

| Key | Action |
|-----|--------|
| **S** | Start a new recording named "TestSession_001" |
| **Space** | Stamp the current moment with a marker labelled "stimulus_onset" |
| **E** | Stop the recording and automatically export to Desktop |

---

## What appears on your Desktop after pressing E

Two or three CSV files appear within about 10–15 seconds:

- **PM data file** — columns for each performance metric score over time
- **MOTION data file** — accelerometer / gyroscope readings over time
- **Markers file** — timestamps and labels for every Space-key press (EEGLAB-compatible)

---

## The connection sequence (what happens when you press Play)

1. App registers itself with the Emotiv Cortex Service using your developer credentials
2. Cortex confirms your app is authorised
3. App finds your EPOC X headset and opens a data session
4. `IsSessionReady` becomes true — you can now record
5. (Nothing further happens until you press S)

---

## Credentials

Three values must be set on the EEGManager GameObject in the Unity Inspector:

- **Client ID** and **Client Secret** — from your Emotiv developer account
  (emotiv.com/developer)
- **App Name** — your application's registered name (default: "TestEEG")

The Cortex URL (`wss://localhost:6868`) is the local Emotiv service and does not need
changing on a standard Windows install.
