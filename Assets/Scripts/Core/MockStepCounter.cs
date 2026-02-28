using UnityEngine;
using StepCounter;

public sealed class MockStepCounter : IStepCounter
{
    public event System.Action<int> OnStepsChanged;
    public bool IsAvailable => true;

    int _steps;
    float _timer;

    StepCounterRunner runner;

    public void Start()
    {
        _steps = 0;
        _timer = 0f;

        runner = StepCounterRunner.Ensure();
        runner.Tick += OnTick;
    }

    public void Stop()
    {
        if (runner != null)
        {
            runner.Tick -= OnTick;
            runner.Cleanup();
            runner = null;
        }
    }

    void OnTick(float dt)
    {
        _timer += dt;
        if (_timer >= 0.5f)
        {
            _timer = 0f;
            _steps += Random.Range(30, 50);
            OnStepsChanged?.Invoke(_steps);
        }
    }

    sealed class StepCounterRunner : MonoBehaviour
    {
        public event System.Action<float> Tick;
        static StepCounterRunner _instance;

        public static StepCounterRunner Ensure()
        {
            if (_instance != null) return _instance;

            var go = new GameObject("~StepCounterRunner");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<StepCounterRunner>();
            return _instance;
        }

        public void Cleanup()
        {
            Tick = null;
            _instance = null;

            if (gameObject != null)
                Destroy(gameObject);
        }

        void Update() => Tick?.Invoke(Time.deltaTime);
    }
}
