using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class JuiceCoolingController : MonoBehaviour
{
    [SerializeField] private Slider temperatureSlider;
    [SerializeField] private float freezeThreshold = 0.2f;

    public float CurrentTemperature { get; private set; }

    // Bottom/minimum temperature is cold enough.
    public bool IsColdEnough => temperatureSlider != null && temperatureSlider.value <= freezeThreshold;

    public event Action<float> TemperatureChanged;

    private void OnValidate()
    {
        if (temperatureSlider == null)
            temperatureSlider = GetComponent<Slider>();
    }

    private void Start()
    {
        if (temperatureSlider != null)
        {
            temperatureSlider.value = temperatureSlider.maxValue;
            SyncFromSlider(temperatureSlider.value);
        }
    }

    public void OnTemperatureChanged()
    {
        if (temperatureSlider != null)
            SyncFromSlider(temperatureSlider.value);
    }

    public void OnTemperatureChanged(float value)
    {
        SyncFromSlider(value);
    }

    private void SyncFromSlider(float value)
    {
        CurrentTemperature = value;
        TemperatureChanged?.Invoke(value);
    }
}
