using System;
using System.IO;
using UnityEngine;

// Writes heart rate samples to a CSV on Desktop for the duration of an EEG session.
public class PolarHRLogger : MonoBehaviour
{
    private StreamWriter _writer;
    private bool _logging;

    private void OnEnable()  { PolarHRReceiver.OnHRReceived += Write; }
    private void OnDisable() { PolarHRReceiver.OnHRReceived -= Write; }

    public void StartLogging(string sessionTitle)
    {
        if (_logging) StopLogging();

        string path = Path.Combine(SessionPaths.HeartRate, $"{sessionTitle}_HR.csv");
        _writer = new StreamWriter(path, append: false);
        _writer.WriteLine("Timestamp,UnixTime,HeartRate_BPM");
        _writer.Flush();
        _logging = true;
        Debug.Log($"[PolarHRLogger] Logging to {path}");
    }

    public void StopLogging()
    {
        if (!_logging) return;
        _logging = false;
        _writer?.Flush();
        _writer?.Close();
        _writer = null;
        Debug.Log("[PolarHRLogger] Logging stopped.");
    }

    private void Write(int bpm)
    {
        if (!_logging || _writer == null) return;
        string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        long unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _writer.WriteLine($"{ts},{unix},{bpm}");
    }

    private void OnDestroy() => StopLogging();
}
