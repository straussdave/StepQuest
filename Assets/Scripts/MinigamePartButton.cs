using UnityEngine;
using UnityEngine.UI;

public class MinigamePartButton : MonoBehaviour
{
    [Header("Part Info")]
    [SerializeField] private string partName;
    [TextArea(3, 8)]
    [SerializeField] private string partDescription;
    [SerializeField] private int minigameIndex;

    [Header("Description Panel")]
    [SerializeField] private PartDescriptionPanel descriptionPanel;

    [Header("Visuals")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite unlockedSprite;

    [Header("Behavior")]
    [SerializeField] private Button button;
    [SerializeField] private bool disableButtonWhenLocked = false;
    [SerializeField] private string lockedDescription = "This part has not been recovered yet.";

    private void Awake()
    {
        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        Refresh();
    }

    private void OnEnable()
    {
        SaveSystem.OnMinigameUnlocked += HandleMinigameUnlocked;
        Refresh();
    }

    private void OnDisable()
    {
        SaveSystem.OnMinigameUnlocked -= HandleMinigameUnlocked;
    }

    public void ShowPartDescription()
    {
        if (descriptionPanel == null)
        {
            Debug.LogError($"[MinigamePartButton] No description panel assigned on {name}.");
            return;
        }

        bool isUnlocked = SaveSystem.IsMinigameUnlocked(minigameIndex);

        if (!isUnlocked)
        {
            descriptionPanel.Show(partName, lockedDescription);
            return;
        }

        descriptionPanel.Show(partName, partDescription);
    }

    public void Refresh()
    {
        bool isUnlocked = SaveSystem.IsMinigameUnlocked(minigameIndex);

        if (buttonImage != null)
        {
            buttonImage.sprite = isUnlocked ? unlockedSprite : lockedSprite;
        }

        if (button != null && disableButtonWhenLocked)
        {
            button.interactable = isUnlocked;
        }
    }

    private void HandleMinigameUnlocked(int unlockedIndex)
    {
        if (unlockedIndex == minigameIndex)
        {
            Refresh();
        }
    }
}