using StepCounter;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class QuestStepListener : MonoBehaviour
{
    IStepCounter _counter;
    int _lastValue;
    int _latestTotalSteps;
    bool _hasSeenStepEvent;
    bool _loggedIncrementalFallback;
    QuestManager _questManager;
    bool _subscribedToQuest;

    [Header("Debug")]
    public bool enableDebugInput = true;
    public int debugStepAmount = 10;

    void Awake()
    {
        _counter = StepCounterFactory.Create();
        _counter.OnStepsChanged += OnStepsChanged;
        _counter.OnRawCumulativeStepsChanged += OnRawCumulativeStepsChanged;
        TrySubscribeQuestManager();
    }

    void OnEnable()
    {
        TrySubscribeQuestManager();
        ResumeCounterAndSync("OnEnable");

        if (_counter != null && !_counter.IsAvailable)
        {
            AnalyticsLogger.Instance?.LogStepSensorUnavailable();
        }
    }

    void OnDisable()
    {
        _counter.Stop();
        UnsubscribeQuestManager();
    }

    void OnDestroy()
    {
        if (_counter != null)
        {
            _counter.OnStepsChanged -= OnStepsChanged;
            _counter.OnRawCumulativeStepsChanged -= OnRawCumulativeStepsChanged;
        }

        UnsubscribeQuestManager();
    }

    void Update()
    {
        TrySubscribeQuestManager();
#if ENABLE_INPUT_SYSTEM
        if (!enableDebugInput) return;

        var kb = Keyboard.current;
        if (kb == null) return; // no keyboard attached

        if (kb.spaceKey.wasPressedThisFrame)
        {
            Debug.Log($"[UserAction] Debug add-steps key pressed (space): {debugStepAmount}.");
            SimulateSteps(debugStepAmount);
        }
        if (kb.backspaceKey.wasPressedThisFrame)
        {
            Debug.Log("[UserAction] Debug reset key pressed (backspace).");
            SaveSystem.ResetGame();
        }
        if (kb.deleteKey.wasPressedThisFrame)
        {
            Debug.Log("[UserAction] Debug clear-day key pressed (delete).");
            DateUtil.Clear();
        }
#endif
    }

    void OnApplicationFocus(bool focus)
    {
        if (focus)
            ResumeCounterAndSync("OnApplicationFocus");
    }

    void OnApplicationPause(bool paused)
    {
        if (!paused)
            ResumeCounterAndSync("OnApplicationPause");
    }

    void OnStepsChanged(int totalStepsFromSensor)
    {
        if (_counter != null && _counter.HasCurrentTotalSteps)
        {
            Debug.Log($"[StepTracking] Ignoring compatibility step event because cumulative counter is active. sessionTotal={totalStepsFromSensor}.");
            return;
        }

        if (!_loggedIncrementalFallback)
        {
            Debug.LogWarning("[StepTracking] Reliable cumulative catch-up unavailable. Using incremental step events only.");
            _loggedIncrementalFallback = true;
        }

        _latestTotalSteps = totalStepsFromSensor;
        _hasSeenStepEvent = true;

        if (totalStepsFromSensor < _lastValue)
        {
            _lastValue = totalStepsFromSensor;
            return;
        }

        int delta = totalStepsFromSensor - _lastValue;
        _lastValue = totalStepsFromSensor;

        Debug.Log($"[StepTracking] Incremental sensor update: total={totalStepsFromSensor}, delta={delta}.");
        if (QuestManager.Instance != null)
            QuestManager.Instance.AddSteps(delta);
    }

    void OnRawCumulativeStepsChanged(int rawCumulativeSteps)
    {
        AnalyticsLogger.Instance?.LogStepCounterSnapshot(rawCumulativeSteps);
        SyncFromAndroidTotal(rawCumulativeSteps, "RawCumulativeEvent");
    }

    void OnQuestSelected(Quest quest)
    {
        int currentSavedSteps = PlayerPrefs.GetInt(SaveKeys.ACTIVE_QUEST_STEPS, 0);
        string trackingMode = PlayerPrefs.GetString(SaveKeys.STEP_TRACKING_MODE, "");

        if (currentSavedSteps == 0 && trackingMode == "NeedsBaseline")
        {
            int androidTotalSteps = (_counter != null && _counter.HasCurrentTotalSteps)
                ? _counter.CurrentTotalSteps
                : -1;

            StepProgressSync.ResetForNewQuest(androidTotalSteps);
            QuestManager.Instance?.SetCurrentStepsFromSave(0);
        }

        _lastValue = _hasSeenStepEvent ? _latestTotalSteps : 0;
    }

    void TrySubscribeQuestManager()
    {
        if (_subscribedToQuest) return;
        var qm = QuestManager.Instance;
        if (qm == null) return;

        qm.OnQuestSelected += OnQuestSelected;
        _questManager = qm;
        _subscribedToQuest = true;

        if (qm.GetCurrentQuest() != null)
            SyncCurrentTotalIfAvailable("QuestManagerSubscription");
    }

    void UnsubscribeQuestManager()
    {
        if (!_subscribedToQuest) return;
        if (_questManager != null)
            _questManager.OnQuestSelected -= OnQuestSelected;
        _questManager = null;
        _subscribedToQuest = false;
    }

    public void SimulateSteps(int amount)
    {
        Debug.Log($"[DEBUG] Simulating {amount} steps.");
        if (QuestManager.Instance != null)
            QuestManager.Instance.AddSteps(amount);
    }

    void ResumeCounterAndSync(string reason)
    {
        if (_counter == null)
            return;

        Debug.Log($"[StepTracking] Resume/start sync. reason={reason}, hasCurrentTotal={_counter.HasCurrentTotalSteps}, currentTotal={_counter.CurrentTotalSteps}.");

        _counter.Start();
        SyncCurrentTotalIfAvailable(reason);
    }

    void SyncCurrentTotalIfAvailable(string reason)
    {
        if (_counter == null || !_counter.HasCurrentTotalSteps)
        {
            Debug.Log($"[StepTracking] No cumulative Android total available for sync. reason={reason}.");
            return;
        }

        SyncFromAndroidTotal(_counter.CurrentTotalSteps, reason);
    }

    void SyncFromAndroidTotal(int rawCumulativeSteps, string reason)
    {
        var qm = QuestManager.Instance;
        if (qm == null)
        {
            Debug.Log($"[StepTracking] Skipping cumulative sync because QuestManager is not ready. reason={reason}, androidTotal={rawCumulativeSteps}.");
            return;
        }

        if (qm.GetCurrentQuest() == null || PlayerPrefs.GetInt(SaveKeys.ACTIVE_QUEST_IS_ACTIVE, 0) != 1)
        {
            Debug.Log($"[StepTracking] Skipping cumulative sync because no quest is active. reason={reason}, androidTotal={rawCumulativeSteps}.");
            return;
        }

        if (qm.QuestDoneToday())
        {
            Debug.Log($"[StepTracking] Skipping cumulative sync because today's quest is already done. reason={reason}, androidTotal={rawCumulativeSteps}.");
            return;
        }

        int syncedSteps = StepProgressSync.SyncFromAndroidTotal(rawCumulativeSteps);
        qm.SetCurrentStepsFromSave(syncedSteps);
    }
}
