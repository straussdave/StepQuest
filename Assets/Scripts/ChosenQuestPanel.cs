using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChosenQuestPanel : MonoBehaviour
{
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text stepsText;
    [SerializeField] TMP_Text progressText;
    [SerializeField] RawImage iconImage;
    [SerializeField] GameObject noQuestState; // optional
    [SerializeField] GameObject questState;   // optional
    [SerializeField] GameObject progress;
    [SerializeField] QuestDB quests;

    QuestProgressBar _progress;

    void OnEnable()
    {
        if (QuestManager.Instance == null) return;

        QuestManager.Instance.OnQuestSelected += Refresh;
        QuestManager.Instance.OnProgressChanged += RefreshProgress;

        // If a quest is already selected, render immediately
        Refresh(QuestManager.Instance.GetCurrentQuest());
        if (QuestManager.Instance.GetCurrentQuest() != null)
            RefreshProgress(QuestManager.Instance.GetCurrentSteps(), QuestManager.Instance.GetCurrentQuest().Steps);
    }

    void OnDisable()
    {
        if (QuestManager.Instance == null) return;

        QuestManager.Instance.OnQuestSelected -= Refresh;
        QuestManager.Instance.OnProgressChanged -= RefreshProgress;
    }

    public void RefreshData(string questId)
    {
        Debug.Log($"ChosenQuestPanel.RefreshData: questId={questId}");
        quests.TryGetById(questId, out Quest quest);
        Refresh(quest);
        int steps = QuestManager.Instance.GetCurrentSteps();
        RefreshProgress(steps, quest != null ? quest.Steps : 0);
    }

    void Refresh(Quest q)
    {
        bool hasQuest = q != null;

        if (!hasQuest) return;

        if (titleText) titleText.text = q.PartName;
        if (stepsText) stepsText.text = $"{q.Steps} steps";
        if (iconImage) iconImage.texture = q.PartTexture;
    }

    void RefreshProgress(int current, int target)
    {
        progress.SetActive(true);
        if (_progress == null)
        {
            _progress = progress.GetComponent<QuestProgressBar>();
        }
        _progress.UpdateUI(current, target);
    }
}
