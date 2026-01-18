using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChosenQuestPanel : MonoBehaviour
{
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text stepsText;
    [SerializeField] TMP_Text progressText;
    [SerializeField] RawImage iconImage;
    [SerializeField] Slider progressSlider; // optional
    [SerializeField] GameObject noQuestState; // optional
    [SerializeField] GameObject questState;   // optional

    void OnEnable()
    {
        if (QuestManager.Instance == null) return;

        QuestManager.Instance.OnQuestSelected += Refresh;
        QuestManager.Instance.OnProgressChanged += RefreshProgress;

        // If a quest is already selected, render immediately
        Refresh(QuestManager.Instance.CurrentQuest);
        if (QuestManager.Instance.CurrentQuest != null)
            RefreshProgress(QuestManager.Instance.CurrentSteps, QuestManager.Instance.CurrentQuest.Steps);
    }

    void OnDisable()
    {
        if (QuestManager.Instance == null) return;

        QuestManager.Instance.OnQuestSelected -= Refresh;
        QuestManager.Instance.OnProgressChanged -= RefreshProgress;
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
        if (progressText) progressText.text = $"{current} / {target}";
        if (progressSlider)
        {
            progressSlider.maxValue = target;
            progressSlider.value = current;
        }
    }
}
