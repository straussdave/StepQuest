using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChosenQuestPanel : MonoBehaviour
{
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text stepsText;
    [SerializeField] TMP_Text progressText;
    [SerializeField] RawImage iconImage;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] TMP_Text missionText;
    [SerializeField] GameObject progress;
    [SerializeField] QuestDB quests;

    [SerializeField]
    private const string ActiveMissionText =
        "Mission: Retrieve the part by walking";
    [SerializeField]
    private const string CompletedMissionText =
        "Mission: Get some rest overnight while Rivet works on repairing the ship. Come back tomorrow for your next mission.";

    QuestProgressBar _progress;
    Quest _currentQuest;

    void Awake()
    {
        ResetVisualState();
    }

    void OnEnable()
    {
        if (QuestManager.Instance == null) return;

        QuestManager.Instance.OnQuestSelected += Refresh;
        QuestManager.Instance.OnProgressChanged += RefreshProgress;

        RefreshFromManager();
    }

    void Start()
    {
        // Important: if QuestManager restores save data in Start(),
        // this second refresh catches the final loaded state.
        RefreshFromManager();
    }

    void OnDisable()
    {
        if (QuestManager.Instance == null) return;

        QuestManager.Instance.OnQuestSelected -= Refresh;
        QuestManager.Instance.OnProgressChanged -= RefreshProgress;
    }

    public void RefreshData(string questId)
    {
        if (quests == null) return;

        Debug.Log($"ChosenQuestPanel.RefreshData: questId={questId}");

        if (quests.TryGetById(questId, out Quest quest))
        {
            Refresh(quest);
        }
        else
        {
            Refresh(null);
        }
    }

    void RefreshFromManager()
    {
        if (QuestManager.Instance == null)
        {
            Refresh(null);
            return;
        }

        Quest quest = QuestManager.Instance.GetCurrentQuest();
        Refresh(quest);
    }

    void Refresh(Quest q)
    {
        _currentQuest = q;
        Render();
    }

    void RefreshProgress(int current, int target)
    {
        if (progress != null)
        {
            progress.SetActive(_currentQuest != null);

            if (_progress == null)
                _progress = progress.GetComponent<QuestProgressBar>();

            if (_progress != null && _currentQuest != null)
                _progress.UpdateUI(current, target);
        }

        Render();
    }

    void Render()
    {
        ResetVisualState();

        if (_currentQuest == null)
            return;

        if (titleText != null)
            titleText.text = _currentQuest.PartName;

        if (stepsText != null)
            stepsText.text = $"{_currentQuest.Steps} steps";

        int currentSteps = 0;
        bool isCompleted = false;

        if (QuestManager.Instance != null)
        {
            currentSteps = QuestManager.Instance.GetCurrentSteps();
            isCompleted = QuestManager.Instance.QuestDoneToday();
        }

        // Mission text must always be rebuilt fresh
        if (missionText != null)
            missionText.text = isCompleted ? CompletedMissionText : ActiveMissionText;

        // Story quest = description, normal quest = image
        if (_currentQuest.IsStoryQuest)
        {
            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = _currentQuest.DescriptionText;
            }

            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(false);
                iconImage.texture = null;
            }
        }
        else
        {
            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(false);
                descriptionText.text = "";
            }

            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(true);
                iconImage.texture = _currentQuest.PartTexture;
            }
        }

        if (progress != null)
        {
            progress.SetActive(true);

            if (_progress == null)
                _progress = progress.GetComponent<QuestProgressBar>();

            if (_progress != null)
                _progress.UpdateUI(currentSteps, _currentQuest.Steps);
        }
    }

    void ResetVisualState()
    {
        if (titleText != null)
            titleText.text = "";

        if (stepsText != null)
            stepsText.text = "";

        if (progressText != null)
            progressText.text = "";

        if (descriptionText != null)
        {
            descriptionText.text = "";
            descriptionText.gameObject.SetActive(false);
        }

        if (iconImage != null)
        {
            iconImage.texture = null;
            iconImage.gameObject.SetActive(false);
        }

        if (progress != null)
            progress.SetActive(false);
    }
}