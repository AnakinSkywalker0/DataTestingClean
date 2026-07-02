using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Plays looping background music (B key) and a randomised distraction audio
/// sequence (D key). Logs EEG markers via EEGManager and writes a CSV to
/// SessionPaths.Events/DistractionAudioEvents.csv.
/// </summary>
public class DistractionAudioTrigger : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Background Music")]
    [SerializeField] AudioSource backgroundSource;
    [SerializeField] AudioClip   backgroundClip;

    [Header("Distraction Audio")]
    [SerializeField] AudioSource distractionSource;
    [SerializeField] AudioClip[] distractionClips;
    [SerializeField] int   playCount    = 3;
    [SerializeField] float minInterval  = 10f;
    [SerializeField] float maxInterval  = 30f;

    // ── State ─────────────────────────────────────────────────────────────────

    bool _sequenceRunning = false;
    bool _clipPlaying     = false;
    float _sessionStart;

    // ── CSV ───────────────────────────────────────────────────────────────────

    const string CsvFileName = "DistractionAudioEvents.csv";
    const string CsvHeader   = "Timestamp,UnixTime,SessionTime_s,EventType,ClipName";

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        _sessionStart = Time.time;
        EnsureCsvExists();
    }

    void Update()
    {
        // Key inputs
        if (Input.GetKeyDown(KeyCode.B)) StartBackground();
        if (Input.GetKeyDown(KeyCode.D)) StartDistractionSequence();

        // Auto-detect clip end
        if (_clipPlaying && distractionSource != null && !distractionSource.isPlaying)
        {
            _clipPlaying = false;
            OnClipEnd(distractionSource.clip ? distractionSource.clip.name : "");
        }
    }

    // ── Background ────────────────────────────────────────────────────────────

    void StartBackground()
    {
        if (backgroundSource == null || backgroundSource.isPlaying) return;

        backgroundSource.clip = backgroundClip;
        backgroundSource.loop = true;
        backgroundSource.Play();

        LogEvent("BACKGROUND_START", "");
        Debug.Log("[DistractionAudioTrigger] Background started.");
    }

    // ── Distraction sequence ──────────────────────────────────────────────────

    void StartDistractionSequence()
    {
        if (_sequenceRunning) return;
        if (distractionClips == null || distractionClips.Length == 0)
        {
            Debug.LogWarning("[DistractionAudioTrigger] No distraction clips assigned.");
            return;
        }
        StartCoroutine(DistractionSequence());
    }

    IEnumerator DistractionSequence()
    {
        _sequenceRunning = true;

        for (int i = 0; i < playCount; i++)
        {
            // Random wait before each clip
            float wait = UnityEngine.Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(wait);

            // Pick a random clip
            AudioClip clip = distractionClips[UnityEngine.Random.Range(0, distractionClips.Length)];

            // Play
            if (distractionSource != null && clip != null)
            {
                distractionSource.clip = clip;
                distractionSource.loop = false;
                distractionSource.Play();
                _clipPlaying = true;

                OnClipStart(clip.name);

                // Wait until the clip finishes (Update will also catch it)
                yield return new WaitUntil(() => !_clipPlaying);
            }
        }

        // Sequence complete
        LogEvent("SEQUENCE_COMPLETE", "");
        Debug.Log("[DistractionAudioTrigger] Distraction sequence complete.");
        _sequenceRunning = false;
    }

    // ── Clip event handlers ───────────────────────────────────────────────────

    void OnClipStart(string clipName)
    {
        InjectMarker("distraction_audio_start");
        LogEvent("AUDIO_START", clipName);
        Debug.Log($"[DistractionAudioTrigger] AUDIO_START: {clipName}");
    }

    void OnClipEnd(string clipName)
    {
        InjectMarker("distraction_audio_end");
        LogEvent("AUDIO_END", clipName);
        Debug.Log($"[DistractionAudioTrigger] AUDIO_END: {clipName}");
    }

    // ── EEG marker ───────────────────────────────────────────────────────────

    void InjectMarker(string label)
    {
        if (EEGManager.Instance != null && EEGManager.Instance.IsSessionReady)
            EEGManager.Instance.InjectInstanceMarker(label, 1);
    }

    // ── CSV logging ───────────────────────────────────────────────────────────

    void EnsureCsvExists()
    {
        try
        {
            string path = CsvPath();
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, CsvHeader + "\n");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[DistractionAudioTrigger] Could not create CSV: {ex.Message}");
        }
    }

    void LogEvent(string eventType, string clipName)
    {
        try
        {
            string timestamp  = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            double unixTime   = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            float  sessionSec = Time.time - _sessionStart;

            string line = $"{timestamp},{unixTime:F3},{sessionSec:F3},{eventType},{clipName}\n";
            File.AppendAllText(CsvPath(), line);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[DistractionAudioTrigger] CSV write failed: {ex.Message}");
        }
    }

    static string CsvPath() => Path.Combine(SessionPaths.Events, CsvFileName);
}
