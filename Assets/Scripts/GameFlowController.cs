using TMPro;
using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject splashPanel;
    public GameObject robotDialoguePanel;
    public GameObject collectionScreenPanel;
    public GameObject gamePanel;
    public TMP_Text robotDialogueText;
    public TMPTypewriter typewriter;
    public ShipController shipController;

    [Header("Game UI")]
    public GameObject questChoiceRow;
    public GameObject progressBarRoot;
    public ShipRotateInput shipRotateInput;
    public GameObject chosenQuestPanel;

    [Header("End Sequence")]
    public GameObject endSequencePopup;
    public TMP_Text endSequencePopupText;
    public LogExportController logExportController;

    ChosenQuestPanel _chosenQuestPanel;
    QuestManager _questManager;

    enum DialoguePhase { None, Intro, Completed, Quest, EndSequence }
    DialoguePhase phase = DialoguePhase.None;

    void Start()
    {
        splashPanel.SetActive(true);
        robotDialoguePanel.SetActive(false);
        gamePanel.SetActive(false);
        chosenQuestPanel.SetActive(false);
        questChoiceRow.SetActive(false);

        if (endSequencePopup != null)
        {
            endSequencePopup.SetActive(false);
        }

            var qm = QuestManager.Instance;

        if (qm != null)
        {
            qm.OnQuestCompleted += HandleQuestCompleted;
            qm.OnQuestSelected += HandleQuestSelected;
            qm.OnAllQuestsCompleted += HandleAllQuestsCompleted;
        }

        if (typewriter == null) typewriter = GetComponent<TMPTypewriter>();
    }

    void OnDestroy()
    {
        var qm = QuestManager.Instance;
        if (qm != null)
        {
            qm.OnQuestCompleted -= HandleQuestCompleted;
            qm.OnQuestSelected -= HandleQuestSelected;
            qm.OnAllQuestsCompleted -= HandleAllQuestsCompleted;
        }
    }

    public void OnPlayClicked()
    {
        Debug.Log("[UserAction] Play button clicked.");
        if (_questManager == null)
        {
            _questManager = QuestManager.Instance;
        }

        if(_questManager.CheckAllQuestsCompleted())
        {
            Debug.Log("[UserAction] All quests already completed. Starting end sequence.");
            HandleAllQuestsCompleted();
            return;
        }

        var isQuestActive = PlayerPrefs.GetInt(SaveKeys.ACTIVE_QUEST_IS_ACTIVE, 0) == 1;
        Debug.Log("is a quest currently active? " + isQuestActive);

        splashPanel.SetActive(false);

        if (isQuestActive)
        {
            gamePanel.SetActive(true);
            chosenQuestPanel.SetActive(true);
            SetQuestChoiceRow(false, "PlayClicked, currently quest is already active -> hide choices");
            return;
        }

        if (_questManager.CanCompleteQuestToday())
        {
            HandleQuestNotDoneToday();
        }
        else
        {
            HandleQuestAlreadyDoneToday();
        }

        gamePanel.SetActive(false);
        SetQuestChoiceRow(false, "PlayClicked -> hide choices");
    }

    private void HandleQuestNotDoneToday()
    {
        phase = DialoguePhase.Intro;
        robotDialoguePanel.SetActive(true);
        BringToFront(robotDialoguePanel);

        if (_questManager == null)
        {
            _questManager = QuestManager.Instance;
        }

        string msg = PlayerPrefs.GetString(
            SaveKeys.NEXT_DAY_TEXT_KEY,
            "*BOOOM CRASH*..... silence..... but you hear a beep from far away buried under the sand, fortunately the triple redundant airbags did their job.. you might just be able to recover your repair droid and make it out alive..."
        );

        WriteMessage(msg);
    }

    private void HandleQuestAlreadyDoneToday()
    {
        phase = DialoguePhase.Completed;
        robotDialoguePanel.SetActive(true);
        BringToFront(robotDialoguePanel);

        string msg = "You've already completed today's mission. Go get some rest while I work on tomorrows route.";
        WriteMessage(msg);
    }

    public void OnDialogueContinue()
    {
        Debug.Log($"[UserAction] Dialogue continue clicked. Current phase: {phase}.");

        // First click: finish typing. Second click: advance/close.
        if (typewriter != null && typewriter.IsTyping)
        {
            Debug.Log("[UserAction] Dialogue typing skipped to full text.");
            typewriter.Skip();
            return;
        }

        switch (phase)
        {
            case DialoguePhase.Intro:
                robotDialoguePanel.SetActive(false);
                gamePanel.SetActive(true);
                chosenQuestPanel.SetActive(false);
                SetQuestChoiceRow(true, "DialogueContinue Intro -> show choices");
                phase = DialoguePhase.None;
                break;

            case DialoguePhase.Completed:
                robotDialoguePanel.SetActive(false);
                gamePanel.SetActive(true);
                chosenQuestPanel.SetActive(true);

                if (_chosenQuestPanel == null)
                {
                    _chosenQuestPanel = chosenQuestPanel.GetComponent<ChosenQuestPanel>();
                }

                _chosenQuestPanel.RefreshData(PlayerPrefs.GetString(SaveKeys.ACTIVE_QUEST_ID));
                phase = DialoguePhase.None;
                break;

            case DialoguePhase.Quest:
                robotDialoguePanel.SetActive(false);
                SetQuestChoiceRow(false, "DialogueContinue Quest -> hide choices");
                gamePanel.SetActive(true);
                chosenQuestPanel.SetActive(true);
                phase = DialoguePhase.None;
                break;

            case DialoguePhase.EndSequence:
                ShowEndPanel();

                phase = DialoguePhase.None;
                break;
        }
    }

    public void OnOpenCollectionScreen()
    {
        Debug.Log("[UserAction] Opened collection screen.");
        collectionScreenPanel.SetActive(true);
        gamePanel.SetActive(false);

        shipController.ReloadParts();
        shipController.PlayPendingUnlockAnimationIfNeeded();

        if (shipRotateInput != null) shipRotateInput.enabled = true;
    }

    public void OnCloseCollectionScreen()
    {
        Debug.Log("[UserAction] Closed collection screen.");
        collectionScreenPanel.SetActive(false);
        gamePanel.SetActive(true);

        if (shipRotateInput != null) shipRotateInput.enabled = false;
    }

    void HandleQuestSelected(Quest q)
    {
        Debug.Log($"[UserAction] Quest selected event handled: {(q != null ? q.Id : "null")}.");

        phase = DialoguePhase.Quest;
        robotDialoguePanel.SetActive(true);
        BringToFront(robotDialoguePanel);
        gamePanel.SetActive(false);
        SetQuestChoiceRow(false, "QuestSelected -> hide choices");

        string msg = (q != null && !string.IsNullOrWhiteSpace(q.ChooseText))
            ? q.ChooseText
            : "Alright. Scanner locked. Keep walking.";

        WriteMessage(msg);

        chosenQuestPanel.SetActive(true);
    }

    void HandleQuestCompleted(Quest q)
    {
        Debug.Log($"[UserAction] Quest completed event handled: {(q != null ? q.Id : "null")}.");

        phase = DialoguePhase.Completed;
        collectionScreenPanel.SetActive(false);
        robotDialoguePanel.SetActive(true);
        BringToFront(robotDialoguePanel);
        gamePanel.SetActive(false);

        string msg = (q != null && !string.IsNullOrWhiteSpace(q.CompletedText))
            ? q.CompletedText
            : "Nice work! Mission complete.";

        WriteMessage(msg);
    }

    void HandleAllQuestsCompleted()
    {
        Debug.Log("[UserAction] All quests completed. Starting end sequence.");

        phase = DialoguePhase.EndSequence;
        splashPanel.SetActive(false);
        gamePanel.SetActive(false);
        SetQuestChoiceRow(false, "AllQuestsCompleted -> hide choices");

        if (progressBarRoot != null) progressBarRoot.SetActive(false);
        if (shipRotateInput != null) shipRotateInput.enabled = false;

        robotDialoguePanel.SetActive(true);

        string msg =
            "All systems are online. The ship is ready to depart. " +
            "Before we leave, I need you to export the mission logs for analysis.";

        WriteMessage(msg);
    }

    public void OnEndPopupExportLogs()
    {
        Debug.Log("[UserAction] End sequence export logs clicked.");

        if (logExportController != null)
        {
            logExportController.ExportLogsToMail_Default();
        }
        else
        {
            Debug.LogWarning("[Logs] No LogExportController assigned on GameFlowController.");
        }
    }

    public void OnEndPopupClose()
    {
        Debug.Log("[UserAction] End sequence popup closed.");

        if (endSequencePopup != null)
            endSequencePopup.SetActive(false);

        // Optional: let player still inspect their collection after finishing
        gamePanel.SetActive(true);
    }

    void SetQuestChoiceRow(bool active, string reason)
    {
        if (questChoiceRow == null) return;

        if (questChoiceRow.activeSelf != active)
            Debug.Log($"[UI] questChoiceRow -> {active} (reason: {reason})", questChoiceRow);

        questChoiceRow.SetActive(active);
    }

    void WriteMessage(string msg)
    {
        if (typewriter != null)
        {
            typewriter.Play(msg);
        }
        else
        {
            robotDialogueText.text = msg;
        }
    }

    void ShowEndPanel()
    {
        robotDialoguePanel.SetActive(false);
        SetQuestChoiceRow(false, "DialogueContinue EndSequence -> hide choices");
        chosenQuestPanel.SetActive(false);
        gamePanel.SetActive(true);
        if (endSequencePopup != null)
        {
            endSequencePopup.SetActive(true);

            if (endSequencePopupText != null)
            {
                endSequencePopupText.text = "Thank you for playing!\r\n\r\nPlease use this button to send the logfiles via E-Mail.";
            }
        }
    }

    void BringToFront(GameObject panel)
    {
        if (panel == null) return;

        // Same parent canvas: move to top in hierarchy
        panel.transform.SetAsLastSibling();

        // Separate canvas: ensure higher render order
        var canvas = panel.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
        }
    }
}