@echo off
cd /d C:\Users\Abhishek\Desktop\DataTestingClean

echo === Switching to new branch ===
git checkout -b feature/vr-locomotion-data-recording 2>nul || git checkout feature/vr-locomotion-data-recording

echo === Staging changes ===
git add Assets/DistractionAudioTrigger.cs
git add Assets/DistractionAudioTrigger.cs.meta
git add Assets/KAT/Script/KatVRDataLogger.cs
git add "Assets/VIVE/EyeTracker/Scripts/ObjectDetectedByEyeGazeValue.cs"
git add Assets/Editor/
git add Assets/Managers/
git add Assets/VIVE/Scripts/

echo === Staging deletions ===
git add -u

echo === Commit ===
git commit -m "Cleanup: remove unused systems, add DistractionAudioTrigger

- Deleted: Audio, AudioFiles, BillBoards, DataSet, Environment, EVP5,
  EasyRoads3D, Indian Street, Indian buildings, NPC, Vehicles_Prefabs,
  Oculus Hands Physics, VIVE/Sumo
- Removed: ScenarioAdvanceButton, ScenarioSequencer, ScenarioSystemBuilder,
  RealtimeVehicleAgent, ProfileDataManeger, NPCCrossing,
  PedestrianDistraction, EnableNetCheckPoint, FadeEffect, fps, TestInput
- Fixed: ObjectDetectedByEyeGazeValue - replaced RealtimeVehicleAgent reads with defaults
- Fixed: KatVRDataLogger - removed FadeEffect dependency
- Added: DistractionAudioTrigger - background + distraction audio with EEG markers and CSV logging"

echo === Pushing to GitHub ===
git push -u origin feature/vr-locomotion-data-recording

echo === Done ===
pause
