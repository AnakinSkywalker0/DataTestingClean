using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ScenarioSeq;

// Scenario-pattern builder for the SELECTED task: click S1..S4 to set their run order, then Start.
public class ScenarioMenuController : MonoBehaviour
{
    [System.Serializable]
    public class ScenarioCard
    {
        public int index;            // 0..3 (S1..S4)
        public Button button;
        public TMP_Text titleText;   // "S1"
        public TMP_Text bodyText;    // factor summary
    }

    [Header("Header")]
    [SerializeField] private TMP_Text headerText;

    [Header("Scenario cards (S1..S4)")]
    [SerializeField] private ScenarioCard[] scenarioCards;

    [Header("Sequence (top)")]
    [SerializeField] private Transform sequenceContainer;
    [SerializeField] private GameObject chipPrefab;   // a small UI element with a TMP_Text child

    [Header("Controls")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button resetButton;

    private TaskDef _task;
    private readonly List<int> _order = new List<int>();
    private readonly List<GameObject> _chips = new List<GameObject>();

    private void Start()
    {
        _task = ScenarioCatalog.GetTask(ScenarioSelection.SelectedTaskId);

        if (headerText != null)
            headerText.text = _task != null
                ? $"Create scenario pattern ({_task.name})"
                : "Create scenario pattern";

        foreach (var card in scenarioCards)
        {
            int idx = card.index;                       // capture for closure
            if (_task != null && idx >= 0 && idx < _task.scenarios.Length)
            {
                if (card.titleText != null) card.titleText.text = $"S{idx + 1}";
                if (card.bodyText != null) card.bodyText.text = _task.scenarios[idx].Summary;
            }
            if (card.button != null)
                card.button.onClick.AddListener(() => AddScenario(idx, card.button));
        }

        if (resetButton != null) resetButton.onClick.AddListener(ResetPattern);
        if (startButton != null) startButton.onClick.AddListener(StartRun);
        RefreshStartInteractable();
    }

    private void AddScenario(int index, Button cardButton)
    {
        if (_order.Contains(index)) return;
        _order.Add(index);
        if (cardButton != null) cardButton.interactable = false;   // placed once

        if (chipPrefab != null && sequenceContainer != null)
        {
            GameObject chip = Instantiate(chipPrefab, sequenceContainer);
            chip.SetActive(true);
            var label = chip.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = $"S{index + 1}";
            _chips.Add(chip);
        }
        RefreshStartInteractable();
    }

    private void ResetPattern()
    {
        _order.Clear();
        foreach (var chip in _chips)
            if (chip != null) Destroy(chip);
        _chips.Clear();
        foreach (var card in scenarioCards)
            if (card.button != null) card.button.interactable = true;
        RefreshStartInteractable();
    }

    private void StartRun()
    {
        if (_task == null) return;
        if (_order.Count < 1) return;
    }

    private void RefreshStartInteractable()
    {
        if (startButton != null)
            startButton.interactable = _order.Count >= 1;
    }
}
