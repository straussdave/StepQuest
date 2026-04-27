using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MinigameSceneButton : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string sceneName;
    [SerializeField] private int minigameIndex;

    [Header("Visuals")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite unlockedSprite;

    [Header("Behavior")]
    [SerializeField] private Button button;
    [SerializeField] private bool disableButtonWhenLocked = true;

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

    public void OpenMinigame()
    {
        bool isUnlocked = SaveSystem.IsMinigameUnlocked(minigameIndex);
        if (!isUnlocked)
        {
            Debug.Log($"[Minigame] Minigame {minigameIndex} is locked.");
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"[Minigame] No scene name assigned on {name}.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[Minigame] Scene '{sceneName}' is not in Build Settings.");
            return;
        }

        Debug.Log($"[Minigame] Opening scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
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