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
    bool _awaitingBaseline;
    QuestManager _questManager;
    bool _subscribedToQuest;

    [Header("Debug")]
    public bool enableDebugInput = true;
    public int debugStepAmount = 10;

    void Awake()
    {
        _counter = StepCounterFactory.Create();
        _counter.OnStepsChanged += OnStepsChanged;
        TrySubscribeQuestManager();
    }

    void OnEnable()
    {
        _counter.Start();
        TrySubscribeQuestManager();
    }

    void OnDisable()
    {
        _counter.Stop();
        UnsubscribeQuestManager();
    }

    void OnDestroy()
    {
        if (_counter != null)
            _counter.OnStepsChanged -= OnStepsChanged;
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
            SimulateSteps(debugStepAmount);
        }
        if (kb.backspaceKey.wasPressedThisFrame)
        {
            SaveSystem.ResetGame();
        }
        if (kb.deleteKey.wasPressedThisFrame)
        {
            DateUtil.Clear();
        }
#endif
    }

    void OnStepsChanged(int totalStepsFromSensor)
    {
        _latestTotalSteps = totalStepsFromSensor;
        _hasSeenStepEvent = true;

        if (_awaitingBaseline)
        {
            _lastValue = totalStepsFromSensor;
            _awaitingBaseline = false;
            return;
        }

        if (totalStepsFromSensor < _lastValue)
        {
            _lastValue = totalStepsFromSensor;
            return;
        }

        int delta = totalStepsFromSensor - _lastValue;
        _lastValue = totalStepsFromSensor;

        if (QuestManager.Instance != null)
            QuestManager.Instance.AddSteps(delta);
    }

    void OnQuestSelected(Quest quest)
    {
        if (_hasSeenStepEvent)
        {
            _lastValue = _latestTotalSteps;
            _awaitingBaseline = false;
        }
        else
        {
            _awaitingBaseline = true;
        }
    }

    void TrySubscribeQuestManager()
    {
        if (_subscribedToQuest) return;
        var qm = QuestManager.Instance;
        if (qm == null) return;

        qm.OnQuestSelected += OnQuestSelected;
        _questManager = qm;
        _subscribedToQuest = true;

        if (qm.CurrentQuest != null)
            OnQuestSelected(qm.CurrentQuest);
    }

    void UnsubscribeQuestManager()
    {
        if (!_subscribedToQuest) return;
        if (_questManager != null)
            _questManager.OnQuestSelected -= OnQuestSelected;
        _questManager = null;
        _subscribedToQuest = false;
    }

    void SimulateSteps(int amount)
    {
        Debug.Log($"[DEBUG] Simulating {amount} steps.");
        if (QuestManager.Instance != null)
            QuestManager.Instance.AddSteps(amount);
    }
}