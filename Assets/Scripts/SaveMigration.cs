using UnityEngine;

public static class SaveMigration
{
    private const int CURRENT_SAVE_VERSION = 2;

    public static void Run()
    {
        int version = PlayerPrefs.GetInt(SaveKeys.SAVE_VERSION, 1);

        Debug.Log($"[SaveMigration] Save version={version}, current={CURRENT_SAVE_VERSION}.");

        if (version < 2)
        {
            MigrateToVersion2();
            PlayerPrefs.SetInt(SaveKeys.SAVE_VERSION, CURRENT_SAVE_VERSION);
            PlayerPrefs.Save();
        }

        LogStepTrackingState("After migration check");
    }

    private static void MigrateToVersion2()
    {
        bool hasActiveQuest = PlayerPrefs.GetInt(SaveKeys.ACTIVE_QUEST_IS_ACTIVE, 0) == 1;
        string activeQuestId = PlayerPrefs.GetString(SaveKeys.ACTIVE_QUEST_ID, "");
        int oldProgress = PlayerPrefs.GetInt(SaveKeys.ACTIVE_QUEST_STEPS, 0);

        // New step system starts uninitialized.
        PlayerPrefs.SetInt(SaveKeys.STEP_COUNTER_BASELINE, -1);
        PlayerPrefs.SetInt(SaveKeys.STEP_COUNTER_LAST_TOTAL, -1);

        if (hasActiveQuest)
        {
            // Keep old progress. Do not overwrite ACTIVE_QUEST_STEPS.
            PlayerPrefs.SetString(SaveKeys.STEP_TRACKING_MODE, "NeedsBaseline");
            Debug.Log($"[SaveMigration] Migrated to v2. ActiveQuestId={activeQuestId}, currentSavedQuestProgress={oldProgress}, mode=NeedsBaseline.");
        }
        else
        {
            PlayerPrefs.SetString(SaveKeys.STEP_TRACKING_MODE, "Inactive");
            Debug.Log("[SaveMigration] Migrated to v2. No active quest, mode=Inactive.");
        }
    }

    private static void LogStepTrackingState(string context)
    {
        Debug.Log(
            $"[SaveMigration] {context}: " +
            $"saveVersion={PlayerPrefs.GetInt(SaveKeys.SAVE_VERSION, 1)}, " +
            $"activeQuestId={PlayerPrefs.GetString(SaveKeys.ACTIVE_QUEST_ID, "")}, " +
            $"isActive={PlayerPrefs.GetInt(SaveKeys.ACTIVE_QUEST_IS_ACTIVE, 0)}, " +
            $"currentSavedQuestProgress={PlayerPrefs.GetInt(SaveKeys.ACTIVE_QUEST_STEPS, 0)}, " +
            $"stepBaseline={PlayerPrefs.GetInt(SaveKeys.STEP_COUNTER_BASELINE, -1)}, " +
            $"lastAndroidTotal={PlayerPrefs.GetInt(SaveKeys.STEP_COUNTER_LAST_TOTAL, -1)}, " +
            $"trackingMode={PlayerPrefs.GetString(SaveKeys.STEP_TRACKING_MODE, "Unset")}"
        );
    }
}
