using System;

namespace StepCounter
{
    public interface IStepCounter
    {
        event Action<int> OnStepsChanged;
        event Action<int> OnRawCumulativeStepsChanged;

        bool IsAvailable { get; }
        void Start();
        void Stop();
    }
}