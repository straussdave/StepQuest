using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveSystem
{
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
}

public static class SaveKeys
{
    public const string ACTIVE_QUEST_ID = "active_quest_id";
    public const string ACTIVE_QUEST_STEPS = "active_quest_steps";
    public const string ACTIVE_QUEST_IS_ACTIVE = "active_quest_is_active";
    public const string QUEST_DONE_TODAY = "QUEST_DONE";
    public const string UNLOCKED_KEY = "UNLOCKED_PART_IDS";
    public const string NEXT_DAY_TEXT_KEY = "NEXT_DAY_TEXT";
    public const string START_DAY_KEY = "START_DAY_KEY";          // int YYYYMMDD
    public const string LAST_QUEST_DAY_KEY = "LAST_QUEST_DAY_KEY"; // int YYYYMMDD
}