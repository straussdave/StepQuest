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

    enum DialoguePhase { None, Intro, Completed, Quest }
    DialoguePhase phase = DialoguePhase.None;

    void Start()
    {
        splashPanel.SetActive(true);
        robotDialoguePanel.SetActive(false);
        gamePanel.SetActive(false);
        chosenQuestPanel.SetActive(false);
        questChoiceRow.SetActive(false);

        var qm = QuestManager.Instance;
        if (qm != null)
        {
            qm.OnQuestCompleted +=HandleQuestCompleted;
            qm.OnQuestSelected += HandleQuestSelected;
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
        }

    }

    public void OnPlayClicked()
    {
        splashPanel.SetActive(false);
        gamePanel.SetActive(false);
        SetQuestChoiceRow(false, "PlayClicked -> hide choices");

        phase = DialoguePhase.Intro;
        robotDialoguePanel.SetActive(true);

        string msg = "Good morning pilot. \n\nAfter repairing the scanning module I can now locate parts that are further away. Which of these parts would you like to retrieve today?";
        if (typewriter != null) typewriter.Play(msg);
        else robotDialogueText.text = msg;

    }

    public void OnDialogueContinue()
    {
        // First click: finish typing. Second click: advance/close.
        if (typewriter != null && typewriter.IsTyping)
        {
            typewriter.Skip();
            return;
        }

        switch (phase)
        {
            case DialoguePhase.Intro:
                robotDialoguePanel.SetActive(false);
                gamePanel.SetActive(true);
                SetQuestChoiceRow(true, "DialogueContinue Intro -> show choices");
                phase = DialoguePhase.None;
                break;
            case DialoguePhase.Completed:
                robotDialoguePanel.SetActive(false);
                gamePanel.SetActive(true);
                phase = DialoguePhase.None;
                break;
            case DialoguePhase.Quest:
                robotDialoguePanel.SetActive(false);
                SetQuestChoiceRow(false, "DialogueContinue Quest -> hide choices");
                gamePanel.SetActive(true);
                phase = DialoguePhase.None;
                break;
        }
    }

    public void OnOpenCollectionScreen()
    {
        collectionScreenPanel.SetActive(true);
        gamePanel.SetActive(false);

        shipController.ReloadParts();

        if (shipRotateInput != null) shipRotateInput.enabled = true;
    }

    public void OnCloseCollectionScreen()
    {
        collectionScreenPanel.SetActive(false);
        gamePanel.SetActive(true);

        if (shipRotateInput != null) shipRotateInput.enabled = false;
    }

    void HandleQuestSelected(Quest q)
    {
        // show quest-specific "chosen" dialogue
        phase = DialoguePhase.Quest;
        robotDialoguePanel.SetActive(true);
        gamePanel.SetActive(false);
        SetQuestChoiceRow(false, "QuestSelected -> hide choices");

        string msg = (q != null && !string.IsNullOrWhiteSpace(q.ChooseText))
            ? q.ChooseText
            : "Alright. Scanner locked. Keep walking.";

        if (typewriter != null) typewriter.Play(msg);
        else robotDialogueText.text = msg;

        chosenQuestPanel.SetActive(true);
    }

    void HandleQuestCompleted(Quest q)
    {
        phase = DialoguePhase.Completed;
        robotDialoguePanel.SetActive(true);
        gamePanel.SetActive(false);

        string msg = (q != null && !string.IsNullOrWhiteSpace(q.CompletedText))
            ? q.CompletedText
            : "Nice work! Mission complete.";

        if (typewriter != null) typewriter.Play(msg);
        else robotDialogueText.text = msg;
    }

    void SetQuestChoiceRow(bool active, string reason)
    {
        if (questChoiceRow == null) return;

        if (questChoiceRow.activeSelf != active)
            Debug.Log($"[UI] questChoiceRow -> {active} (reason: {reason})", questChoiceRow);

        questChoiceRow.SetActive(active);
    }

}
