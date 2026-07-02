using System;
using System.IO;
using UnityEngine;
using VIVE.OpenXR.Samples.EyeTracker;

/// <summary>
/// Pedestrian distraction — placeholder box representation.
///
/// The box is created automatically at runtime (no model needed).
/// Swap in a real character later by assigning a prefab to the
/// Visual Prefab field — the box is suppressed when one is set.
///
/// Setup (Inspector):
///   • Attach this script to any empty GameObject in the Park scene.
///   • Set Conversation Position to an empty Transform placed ~1.5 m
///     in front of the player's start position, rotated so its forward
///     faces the player.
///   • Drop your ElevenLabs .wav / .mp3 clip into Talking Clip.
///   • Leave the GameObject DISABLED in the scene.
///
/// Keyboard control (via TestInput):
///   D (first press)  →  box walks to conversation spot, clip plays
///   D (second press) →  clip stops, box walks back, deactivates
///
/// Data outputs:
///   • EEG marker  "distraction_start" (value 1)  when clip begins
///   • EEG marker  "distraction_end"   (value 0)  when deactivated
///   • Data/{participant}/Events/DistractionEvents.csv
/// </summary>
public class PedestrianDistraction : MonoBehaviour
{
    public static PedestrianDistraction Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────

    [Header("Movement")]
    [Tooltip("Empty Transform placed ~1.5 m in front of the player, facing them.")]
    [SerializeField] private Transform conversationPosition;

    [Tooltip("Walk speed in m/s.")]
    [SerializeField] private float walkSpeed = 1.2f;

    [Tooltip("Rotation speed while walking.")]
    [SerializeField] private float rotationSpeed = 6f;

    [Tooltip("Distance at which the box is considered arrived.")]
    [SerializeField] private float arrivalThreshold = 0.35f;

    [Header("Box Appearance")]
    [Tooltip("Height of the placeholder box in metres — set to match your participant average.")]
    [SerializeField] private float boxHeight = 1.75f;

    [Tooltip("Optional: assign a real character prefab here to replace the box.")]
    [SerializeField] private GameObject visualPrefab;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("ElevenLabs voice clip. Not looped — plays once then the box stands silently until D is pressed again.")]
    [SerializeField] private AudioClip talkingClip;

    [Header("EEG Markers")]
    [SerializeField] private string startMarkerLabel = "distraction_start";
    [SerializeField] private string endMarkerLabel   = "distraction_end";

    // ── State machine ─────────────────────────────────────────────────────

    private enum DistractionState { Idle, WalkingIn, Talking, WalkingOut }
    private DistractionState _state = DistractionState.Idle;

    /// <summary>True while the pedestrian is walking in, talking, or walking out.</summary>
    public bool IsActive => _state != DistractionState.Idle;

    private Vector3    _spawnPosition;
    private Quaternion _spawnRotation;

    private GameObject _visual; // the box (or prefab instance)

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;

        BuildVisual();
    }

    private void Update()
    {
        switch (_state)
        {
            case DistractionState.WalkingIn:
                if (conversationPosition == null) break;
                if (StepTowards(conversationPosition.position))
                    BeginTalking();
                break;

            case DistractionState.WalkingOut:
                if (StepTowards(_spawnPosition))
                    ReturnComplete();
                break;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void Activate()
    {
        if (_state != DistractionState.Idle)
        {
            Debug.LogWarning("[PedestrianDistraction] Already active — ignoring.");
            return;
        }

        gameObject.SetActive(true);
        transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
        _state = DistractionState.WalkingIn;
        Debug.Log("[PedestrianDistraction] Activated — walking in.");
    }

    public void Deactivate()
    {
        if (_state == DistractionState.Idle) return;

        EndTalking(fireMarker: _state == DistractionState.Talking);
        _state = DistractionState.WalkingOut;
        Debug.Log("[PedestrianDistraction] Deactivating — walking back.");
    }

    // ── Movement ──────────────────────────────────────────────────────────

    private bool StepTowards(Vector3 target)
    {
        Vector3 flat = new Vector3(target.x, transform.position.y, target.z);
        Vector3 dir  = flat - transform.position;

        if (dir.magnitude <= arrivalThreshold)
        {
            transform.position = flat;
            return true;
        }

        transform.rotation  = Quaternion.Slerp(transform.rotation,
                                                Quaternion.LookRotation(dir.normalized),
                                                rotationSpeed * Time.deltaTime);
        transform.position += dir.normalized * (walkSpeed * Time.deltaTime);
        return false;
    }

    // ── Talking ───────────────────────────────────────────────────────────

    private void BeginTalking()
    {
        _state = DistractionState.Talking;

        // Face the player (conversation position forward points at them)
        if (conversationPosition != null)
            transform.rotation = conversationPosition.rotation;

        // Play clip once — ElevenLabs lines aren't looped
        if (audioSource != null && talkingClip != null)
        {
            audioSource.clip = talkingClip;
            audioSource.loop = false;
            audioSource.Play();
        }

        GazeMarkerEvents.Invoke(startMarkerLabel, 1);
        WriteEventRow("distraction_start");
        Debug.Log("[PedestrianDistraction] Arrived — clip playing, EEG marker fired.");
    }

    private void EndTalking(bool fireMarker)
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        if (fireMarker)
        {
            GazeMarkerEvents.Invoke(endMarkerLabel, 0);
            WriteEventRow("distraction_end");
            Debug.Log("[PedestrianDistraction] Talking ended, EEG marker fired.");
        }
    }

    private void ReturnComplete()
    {
        _state = DistractionState.Idle;
        gameObject.SetActive(false);
        Debug.Log("[PedestrianDistraction] Back at spawn — deactivated.");
    }

    // ── Box visual ────────────────────────────────────────────────────────

    private void BuildVisual()
    {
        if (visualPrefab != null)
        {
            _visual = Instantiate(visualPrefab, transform);
            return;
        }

        // Placeholder: a cube scaled to player height with a distinct colour
        _visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _visual.transform.SetParent(transform);
        _visual.transform.localPosition = new Vector3(0f, boxHeight / 2f, 0f); // pivot at feet
        _visual.transform.localScale    = new Vector3(0.5f, boxHeight, 0.3f);  // ~shoulder width, player height, shallow depth
        _visual.transform.localRotation = Quaternion.identity;

        // Remove the collider so it doesn't interfere with raycasts
        Destroy(_visual.GetComponent<BoxCollider>());

        // Bright colour so it's obviously a placeholder in testing
        var rend = _visual.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            rend.material.color = new Color(0.2f, 0.6f, 1f); // light blue
        }

        Debug.Log($"[PedestrianDistraction] Placeholder box created ({boxHeight} m tall).");
    }

    // ── CSV event log ─────────────────────────────────────────────────────

    private void WriteEventRow(string eventType)
    {
        try
        {
            string path  = Path.Combine(SessionPaths.Events, "DistractionEvents.csv");
            bool   isNew = !File.Exists(path);
            using var sw = new StreamWriter(path, append: true);
            if (isNew)
                sw.WriteLine("Timestamp,UnixTime,SessionTime_s,EventType");

            sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}," +
                         $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}," +
                         $"{Time.time:F3},{eventType}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PedestrianDistraction] Log write failed: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        if (_state == DistractionState.Talking)
            EndTalking(fireMarker: true);
    }
}
