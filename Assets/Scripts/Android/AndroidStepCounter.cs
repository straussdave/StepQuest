// Assets/StepCounter/Android/AndroidStepCounter.cs
using UnityEngine;
using UnityEngine.Scripting;

namespace StepCounter.AndroidImpl
{
    [Preserve]
    public sealed class AndroidStepCounter : IStepCounter
    {
        public event System.Action<int> OnStepsChanged;
        public bool IsAvailable => _stepCounter != null || _stepDetector != null;

        AndroidJavaObject _activity, _sensorManager, _stepCounter, _stepDetector;
        SensorListener _listenerCounter, _listenerDetector;
        float? _baseline;    // for TYPE_STEP_COUNTER
        int _sessionSteps;   // for TYPE_STEP_DETECTOR or post-baseline
        bool _registered;

        public AndroidStepCounter()
        {
            Debug.Log("[StepCounter] AndroidStepCounter ctor");

            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            _activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            using var ctx = new AndroidJavaClass("android.content.Context");
            string SENSOR_SERVICE = ctx.GetStatic<string>("SENSOR_SERVICE");
            _sensorManager = _activity.Call<AndroidJavaObject>("getSystemService", SENSOR_SERVICE);

            using var sensorClass = new AndroidJavaClass("android.hardware.Sensor");
            int TYPE_STEP_COUNTER = sensorClass.GetStatic<int>("TYPE_STEP_COUNTER");
            int TYPE_STEP_DETECTOR = sensorClass.GetStatic<int>("TYPE_STEP_DETECTOR");

            _stepCounter = _sensorManager.Call<AndroidJavaObject>("getDefaultSensor", TYPE_STEP_COUNTER);
            if (_stepCounter == null)
                _stepDetector = _sensorManager.Call<AndroidJavaObject>("getDefaultSensor", TYPE_STEP_DETECTOR);

            Debug.Log($"[StepCounter] Available sensors: Counter={_stepCounter != null}, Detector={_stepDetector != null}");
        }

        public void Start()
        {
            Debug.Log($"[StepCounter] Start() called. IsAvailable={IsAvailable}");

            if (!IsAvailable)
            {
                Debug.LogWarning("[StepCounter] No step sensor available.");
                return;
            }

            const string PERM = "android.permission.ACTIVITY_RECOGNITION";
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(PERM))
            {
                Debug.Log("[StepCounter] Requesting ACTIVITY_RECOGNITION permission...");
                var cb = new UnityEngine.Android.PermissionCallbacks();
                cb.PermissionGranted += p =>
                {
                    Debug.Log($"[StepCounter] Permission granted: {p}");
                    if (p == PERM) Register();
                };
                cb.PermissionDenied += p => Debug.LogWarning($"[StepCounter] Permission denied: {p}");
                cb.PermissionDeniedAndDontAskAgain += p => Debug.LogWarning($"[StepCounter] Permission dont-ask-again: {p}");
                UnityEngine.Android.Permission.RequestUserPermission(PERM, cb);
                return; // wait for callback
            }

            Debug.Log("[StepCounter] Permission already granted, registering listener...");
            Register();
        }

        public void Stop()
        {
            Debug.Log("[StepCounter] Stop()");

            if (!_registered) return;

            if (_listenerCounter != null)
            {
                _sensorManager.Call("unregisterListener", _listenerCounter);
                _listenerCounter = null;
            }
            if (_listenerDetector != null)
            {
                _sensorManager.Call("unregisterListener", _listenerDetector);
                _listenerDetector = null;
            }
            _registered = false;
        }

        void Register()
        {
            if (_registered)
            {
                Debug.Log("[StepCounter] Register() called but already registered");
                return;
            }

            if (_stepCounter != null)
            {
                _baseline = null;
                _sessionSteps = 0;
                _listenerCounter = new SensorListener(OnCounterChanged);
                // 3 == SENSOR_DELAY_NORMAL
                bool ok = _sensorManager.Call<bool>("registerListener", _listenerCounter, _stepCounter, 3);
                Debug.Log($"[StepCounter] Register TYPE_STEP_COUNTER -> {ok}");
            }
            else if (_stepDetector != null)
            {
                _sessionSteps = 0;
                _listenerDetector = new SensorListener(OnDetector);
                bool ok = _sensorManager.Call<bool>("registerListener", _listenerDetector, _stepDetector, 3);
                Debug.Log($"[StepCounter] Register TYPE_STEP_DETECTOR -> {ok}");
            }
            else
            {
                Debug.LogWarning("[StepCounter] Register() called but no sensor instance.");
                return;
            }

            _registered = true;
        }

        // TYPE_STEP_COUNTER: cumulative since boot, float
        void OnCounterChanged(float cumulative)
        {
            Debug.Log($"[StepCounter] OnCounterChanged raw cumulative={cumulative}");

            if (!_baseline.HasValue)
            {
                _baseline = cumulative;
                _sessionSteps = 0;
                Debug.Log($"[StepCounter] Baseline set: {cumulative}");
                return;
            }

            _sessionSteps = Mathf.Max(0, Mathf.RoundToInt(cumulative - _baseline.Value));
            Debug.Log($"[StepCounter] Counter event: cumulative={cumulative}, session={_sessionSteps}");

            OnStepsChanged?.Invoke(_sessionSteps);
        }

        // TYPE_STEP_DETECTOR: one event == one step
        void OnDetector(float _)
        {
            _sessionSteps++;
            Debug.Log($"[StepCounter] Detector step -> session={_sessionSteps}");
            OnStepsChanged?.Invoke(_sessionSteps);
        }

        class SensorListener : AndroidJavaProxy
        {
            readonly System.Action<float> _onChanged;
            public SensorListener(System.Action<float> onChanged)
                : base("android.hardware.SensorEventListener")
            {
                _onChanged = onChanged;
                Debug.Log("[StepCounter] SensorListener created");
            }

            void onSensorChanged(AndroidJavaObject evt)
            {
                var values = evt.Get<float[]>("values");
                if (values != null && values.Length > 0)
                {
                    _onChanged(values[0]);
                }
                else
                {
                    Debug.LogWarning("[StepCounter] onSensorChanged with empty values");
                }
            }

            void onAccuracyChanged(AndroidJavaObject sensor, int accuracy) { }
        }
    }
}
