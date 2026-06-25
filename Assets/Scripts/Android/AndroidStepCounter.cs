using UnityEngine;
using UnityEngine.Scripting;

namespace StepCounter.AndroidImpl
{
    [Preserve]
    public sealed class AndroidStepCounter : IStepCounter
    {
        public event System.Action<int> OnStepsChanged;

        // Raw TYPE_STEP_COUNTER value: cumulative steps since last device boot.
        // Use this for your new baseline-based quest tracking.
        public event System.Action<int> OnRawCumulativeStepsChanged;

        private const string ActivityRecognitionPermission = "android.permission.ACTIVITY_RECOGNITION";
        private const int SensorDelayNormal = 3; // android.hardware.SensorManager.SENSOR_DELAY_NORMAL
        private const int AndroidQ = 29;

        private AndroidJavaObject _activity;
        private AndroidJavaObject _sensorManager;
        private AndroidJavaObject _stepCounter;
        private AndroidJavaObject _stepDetector;

        private SensorListener _listenerCounter;
        private SensorListener _listenerDetector;
        private UnityEngine.Android.PermissionCallbacks _permissionCallbacks;

        private bool _registered;

        // Latest raw TYPE_STEP_COUNTER value.
        // -1 means we have not received a real cumulative value yet.
        private int _currentTotalSteps = -1;

        // Compatibility session value for existing OnStepsChanged logic.
        // This is NOT the Android raw total. It is the session delta from the first received counter value.
        private int _sessionSteps;

        // Baseline for compatibility session steps.
        // Important: this is intentionally NOT reset on every Register().
        // Otherwise steps taken while the app is paused/idle are lost.
        private int _sessionBaselineTotal = -1;

        public bool HasCurrentTotalSteps => _currentTotalSteps >= 0;
        public int CurrentTotalSteps => _currentTotalSteps;

        public bool IsAvailable => _stepCounter != null || _stepDetector != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterFactory()
        {
            global::StepCounter.StepCounterFactory.RegisterAndroidFactory(() => new AndroidStepCounter());
        }

        public AndroidStepCounter()
        {
            Debug.Log("[StepCounter] AndroidStepCounter ctor");

            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                _activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                using var ctx = new AndroidJavaClass("android.content.Context");
                string sensorService = ctx.GetStatic<string>("SENSOR_SERVICE");
                _sensorManager = _activity.Call<AndroidJavaObject>("getSystemService", sensorService);

                using var sensorClass = new AndroidJavaClass("android.hardware.Sensor");
                int typeStepCounter = sensorClass.GetStatic<int>("TYPE_STEP_COUNTER");
                int typeStepDetector = sensorClass.GetStatic<int>("TYPE_STEP_DETECTOR");

                _stepCounter = _sensorManager.Call<AndroidJavaObject>("getDefaultSensor", typeStepCounter);
                _stepDetector = _sensorManager.Call<AndroidJavaObject>("getDefaultSensor", typeStepDetector);

                Debug.Log($"[StepCounter] Available sensors: Counter={_stepCounter != null}, Detector={_stepDetector != null}");

                if (_stepCounter == null && _stepDetector != null)
                {
                    Debug.LogWarning("[StepCounter] TYPE_STEP_COUNTER unavailable. Falling back to TYPE_STEP_DETECTOR; reliable idle/background catch-up is unavailable.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[StepCounter] Failed to initialize AndroidStepCounter: {ex}");
            }
        }

        public void Start()
        {
            Debug.Log($"[StepCounter] Start() called. IsAvailable={IsAvailable}, Registered={_registered}");

            if (!IsAvailable)
            {
                Debug.LogWarning("[StepCounter] No step sensor available.");
                return;
            }

            if (_registered)
            {
                Debug.Log("[StepCounter] Already registered.");
                return;
            }

            bool requiresActivityRecognitionPermission = RequiresActivityRecognitionPermission();
            bool hasActivityRecognitionPermission = !requiresActivityRecognitionPermission ||
                UnityEngine.Android.Permission.HasUserAuthorizedPermission(ActivityRecognitionPermission);

            Debug.Log(
                $"[StepCounter] Permission {ActivityRecognitionPermission} required={requiresActivityRecognitionPermission}, " +
                $"granted={hasActivityRecognitionPermission}, sdk={GetAndroidSdkInt()}."
            );

            if (!hasActivityRecognitionPermission)
            {
                Debug.Log("[StepCounter] Requesting ACTIVITY_RECOGNITION permission...");

                _permissionCallbacks = new UnityEngine.Android.PermissionCallbacks();

                _permissionCallbacks.PermissionGranted += permission =>
                {
                    Debug.Log($"[StepCounter] Permission granted: {permission}");

                    if (permission == ActivityRecognitionPermission)
                    {
                        _permissionCallbacks = null;
                        Register();
                    }
                };

                _permissionCallbacks.PermissionDenied += permission =>
                {
                    Debug.LogWarning($"[StepCounter] Permission denied: {permission}");
                    _permissionCallbacks = null;
                };

                _permissionCallbacks.PermissionDeniedAndDontAskAgain += permission =>
                {
                    Debug.LogWarning($"[StepCounter] Permission don't-ask-again: {permission}");
                    _permissionCallbacks = null;
                };

                UnityEngine.Android.Permission.RequestUserPermission(ActivityRecognitionPermission, _permissionCallbacks);
                return;
            }

            Debug.Log("[StepCounter] Permission already granted, registering listener...");
            Register();
        }

        public void Stop()
        {
            Debug.Log("[StepCounter] Stop()");

            if (!_registered)
                return;

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

        private void Register()
        {
            if (_registered)
            {
                Debug.Log("[StepCounter] Register() called but already registered.");
                return;
            }

            if (_stepCounter != null)
            {
                _listenerCounter = new SensorListener(OnCounterChanged);

                bool ok = false;

                try
                {
                    ok = _sensorManager.Call<bool>(
                        "registerListener",
                        _listenerCounter,
                        _stepCounter,
                        SensorDelayNormal
                    );
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[StepCounter] Exception registering TYPE_STEP_COUNTER listener: {ex}");
                }

                _registered = ok;

                Debug.Log($"[StepCounter] Register TYPE_STEP_COUNTER -> {ok}");

                if (!ok)
                {
                    _listenerCounter = null;
                    Debug.LogWarning("[StepCounter] Failed to register TYPE_STEP_COUNTER listener.");
                }

                return;
            }

            if (_stepDetector != null)
            {
                Debug.LogWarning("[StepCounter] Registering TYPE_STEP_DETECTOR fallback. Cumulative catch-up is unavailable on this device.");

                _listenerDetector = new SensorListener(OnDetectorChanged);

                bool ok = false;

                try
                {
                    ok = _sensorManager.Call<bool>(
                        "registerListener",
                        _listenerDetector,
                        _stepDetector,
                        SensorDelayNormal
                    );
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[StepCounter] Exception registering TYPE_STEP_DETECTOR listener: {ex}");
                }

                _registered = ok;

                Debug.Log($"[StepCounter] Register TYPE_STEP_DETECTOR -> {ok}");

                if (!ok)
                {
                    _listenerDetector = null;
                    Debug.LogWarning("[StepCounter] Failed to register TYPE_STEP_DETECTOR listener.");
                }

                return;
            }

            Debug.LogWarning("[StepCounter] Register() called but no sensor instance exists.");
        }

        private static bool RequiresActivityRecognitionPermission()
        {
            return GetAndroidSdkInt() >= AndroidQ;
        }

        private static int GetAndroidSdkInt()
        {
            try
            {
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                return version.GetStatic<int>("SDK_INT");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[StepCounter] Could not read Android SDK version. Assuming runtime activity permission is required. Error: {ex.Message}");
                return AndroidQ;
            }
        }

        // TYPE_STEP_COUNTER: cumulative steps since last device boot.
        private void OnCounterChanged(float cumulative)
        {
            int rawCumulativeSteps = Mathf.Max(0, Mathf.RoundToInt(cumulative));

            _currentTotalSteps = rawCumulativeSteps;

            Debug.Log($"[StepCounter] Counter raw cumulative={rawCumulativeSteps}");

            // This is the important event for the new reliable step tracking.
            OnRawCumulativeStepsChanged?.Invoke(rawCumulativeSteps);

            // Compatibility behavior for existing code that listens to OnStepsChanged.
            // We use the first seen cumulative value as the in-memory session baseline.
            if (_sessionBaselineTotal < 0)
            {
                _sessionBaselineTotal = rawCumulativeSteps;
                _sessionSteps = 0;

                Debug.Log($"[StepCounter] Session baseline set: {_sessionBaselineTotal}");

                OnStepsChanged?.Invoke(_sessionSteps);
                return;
            }

            int calculatedSessionSteps = rawCumulativeSteps - _sessionBaselineTotal;

            if (calculatedSessionSteps < 0)
            {
                // This can happen after device reboot or weird sensor reset.
                // Do not reduce session steps.
                Debug.LogWarning(
                    $"[StepCounter] Raw cumulative lower than baseline. Raw={rawCumulativeSteps}, Baseline={_sessionBaselineTotal}. Keeping session={_sessionSteps}."
                );

                OnStepsChanged?.Invoke(_sessionSteps);
                return;
            }

            // Never go backwards.
            if (calculatedSessionSteps < _sessionSteps)
            {
                Debug.LogWarning(
                    $"[StepCounter] Calculated session went backwards. Calculated={calculatedSessionSteps}, Current={_sessionSteps}. Keeping current."
                );

                OnStepsChanged?.Invoke(_sessionSteps);
                return;
            }

            _sessionSteps = calculatedSessionSteps;

            Debug.Log($"[StepCounter] Counter session={_sessionSteps}, raw={rawCumulativeSteps}, baseline={_sessionBaselineTotal}");

            OnStepsChanged?.Invoke(_sessionSteps);
        }

        // TYPE_STEP_DETECTOR: one event == one step.
        // Fallback only. It cannot catch up missed steps after idle/background.
        private void OnDetectorChanged(float _)
        {
            _sessionSteps++;

            Debug.Log($"[StepCounter] Detector step -> session={_sessionSteps}");

            OnStepsChanged?.Invoke(_sessionSteps);
        }

        [Preserve]
        private sealed class SensorListener : AndroidJavaProxy
        {
            private readonly System.Action<float> _onChanged;

            public SensorListener(System.Action<float> onChanged)
                : base("android.hardware.SensorEventListener")
            {
                _onChanged = onChanged;
                Debug.Log("[StepCounter] SensorListener created");
            }

            [Preserve]
            public void onSensorChanged(AndroidJavaObject sensorEvent)
            {
                if (sensorEvent == null)
                {
                    Debug.LogWarning("[StepCounter] onSensorChanged called with null event.");
                    return;
                }

                float[] values = sensorEvent.Get<float[]>("values");

                if (values == null || values.Length == 0)
                {
                    Debug.LogWarning("[StepCounter] onSensorChanged with empty values.");
                    return;
                }

                _onChanged?.Invoke(values[0]);
            }

            [Preserve]
            public void onAccuracyChanged(AndroidJavaObject sensor, int accuracy)
            {
            }
        }
    }
}
