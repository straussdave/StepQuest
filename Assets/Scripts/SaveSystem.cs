using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveSystem
{
    private const char Separator = ';';

    public static void ResetGame()
    {
        ResetToFreshStart();
        ReloadFromStartScene();
    }

    public static void ResetToFreshStart()
    {
        DeleteKey(SaveKeys.UNLOCKED_KEY);
        DeleteKey(SaveKeys.ACTIVE_QUEST_ID);
        DeleteKey(SaveKeys.NEXT_DAY_TEXT_KEY);
        DeleteKey(SaveKeys.ACTIVE_QUEST_IS_ACTIVE);
        DeleteKey(SaveKeys.QUEST_DONE_TODAY);

        DeleteKey(SaveKeys.START_DAY_KEY);
        DeleteKey(SaveKeys.LAST_QUEST_DAY_KEY);

        DeleteKey(SaveKeys.LAST_UNLOCKED_PART_ID);
        DeleteKey(SaveKeys.PENDING_COLLECTION_HIGHLIGHT);

        DeleteKey(SaveKeys.HAS_ROTATED_SHIP);

        DeleteKey(SaveKeys.MINIGAME_1_UNLOCKED);
        DeleteKey(SaveKeys.MINIGAME_2_UNLOCKED);
        DeleteKey(SaveKeys.MINIGAME_3_UNLOCKED);
        DeleteKey(SaveKeys.MINIGAME_4_UNLOCKED);

        PlayerPrefs.Save();
    }

    public static void DeleteKey(string key)
    {
        PlayerPrefs.DeleteKey(key);
    }

    public static void ReloadFromStartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void UnlockMinigameForQuest(string questId)
    {
        switch (questId)
        {
            case "9":
                PlayerPrefs.SetInt(SaveKeys.MINIGAME_1_UNLOCKED, 1);
                break;
            case "10":
                PlayerPrefs.SetInt(SaveKeys.MINIGAME_2_UNLOCKED, 1);
                break;
            case "11":
                PlayerPrefs.SetInt(SaveKeys.MINIGAME_3_UNLOCKED, 1);
                break;
            case "12":
                PlayerPrefs.SetInt(SaveKeys.MINIGAME_4_UNLOCKED, 1);
                break;
        }

        PlayerPrefs.Save();
    }

    public static bool IsMinigameUnlocked(int index)
    {
        return PlayerPrefs.GetInt("MINIGAME_" + index + "_UNLOCKED", 0) == 1;
    }
}

public static class SaveKeys
{
    public const string ACTIVE_QUEST_ID = "active_quest_id";
    public const string ACTIVE_QUEST_STEPS = "active_quest_steps";
    public const string ACTIVE_QUEST_IS_ACTIVE = "active_quest_is_active";
    public const string QUEST_DONE_TODAY = "QUEST_DONE";
    public const string UNLOCKED_KEY = "UNLOCKED_PART_IDS";
    public const string NEXT_DAY_TEXT_KEY = "NEXT_DAY_TEXT";
    public const string START_DAY_KEY = "START_DAY_KEY";
    public const string LAST_QUEST_DAY_KEY = "LAST_QUEST_DAY_KEY";
    public const string LAST_UNLOCKED_PART_ID = "LAST_UNLOCKED_PART_ID";
    public const string PENDING_COLLECTION_HIGHLIGHT = "PENDING_COLLECTION_HIGHLIGHT";
    public const string HAS_ROTATED_SHIP = "HAS_ROTATED_SHIP";
    public const string MINIGAME_1_UNLOCKED = "MINIGAME_1_UNLOCKED";
    public const string MINIGAME_2_UNLOCKED = "MINIGAME_2_UNLOCKED";
    public const string MINIGAME_3_UNLOCKED = "MINIGAME_3_UNLOCKED";
    public const string MINIGAME_4_UNLOCKED = "MINIGAME_4_UNLOCKED";
}