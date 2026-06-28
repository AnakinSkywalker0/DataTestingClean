using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ScenarioSeq;

// Task picker: each button selects a task and opens the scenario-pattern menu for it.
public class TaskMenuController : MonoBehaviour
{
    [System.Serializable]
    public class TaskButton
    {
        public int taskId;          // 1..4
        public Button button;
        public TMP_Text label;
    }

    [SerializeField] private TaskButton[] taskButtons;
    [SerializeField] private string scenarioMenuScene = "ScenarioMenu";

    private void Start()
    {
        foreach (var tb in taskButtons)
        {
            int id = tb.taskId;                         // capture for closure
            TaskDef def = ScenarioCatalog.GetTask(id);
            if (def != null && tb.label != null) tb.label.text = def.MenuLabel;
            if (tb.button != null) tb.button.onClick.AddListener(() => Select(id));
        }
    }

    private void Select(int taskId)
    {
        ScenarioSelection.SelectedTaskId = taskId;
        SceneManager.LoadScene(scenarioMenuScene);
    }
}
