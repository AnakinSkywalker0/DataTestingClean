using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EmotivUnityPlugin;
using VIVE.OpenXR.Samples.EyeTracker;

public class EEGManager : MonoBehaviour
{
    public static EEGManager Instance { get; private set; }

    [SerializeField] private string clientId = "";
    [SerializeField] private string clientSecret = "";
    [SerializeField] private string appName = "TestEEG";
    [SerializeField] private string cortexUrl = "wss://localhost:6868";

    public bool IsSessionReady { get; private set; }
    public string LastSessionTitle { get; private set; } = "";

    private ConnectToCortexStates _lastCortexState;
    private string _lastMessageLog = "";
    private bool _sessionLoggedOnce;
    private string _pendingExportUuid = "";
    private bool _dataProcessingFinished;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        GazeMarkerEvents.OnGazeMarker += HandleGazeMarker;
    }

    private void OnDestroy()
    {
        GazeMarkerEvents.OnGazeMarker -= HandleGazeMarker;
    }

    private void HandleGazeMarker(string label, int value)
    {
        if (!IsSessionReady || !EmotivUnityItf.Instance.IsRecording) return;
        InjectInstanceMarker(label, value);
    }

    private void Start()
    {
        Debug.Log($"[EEGManager] Starting — clientId={clientId}, url={cortexUrl}");
        EmotivUnityItf.Instance.Init(clientId, clientSecret, appName, true, true, cortexUrl);
        EmotivUnityItf.Instance.Start();
        _lastCortexState = EmotivUnityItf.Instance.GetConnectToCortexState();
        Debug.Log($"[EEGManager] Init + Start done. Initial state: {_lastCortexState}");
        StartCoroutine(AuthAndConnectRoutine());
    }

    private void Update()
    {
        var state = EmotivUnityItf.Instance.GetConnectToCortexState();
        if (state != _lastCortexState)
        {
            Debug.Log($"[EEGManager] Cortex state: {_lastCortexState} → {state}");
            _lastCortexState = state;
        }

        var msg = EmotivUnityItf.Instance.MessageLog;
        if (!string.IsNullOrEmpty(msg) && msg != _lastMessageLog)
        {
            Debug.Log($"[EEGManager] {msg}");
            _lastMessageLog = msg;

            if (msg.StartsWith("Data post processing finished"))
            {
                Debug.Log("[EEGManager] WarningCode 30 detected — post-processing complete.");
                _dataProcessingFinished = true;
            }
        }

        if (EmotivUnityItf.Instance.IsSessionCreated && !_sessionLoggedOnce)
        {
            _sessionLoggedOnce = true;
            IsSessionReady = true;
            Debug.Log($"[EEGManager] Session ready. Headset: {EmotivUnityItf.Instance.WorkingHeadsetId}");
            StartCoroutine(EEGDiagnosticRoutine());
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            StartRecording();
            Debug.Log("Recording started via S key");
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            StopRecording();
            Debug.Log("Recording stopped via E key");
        }
    }

    public void StartRecording()
    {
        if (!IsSessionReady)
        {
            Debug.LogWarning("[EEGManager] StartRecording: session not ready.");
            return;
        }
        if (EmotivUnityItf.Instance.IsRecording)
        {
            Debug.LogWarning("[EEGManager] StartRecording: already recording.");
            return;
        }

        FindObjectOfType<VIVE.OpenXR.Samples.EyeTracker.ObjectDetectedByEyeGazeValue>()?.ResetGazeEpisode();

        string title = GetSessionTitle();
        Debug.Log($"[EEGManager] StartRecording: {title}");
        EmotivUnityItf.Instance.StartRecord(title);
        FindObjectOfType<PolarHRLogger>()?.StartLogging(title);
    }

    private string GetSessionTitle()
    {
        int count = PlayerPrefs.GetInt("SessionCount", 0) + 1;
        PlayerPrefs.SetInt("SessionCount", count);
        PlayerPrefs.Save();
        LastSessionTitle = $"Session_{count:D3}_{DateTime.Now:ddMMMyyyy}";
        return LastSessionTitle;
    }

    public void InjectInstanceMarker(string label, int value)
    {
        if (!EmotivUnityItf.Instance.IsRecording)
        {
            Debug.LogWarning("[EEGManager] InjectInstanceMarker: not recording.");
            return;
        }
        Debug.Log($"[EEGManager] Marker injected — label={label}, value={value}");
        EmotivUnityItf.Instance.InjectMarker(label, value.ToString());
    }

    public void StopRecording()
    {
        if (!EmotivUnityItf.Instance.IsRecording)
        {
            Debug.LogWarning("[EEGManager] StopRecording: not recording.");
            return;
        }
        Debug.Log("[EEGManager] StopRecording called.");
        FindObjectOfType<PolarHRLogger>()?.StopLogging();
        _dataProcessingFinished = false;
        EmotivUnityItf.Instance.StopRecord();
        StartCoroutine(StopAndExportRoutine());
    }

    private IEnumerator StopAndExportRoutine()
    {
        Debug.Log($"[EEGManager] StopAndExportRoutine entry. Plugin IsRecording={EmotivUnityItf.Instance.IsRecording}");
        float elapsed = 0f;
        while (EmotivUnityItf.Instance.IsRecording && elapsed < 12f)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
            Debug.Log($"[EEGManager] Waiting for stop confirmation... elapsed={elapsed:F1}s, IsRecording={EmotivUnityItf.Instance.IsRecording}");
        }

        _pendingExportUuid = EmotivUnityItf.Instance.RecentRecord?.Uuid ?? "";

        if (EmotivUnityItf.Instance.IsRecording)
            Debug.LogWarning($"[EEGManager] 12 s timeout reached. UUID={_pendingExportUuid}. Proceeding to wait for post-processing.");
        else
            Debug.Log($"[EEGManager] Stop confirmed after {elapsed:F1} s. UUID={_pendingExportUuid}. Waiting for post-processing (WarningCode 30)...");

        float procElapsed = 0f;
        while (!_dataProcessingFinished && procElapsed < 30f)
        {
            yield return new WaitForSeconds(0.5f);
            procElapsed += 0.5f;
            Debug.Log($"[EEGManager] Waiting for post-processing... elapsed={procElapsed:F1}s");
        }

        if (_dataProcessingFinished)
            Debug.Log($"[EEGManager] Post-processing complete after {procElapsed:F1} s. Calling ExportSession.");
        else
            Debug.LogWarning($"[EEGManager] 30 s post-processing timeout. Calling ExportSession anyway.");

        ExportSession();
    }

    private void ExportSession()
    {
        Debug.Log($"[EEGManager] ExportSession: UUID={_pendingExportUuid}");
        if (string.IsNullOrEmpty(_pendingExportUuid))
        {
            Debug.LogError("[EEGManager] ExportSession: UUID is empty. Cannot export.");
            return;
        }
        string exportFolder = SessionPaths.EEG;
        Debug.Log($"[EEGManager] Calling ExportRecord — uuid={_pendingExportUuid}, folder={exportFolder}, streamTypes=[EEG,PM,MOTION], format=CSV, version=V2");
        EmotivUnityItf.Instance.ExportRecord(
            new List<string> { _pendingExportUuid },
            exportFolder,
            new List<string> { "EEG", "PM", "MOTION" },
            "CSV",
            "V2",
            includeMarkerExtraInfos: true
        );
        Debug.Log("[EEGManager] ExportRecord dispatched. Waiting for result via MessageLog...");
    }

    /// <summary>
    /// Runs once after the session activates. Waits a few seconds for the EEG
    /// subscription to populate the buffer, then logs a clear PASS / FAIL so you
    /// can confirm the pro licence EEG scope is working before running participants.
    ///
    /// What to look for in the Console:
    ///   [EEG DIAGNOSTIC] PASS  — 14 channels found, buffer filling  → EEG is live
    ///   [EEG DIAGNOSTIC] FAIL  — 0 channels                         → licence scope "eeg" not active
    ///   [EEG DIAGNOSTIC] WARN  — channels present but buffer empty   → subscribed but no data yet
    /// </summary>
    private IEnumerator EEGDiagnosticRoutine()
    {
        Debug.Log("[EEG DIAGNOSTIC] Session ready — waiting 5 s for EEG buffer to populate...");
        yield return new WaitForSeconds(5f);

        var channels = EmotivUnityItf.Instance.GetEEGChannels();

        if (channels == null || channels.Count == 0)
        {
            Debug.LogError("[EEG DIAGNOSTIC] FAIL — GetEEGChannels() returned 0 channels. " +
                           "The pro licence 'eeg' scope is likely not active on this account, " +
                           "or the session was not activated before subscribing.");
            yield break;
        }

        var firstChan  = channels[0];
        double[] samples = EmotivUnityItf.Instance.GetEEGData(firstChan);
        int bufferSize   = EmotivUnityItf.Instance.GetNumberEEGSamples();
        string chanNames = string.Join(", ", channels);

        if (samples != null && samples.Length > 0)
        {
            Debug.Log($"[EEG DIAGNOSTIC] PASS — {channels.Count} channels live: {chanNames}\n" +
                      $"Buffer size: {bufferSize} samples  |  First channel ({firstChan}): " +
                      $"last sample = {samples[samples.Length - 1]:F4} µV");
        }
        else
        {
            Debug.LogWarning($"[EEG DIAGNOSTIC] WARN — {channels.Count} channels subscribed ({chanNames}) " +
                             "but buffer is empty. Data may still be arriving — wait a few more seconds " +
                             "and check the Console for 'eeg data:' lines from the plugin.");
        }
    }

    private IEnumerator AuthAndConnectRoutine()
    {
        yield return new WaitUntil(() => EmotivUnityItf.Instance.IsAuthorizedOK);
        Debug.Log("[EEGManager] Auth OK. Starting headset discovery and session setup...");

        while (!EmotivUnityItf.Instance.IsSessionCreated)
        {
            Debug.Log("[EEGManager] Querying headsets...");
            EmotivUnityItf.Instance.QueryHeadsets();
            yield return new WaitForSeconds(3f);

            Debug.Log("[EEGManager] Calling StartDataStream(eeg, met, auto-select)...");
            EmotivUnityItf.Instance.StartDataStream(new List<string> { "eeg", "met" }, "");

            for (int i = 0; i < 30 && !EmotivUnityItf.Instance.IsSessionCreated; i++)
            {
                yield return new WaitForSeconds(1f);
            }

            if (!EmotivUnityItf.Instance.IsSessionCreated)
                Debug.LogWarning("[EEGManager] Session not created after 30 s. Retrying cycle...");
        }
    }
}
