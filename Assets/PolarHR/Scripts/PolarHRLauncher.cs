using System.Diagnostics;
using UnityEngine;

// Launches polar_hr_bridge.py as a subprocess and kills it on quit.
public class PolarHRLauncher : MonoBehaviour
{
    public static PolarHRLauncher Instance { get; private set; }

    [SerializeField] private string pythonExecutable = @"D:\Python 3.10\python.exe";

    private Process _bridgeProcess;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        string scriptPath = System.IO.Path.Combine(
            Application.dataPath, "PolarHR", "Python", "polar_hr_bridge.py");

        var info = new ProcessStartInfo
        {
            FileName = pythonExecutable,
            Arguments = $"\"{scriptPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = false,
            CreateNoWindow = false
        };

        try
        {
            _bridgeProcess = Process.Start(info);
            UnityEngine.Debug.Log($"[PolarHRLauncher] Started bridge PID={_bridgeProcess?.Id}");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"[PolarHRLauncher] Failed to start bridge. Check 'Python Executable' path in Inspector.\nPath used: {pythonExecutable}\nError: {ex.Message}");
        }
    }

    private void OnApplicationQuit()
    {
        if (_bridgeProcess != null && !_bridgeProcess.HasExited)
        {
            _bridgeProcess.Kill();
            UnityEngine.Debug.Log("[PolarHRLauncher] Bridge process killed.");
        }
    }
}
