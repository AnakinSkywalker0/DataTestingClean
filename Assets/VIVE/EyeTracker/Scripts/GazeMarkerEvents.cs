using System;

namespace VIVE.OpenXR.Samples.EyeTracker
{
    public static class GazeMarkerEvents
    {
        public static event Action<string, int> OnGazeMarker;

        public static void Invoke(string label, int value)
        {
            OnGazeMarker?.Invoke(label, value);
        }
    }
}
