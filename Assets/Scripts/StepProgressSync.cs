using UnityEngine;

public static class StepProgressSync
{
    private const int UninitializedBaseline = -1;

    public static int SyncFromAndroidTotal(int androidTotalSteps)
    {
        if (androidTotalSteps < 0)
        {
            int savedSteps = PlayerPrefs.GetInt(SaveKeys.ACTIVE_QUEST_STEPS, 0);
            Debug.Log($"[StepProgressSync] Ignoring negative Android total={androidTotalSteps}. Keeping saved progress={savedSteps}.");
            return savedSteps;
        }

        int baseline = PlayerPrefs.GetInt(SaveKeys.STEP_COUNTER_BASELINE, UninitializedBaseline);
        int oldProgress = PlayerPrefs.GetInt(SaveKeys.ACTIVE_QUEST_STEPS, 0);
        int lastAndroidTotal = PlayerPrefs.GetInt(SaveKeys.STEP_COUNTER_LAST_TOTAL, -1);
        string trackingMode = PlayerPrefs.GetString(SaveKeys.STEP_TRACKING_MODE, "");

        if (baseline == UninitializedBaseline && trackingMode != "CounterBased")
        {
            baseline = InitializeBaseline(androidTotalSteps);
        }
        else if (lastAndroidTotal >= 0 && androidTotalSteps < lastAndroidTotal)
        {
            Debug.LogWarning(
                $"[StepProgressSync] Android total decreased. Current={androidTotalSteps}, last={lastAndroidTotal}. " +
                "Rebasing against saved progress so future post-reset steps can continue counting."
            );

            baseline = CalculateBaseline(androidTotalSteps, oldProgress);
            PlayerPrefs.SetInt(SaveKeys.STEP_COUNTER_BASELINE, baseline);
        }

        int progress = androidTotalSteps - baseline;

        if (progress < 0)
            progress = 0;

        // Important: never reduce progress because of sensor reset/reboot/weird value.
        if (progress < oldProgress)
        {
            Debug.LogWarning(
                $"[StepProgressSync] Calculated progress would decrease. " +
                $"androidTotal={androidTotalSteps}, baseline={baseline}, calculated={progress}, saved={oldProgress}. Keeping saved progress."
            );
            progress = oldProgress;
        }

        PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_STEPS, progress);
        PlayerPrefs.SetInt(SaveKeys.STEP_COUNTER_BASELINE, baseline);
        PlayerPrefs.SetInt(SaveKeys.STEP_COUNTER_LAST_TOTAL, androidTotalSteps);
        PlayerPrefs.SetString(SaveKeys.STEP_TRACKING_MODE, "CounterBased");
        PlayerPrefs.Save();

        Debug.Log(
            $"[StepProgressSync] Synced Android total. " +
            $"androidTotal={androidTotalSteps}, baseline={baseline}, savedProgress={progress}, " +
            $"lastAndroidTotal={androidTotalSteps}, mode=CounterBased."
        );

        return progress;
    }

    private static int InitializeBaseline(int androidTotalSteps)
    {
        int existingProgress = PlayerPrefs.GetInt(SaveKeys.ACTIVE_QUEST_STEPS, 0);

        int baseline = CalculateBaseline(androidTotalSteps, existingProgress);

        PlayerPrefs.SetInt(SaveKeys.STEP_COUNTER_BASELINE, baseline);
        PlayerPrefs.SetInt(SaveKeys.STEP_COUNTER_LAST_TOTAL, androidTotalSteps);
        PlayerPrefs.SetString(SaveKeys.STEP_TRACKING_MODE, "CounterBased");
        PlayerPrefs.Save();

        Debug.Log(
            $"[StepProgressSync] Step baseline initialized. " +
            $"androidTotal={androidTotalSteps}, existingProgress={existingProgress}, baseline={baseline}, mode=CounterBased."
        );

        return baseline;
    }

    private static int CalculateBaseline(int androidTotalSteps, int existingProgress)
    {
        return androidTotalSteps - Mathf.Max(0, existingProgress);
    }

    public static void RebasePreservingProgress(int androidTotalSteps, int preservedProgress)
    {
        preservedProgress = Mathf.Max(0, preservedProgress);
        PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_STEPS, preservedProgress);

        if (androidTotalSteps >= 0)
        {
            int baseline = CalculateBaseline(androidTotalSteps, preservedProgress);
            PlayerPrefs.SetInt(SaveKeys.STEP_COUNTER_BASELINE, baseline);
            PlayerPrefs.SetInt(SaveKeys.STEP_COUNTER_LAST_TOTAL, androidTotalSteps);
            PlayerPrefs.SetString(SaveKeys.STEP_TRACKING_MODE, "CounterBased");
        }
        else
        {
            PlayerPrefs.SetInt(SaveKeys.STEP_COUNTER_BASELINE, UninitializedBaseline);
            PlayerPrefs.SetInt(SaveKeys.STEP_COUNTER_LAST_TOTAL, -1);
            PlayerPrefs.SetString(SaveKeys.STEP_TRACKING_MODE, "NeedsBaseline");
        }

        PlayerPrefs.Save();

        Debug.Log(
            $"[StepProgressSync] Rebased while preserving progress. " +
            $"androidTotal={androidTotalSteps}, preservedProgress={preservedProgress}, " +
            $"baseline={PlayerPrefs.GetInt(SaveKeys.STEP_COUNTER_BASELINE, UninitializedBaseline)}, " +
            $"lastAndroidTotal={PlayerPrefs.GetInt(SaveKeys.STEP_COUNTER_LAST_TOTAL, -1)}, " +
            $"mode={PlayerPrefs.GetString(SaveKeys.STEP_TRACKING_MODE, "Unset")}."
        );
    }

    public static void ResetForNewQuest(int androidTotalSteps)
    {
        PlayerPrefs.SetInt(SaveKeys.ACTIVE_QUEST_STEPS, 0);

        if (androidTotalSteps >= 0)
        {
            PlayerPrefs.SetInt(SaveKeys.STEP_COUNTER_BASELINE, androidTotalSteps);
            PlayerPrefs.SetInt(SaveKeys.STEP_COUNTER_LAST_TOTAL, androidTotalSteps);
            PlayerPrefs.SetString(SaveKeys.STEP_TRACKING_MODE, "CounterBased");
        }
        else
        {
            PlayerPrefs.SetInt(SaveKeys.STEP_COUNTER_BASELINE, UninitializedBaseline);
            PlayerPrefs.SetInt(SaveKeys.STEP_COUNTER_LAST_TOTAL, -1);
            PlayerPrefs.SetString(SaveKeys.STEP_TRACKING_MODE, "NeedsBaseline");
        }

        PlayerPrefs.Save();

        Debug.Log(
            $"[StepProgressSync] Reset for new quest. " +
            $"androidTotal={androidTotalSteps}, baseline={PlayerPrefs.GetInt(SaveKeys.STEP_COUNTER_BASELINE, UninitializedBaseline)}, " +
            $"lastAndroidTotal={PlayerPrefs.GetInt(SaveKeys.STEP_COUNTER_LAST_TOTAL, -1)}, " +
            $"mode={PlayerPrefs.GetString(SaveKeys.STEP_TRACKING_MODE, "Unset")}."
        );
    }

    public static void ClearWhenNoActiveQuest()
    {
        PlayerPrefs.SetInt(SaveKeys.STEP_COUNTER_BASELINE, UninitializedBaseline);
        PlayerPrefs.SetString(SaveKeys.STEP_TRACKING_MODE, "Inactive");
        PlayerPrefs.Save();

        Debug.Log(
            $"[StepProgressSync] Cleared active quest tracking. " +
            $"baseline=-1, lastAndroidTotal={PlayerPrefs.GetInt(SaveKeys.STEP_COUNTER_LAST_TOTAL, -1)}, mode=Inactive."
        );
    }
}
