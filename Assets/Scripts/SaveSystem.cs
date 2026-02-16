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
        DeleteKey(SaveKeys.CURRENT_QUEST_KEY);
        DeleteKey(SaveKeys.NEXT_DAY_TEXT_KEY);
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
    public const string CURRENT_QUEST_KEY = "CURRENT_QUEST_ID";
    public const string UNLOCKED_KEY = "UNLOCKED_PART_IDS";
    public const string NEXT_DAY_TEXT_KEY = "NEXT_DAY_TEXT";
}