using UnityEngine;
using VIVE.OpenXR.EyeTracker;

namespace VIVE.OpenXR.Samples.EyeTracker
{
    // Logs whether eye tracking is active at scene start and on first data poll.
    public class EyeTrackingStatusLogger : MonoBehaviour
    {
        private bool _statusLogged = false;

        private void Start()
        {
            Debug.Log($"[EyeTracking][{gameObject.scene.name}] Status check started.");
        }

        private void Update()
        {
            if (_statusLogged) return;

            XR_HTC_eye_tracker.Interop.GetEyeGazeData(out XrSingleEyeGazeDataHTC[] gazes);

            if (gazes == null || gazes.Length == 0)
            {
                Debug.LogWarning($"[EyeTracking][{gameObject.scene.name}] DISABLED — no gaze data returned from XR runtime.");
            }
            else
            {
                bool valid = gazes[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC].isValid;
                Debug.Log($"[EyeTracking][{gameObject.scene.name}] ENABLED — left eye valid: {valid}");
            }

            _statusLogged = true;
        }
    }
}
