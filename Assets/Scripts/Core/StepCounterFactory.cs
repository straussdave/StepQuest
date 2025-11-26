// StepCounterFactory.cs (Core)
using System;
using UnityEngine;

namespace StepCounter
{
    public static class StepCounterFactory
    {
        public static IStepCounter Create()
        {
#if UNITY_ANDROID
            Debug.Log("[StepCounter] Factory.Create() on ANDROID");
            var counter = TryCreate("StepCounter.AndroidImpl.AndroidStepCounter, StepCounter.Android");
            Debug.Log($"[StepCounter] TryCreate returned: {counter?.GetType().FullName ?? "null"}");

            if (counter != null && counter.IsAvailable)
            {
                Debug.Log("[StepCounter] Using AndroidStepCounter");
                return counter;
            }

            Debug.Log("[StepCounter] Android impl not available; falling back to Mock");
#else
    Debug.Log("[StepCounter] Non-Android platform; using Mock");
#endif
            return new MockStepCounter();
        }

        static IStepCounter TryCreate(string assemblyQualifiedName)
        {
            var t = Type.GetType(assemblyQualifiedName, throwOnError: false);
            return t != null ? Activator.CreateInstance(t) as IStepCounter : null;
        }
    }
}
