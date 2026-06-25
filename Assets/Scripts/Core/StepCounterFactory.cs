// StepCounterFactory.cs (Core)
using System;
using UnityEngine;

namespace StepCounter
{
    public static class StepCounterFactory
    {
        private static Func<IStepCounter> _androidFactory;

        public static void RegisterAndroidFactory(Func<IStepCounter> factory)
        {
            _androidFactory = factory;
            Debug.Log($"[StepCounter] Android factory registered: {factory != null}");
        }

        public static IStepCounter Create()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log("[StepCounter] Factory.Create() on ANDROID");

            var counter = TryCreateRegisteredAndroidCounter();

            if (counter == null)
            {
                counter = TryCreate("StepCounter.AndroidImpl.AndroidStepCounter, StepCounter.Android");
            }

            Debug.Log($"[StepCounter] TryCreate returned: {counter?.GetType().FullName ?? "null"}");

            if (counter != null && counter.IsAvailable)
            {
                Debug.Log("[StepCounter] Using AndroidStepCounter");
                return counter;
            }

            Debug.LogWarning("[StepCounter] Android impl not available or no step sensors exist; using unavailable counter.");
            return new UnavailableStepCounter();
#else
    Debug.Log("[StepCounter] Non-Android platform; using Mock");
            return new MockStepCounter();
#endif
        }

        static IStepCounter TryCreateRegisteredAndroidCounter()
        {
            if (_androidFactory == null)
            {
                Debug.LogWarning("[StepCounter] No registered Android factory found; trying reflection fallback.");
                return null;
            }

            try
            {
                return _androidFactory();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StepCounter] Registered Android factory failed: {ex}");
                return null;
            }
        }

        static IStepCounter TryCreate(string assemblyQualifiedName)
        {
            try
            {
                var t = Type.GetType(assemblyQualifiedName, throwOnError: false);

                if (t == null)
                {
                    Debug.LogWarning($"[StepCounter] Could not find step counter type: {assemblyQualifiedName}");
                    return null;
                }

                return Activator.CreateInstance(t) as IStepCounter;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StepCounter] Failed to create step counter '{assemblyQualifiedName}': {ex}");
                return null;
            }
        }

        private sealed class UnavailableStepCounter : IStepCounter
        {
            public event Action<int> OnStepsChanged;
            public event Action<int> OnRawCumulativeStepsChanged;

            public bool IsAvailable => false;
            public bool HasCurrentTotalSteps => false;
            public int CurrentTotalSteps => -1;

            public void Start()
            {
                Debug.LogWarning("[StepCounter] Start() ignored because no Android step counter is available.");
            }

            public void Stop()
            {
            }
        }
    }
}
