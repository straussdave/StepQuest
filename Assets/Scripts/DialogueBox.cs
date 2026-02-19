using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueBox : MonoBehaviour
{
    [Header("UI References (from your hierarchy)")]
    [SerializeField] private GameObject speakerNameRoot;   // "SpeakerName" GameObject
    [SerializeField] private TMP_Text speakerNameText;     // TMP component inside SpeakerName (optional)
    [SerializeField] private GameObject portraitRoot;      // "Portrait" GameObject
    [SerializeField] private Image portraitImage;          // Image component on Portrait (optional)

    void OnEnable()
    {
        // If your QuestManager has events, hook them here.
        // Example:
        // QuestManager.Instance.OnQuestSelected += OnQuestChanged;
        // QuestManager.Instance.OnQuestCompleted += OnQuestChanged;

        Refresh();
    }

    void OnDisable()
    {
        // Unhook events if you hooked them.
        // Example:
        // if (QuestManager.Instance == null) return;
        // QuestManager.Instance.OnQuestSelected -= OnQuestChanged;
        // QuestManager.Instance.OnQuestCompleted -= OnQuestChanged;
    }

    // Call this whenever currentQuest changes
    public void Refresh()
    {
        var qm = QuestManager.Instance;
        var quest = qm != null ? qm.GetCurrentQuest() : null;

        bool show = quest != null && quest.showPortrait;

        if (speakerNameRoot != null) speakerNameRoot.SetActive(show);
        if (portraitRoot != null) portraitRoot.SetActive(show);

        // Optional: fill content if you have it on the quest
        // if (show && speakerNameText != null) speakerNameText.text = quest.speakerName;
        // if (show && portraitImage != null) portraitImage.sprite = quest.portraitSprite;
    }

    // Optional event handler if you wire it up
    private void OnQuestChanged(/*Quest quest*/)
    {
        Refresh();
    }
}
