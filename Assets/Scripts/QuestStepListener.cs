using StepCounter;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class QuestStepListener : MonoBehaviour
{
    IStepCounter _counter;
    int _lastValue;

    [Header("Debug")]
    public bool enableDebugInput = true;
    public int debugStepAmount = 10;

    void Awake()
    {
        _counter = StepCounterFactory.Create();
        _counter.OnStepsChanged += OnStepsChanged;
    }

    void OnEnable() => _counter.Start();
    void OnDisable() => _counter.Stop();

    void OnDestroy()
    {
        if (_counter != null)
            _counter.OnStepsChanged -= OnStepsChanged;
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (!enableDebugInput) return;

        var kb = Keyboard.current;
        if (kb == null) return; // no keyboard attached

        if (kb.spaceKey.wasPressedThisFrame)
        {
            SimulateSteps(debugStepAmount);
        }
#endif
    }

    void OnStepsChanged(int totalStepsFromSensor)
    {
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

    void SimulateSteps(int amount)
    {
        Debug.Log($"[DEBUG] Simulating {amount} steps.");
        if (QuestManager.Instance != null)
            QuestManager.Instance.AddSteps(amount);
    }
}
