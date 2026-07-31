using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveSystem
{
    public static event Action<int> OnMinigameUnlocked;

    public static void ResetGame()
    {
        ResetToFreshStart();
        ReloadFromStartScene();
    }

    public static void ResetToFreshStart()
    {
        PlayerPrefs.SetInt(SaveKeys.SAVE_VERSION, 2);
        PlayerPrefs.SetString(SaveKeys.ACTIVE_QUEST_ID, "");
        PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_STEPS, 0);
        PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_IS_ACTIVE, 0);
        PlayerPrefs.SetInt(SaveKeys.STEP_COUNTER_BASELINE, -1);
        PlayerPrefs.SetInt(SaveKeys.STEP_COUNTER_LAST_TOTAL, -1);
        PlayerPrefs.SetString(SaveKeys.STEP_TRACKING_MODE, "Inactive");
        PlayerPrefs.SetInt(SaveKeys.QUEST_DONE_TODAY, 0);
        PlayerPrefs.SetString(SaveKeys.UNLOCKED_KEY, "");
        PlayerPrefs.DeleteKey(SaveKeys.NEXT_DAY_TEXT_KEY);
        PlayerPrefs.SetInt(SaveKeys.START_DAY_KEY, 0);
        PlayerPrefs.SetInt(SaveKeys.LAST_QUEST_DAY_KEY, 0);
        PlayerPrefs.SetString(SaveKeys.LAST_UNLOCKED_PART_ID, "");
        PlayerPrefs.SetInt(SaveKeys.PENDING_COLLECTION_HIGHLIGHT, 0);
        PlayerPrefs.SetInt(SaveKeys.PENDING_QUEST_COMPLETION_DIALOGUE, 0);
        PlayerPrefs.SetInt(SaveKeys.HAS_ROTATED_SHIP, 0);
        PlayerPrefs.SetInt(SaveKeys.MINIGAME_1_UNLOCKED, 0);
        PlayerPrefs.SetInt(SaveKeys.MINIGAME_2_UNLOCKED, 0);
        PlayerPrefs.SetInt(SaveKeys.MINIGAME_3_UNLOCKED, 0);
        PlayerPrefs.SetInt(SaveKeys.MINIGAME_4_UNLOCKED, 0);
        PlayerPrefs.Save();
    }

    public static void ReloadFromStartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void UnlockMinigameForQuest(string questId)
    {
        int minigameIndex = QuestMinigameMap.GetMinigameIndexForQuest(questId);

        switch (minigameIndex)
        {
            case 0:
                PlayerPrefs.SetInt(SaveKeys.MINIGAME_1_UNLOCKED, 1);
                break;
            case 1:
                PlayerPrefs.SetInt(SaveKeys.MINIGAME_2_UNLOCKED, 1);
                break;
            case 2:
                PlayerPrefs.SetInt(SaveKeys.MINIGAME_3_UNLOCKED, 1);
                break;
            case 3:
                PlayerPrefs.SetInt(SaveKeys.MINIGAME_4_UNLOCKED, 1);
                break;
            default:
                Debug.LogWarning($"[SaveSystem] No minigame mapped for questId '{questId}'.");
                return;
        }

        PlayerPrefs.Save();
        OnMinigameUnlocked?.Invoke(minigameIndex);
    }

    public static bool IsMinigameUnlocked(int index)
    {
        switch (index)
        {
            case 0: return PlayerPrefs.GetInt(SaveKeys.MINIGAME_1_UNLOCKED, 0) == 1;
            case 1: return PlayerPrefs.GetInt(SaveKeys.MINIGAME_2_UNLOCKED, 0) == 1;
            case 2: return PlayerPrefs.GetInt(SaveKeys.MINIGAME_3_UNLOCKED, 0) == 1;
            case 3: return PlayerPrefs.GetInt(SaveKeys.MINIGAME_4_UNLOCKED, 0) == 1;
            default: return false;
        }
    }
}

public static class SaveKeys
{
    public const string SAVE_VERSION = "SAVE_VERSION";
    public const string ACTIVE_QUEST_ID = "active_quest_id";
    public const string ACTIVE_QUEST_STEPS = "active_quest_steps";
    public const string ACTIVE_QUEST_IS_ACTIVE = "active_quest_is_active";
    public const string STEP_COUNTER_BASELINE = "STEP_COUNTER_BASELINE";
    public const string STEP_COUNTER_LAST_TOTAL = "STEP_COUNTER_LAST_TOTAL";
    public const string STEP_TRACKING_MODE = "STEP_TRACKING_MODE";
    public const string QUEST_DONE_TODAY = "QUEST_DONE";
    public const string UNLOCKED_KEY = "UNLOCKED_PART_IDS";
    public const string NEXT_DAY_TEXT_KEY = "NEXT_DAY_TEXT";
    public const string START_DAY_KEY = "START_DAY_KEY";
    public const string LAST_QUEST_DAY_KEY = "LAST_QUEST_DAY_KEY";
    public const string LAST_UNLOCKED_PART_ID = "LAST_UNLOCKED_PART_ID";
    public const string PENDING_COLLECTION_HIGHLIGHT = "PENDING_COLLECTION_HIGHLIGHT";
    public const string PENDING_QUEST_COMPLETION_DIALOGUE = "PENDING_QUEST_COMPLETION_DIALOGUE";
    public const string HAS_ROTATED_SHIP = "HAS_ROTATED_SHIP";
    public const string MINIGAME_1_UNLOCKED = "MINIGAME_1_UNLOCKED";
    public const string MINIGAME_2_UNLOCKED = "MINIGAME_2_UNLOCKED";
    public const string MINIGAME_3_UNLOCKED = "MINIGAME_3_UNLOCKED";
    public const string MINIGAME_4_UNLOCKED = "MINIGAME_4_UNLOCKED";

}
