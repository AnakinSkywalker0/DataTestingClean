using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using TMPro;

// Single-screen recording UI: enter participant details, start/stop the biometric recording session.
public class RecordingSessionUI : MonoBehaviour
{
    [Header("Participant Input")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField idInput;

    [Header("Controls")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button stopButton;

    [Header("Status Labels")]
    [SerializeField] private TMP_Text eegStatusText;
    [SerializeField] private TMP_Text hrStatusText;
    [SerializeField] private TMP_Text eyeStatusText;

    [Header("Session")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text sessionLabelText;
    [SerializeField] private TMP_Text messageText;

    private bool _isRecording;
    private float _elapsed;

    private void Start()
    {
        startButton.onClick.AddListener(StartRecording);
        stopButton.onClick.AddListener(StopRecording);

        stopButton.interactable = false;
        timerText.text = "00:00:00";
        sessionLabelText.text = "";
        messageText.text = "";
    }

    private void Update()
    {
        UpdateStatusLabels();

        if (_isRecording)
        {
            _elapsed += Time.deltaTime;
            timerText.text = TimeSpan.FromSeconds(_elapsed).ToString(@"hh\:mm\:ss");
        }
    }

    private void UpdateStatusLabels()
    {
        var eeg = EEGManager.Instance;
        if (eeg != null && eeg.IsSessionReady)
            eegStatusText.text = "EEG: CONNECTED";
        else if (eeg != null && EmotivUnityPlugin.EmotivUnityItf.Instance.IsAuthorizedOK)
            eegStatusText.text = "EEG: CONNECTING";
        else
            eegStatusText.text = "EEG: OFFLINE";

        var hr = PolarHRReceiver.Instance;
        hrStatusText.text = (hr != null && hr.CurrentHR > 0)
            ? $"HR: {hr.CurrentHR} BPM"
            : "HR: OFFLINE";

        eyeStatusText.text = XRSettings.isDeviceActive ? "EYE: ACTIVE" : "EYE: INACTIVE";
    }

    private void StartRecording()
    {
        string name = nameInput.text;
        string id = idInput.text;

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(id))
        {
            messageText.text = "Please enter Name and ID.";
            return;
        }

        StoreData.Name = name;
        StoreData.ID = id;

        EEGManager.Instance.StartRecording();
        sessionLabelText.text = EEGManager.Instance.LastSessionTitle;

        _isRecording = true;
        _elapsed = 0f;
        timerText.text = "00:00:00";
        messageText.text = "";

        startButton.interactable = false;
        stopButton.interactable = true;
    }

    private void StopRecording()
    {
        EEGManager.Instance.StopRecording();

        _isRecording = false;
        messageText.text = "Session saved.";

        startButton.interactable = true;
        stopButton.interactable = false;
    }
}
