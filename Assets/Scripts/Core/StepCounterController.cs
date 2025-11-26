// StepCounterController.cs (Core)
using TMPro;
using UnityEngine;

namespace StepCounter
{
    public class StepCounterController : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI stepsText;
        IStepCounter _counter;

        void Awake()
        {
            _counter = StepCounterFactory.Create();
            _counter.OnStepsChanged += s => 
            {
                if (stepsText)
                {
                    stepsText.text = $"Steps: {s}";
                }
            };
        }

        void OnApplicationFocus(bool focus)
        {
            if (focus && _counter != null)
            {
                _counter.Stop();
                _counter.Start(); // restart after permission is granted to ensure registration of sensor
            }
        }


        void OnEnable() => _counter.Start();
        void OnDisable() => _counter.Stop();
    }
}
