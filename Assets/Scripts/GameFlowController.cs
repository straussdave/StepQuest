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

    enum DialoguePhase { None, Intro, Completed }
    DialoguePhase phase = DialoguePhase.None;

    void Start()
    {
        splashPanel.SetActive(true);
        robotDialoguePanel.SetActive(false);
        gamePanel.SetActive(false);
        chosenQuestPanel.SetActive(false);

        var qm = QuestManager.Instance;
        if (qm != null)
        {
            qm.OnQuestCompleted += OnQuestCompleted;
            qm.OnQuestSelected += OnQuestSelected;
        }
            


        if (typewriter == null) typewriter = GetComponent<TMPTypewriter>();
    }

    void OnDestroy()
    {
        var qm = QuestManager.Instance;
        if (qm != null)
        {
            qm.OnQuestCompleted -= OnQuestCompleted;
            qm.OnQuestSelected -= OnQuestSelected;
        }

    }

    public void OnPlayClicked()
    {
        splashPanel.SetActive(false);
        gamePanel.SetActive(false);
        questChoiceRow.SetActive(false);

        phase = DialoguePhase.Intro;
        robotDialoguePanel.SetActive(true);

        string msg = "Hello pilot! I'm Robo-Jet.\n\nWhat part would you like to retrieve today?";
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
                questChoiceRow.SetActive(true);
                phase = DialoguePhase.None;
                break;
            case DialoguePhase.Completed:
                robotDialoguePanel.SetActive(false);
                gamePanel.SetActive(true);
                phase = DialoguePhase.None;
                break;
        }
    }

    void OnQuestSelected(Quest q)
    {
        if (questChoiceRow != null) questChoiceRow.SetActive(false);
        if (chosenQuestPanel != null) chosenQuestPanel.SetActive(true);
        if (progressBarRoot != null) progressBarRoot.SetActive(true);
    }

    void OnQuestCompleted()
    {
        phase = DialoguePhase.Completed;

        robotDialoguePanel.SetActive(true);

        string msg = "Nice work! You completed your mission.\n\nGreat job keeping your steps going!";
        if (typewriter != null) typewriter.Play(msg);
        else robotDialogueText.text = msg;
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
}
