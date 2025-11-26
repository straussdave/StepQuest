// IStepCounter.cs
namespace StepCounter
{
    public interface IStepCounter
    {
        event System.Action<int> OnStepsChanged;
        bool IsAvailable { get; }
        void Start();
        void Stop();
    }
}