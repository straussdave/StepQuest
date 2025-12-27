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
    public ShipController shipController;

    [Header("Game UI")]
    public GameObject questChoiceRow;
    public GameObject progressBarRoot;
    public ShipRotateInput shipRotateInput;

    enum DialoguePhase { None, Intro, Completed }
    DialoguePhase phase = DialoguePhase.None;

    void Start()
    {
        splashPanel.SetActive(true);
        robotDialoguePanel.SetActive(false);
        gamePanel.SetActive(false);

        QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
    }

    void OnDestroy()
    {
        QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
    }

    // Play button action
    public void OnPlayClicked()
    {
        splashPanel.SetActive(false);
        gamePanel.SetActive(true);

        phase = DialoguePhase.Intro;
        robotDialoguePanel.SetActive(true);

        robotDialogueText.text =
            "Hello pilot! I'm Robo-Jet.\n\nWhat part would you like to retrieve today?";

        questChoiceRow.SetActive(true);
    }

    // Continue button action
    public void OnDialogueContinue()
    {
        switch (phase)
        {
            case DialoguePhase.Intro:
                robotDialoguePanel.SetActive(false);
                phase = DialoguePhase.None;
                break;

            case DialoguePhase.Completed:
                robotDialoguePanel.SetActive(false);
                phase = DialoguePhase.None;
                break;
        }
    }

    void OnQuestCompleted()
    {
        phase = DialoguePhase.Completed;

        robotDialoguePanel.SetActive(true);
        robotDialogueText.text =
            "Nice work! You completed your mission.\n\nGreat job keeping your steps going!";
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
