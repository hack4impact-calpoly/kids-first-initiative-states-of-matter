using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class HeatController : MonoBehaviour
{
    [SerializeField] private Slider heatSlider;
    public float CurrentHeat { get; private set; }
    public bool IsMaxHeat => heatSlider != null && heatSlider.value >= heatSlider.maxValue - 0.001f;
    public event Action<float> HeatChanged;

    private void OnValidate()
    {
        if (heatSlider == null)
            heatSlider = GetComponent<Slider>();
    }

    private void Start()
    {
        if (heatSlider != null)
            SyncFromSlider(heatSlider.value);
    }

    public void OnHeatChanged()
    {
        if (heatSlider != null)
            SyncFromSlider(heatSlider.value);
    }

    public void OnHeatChanged(float value)
    {
        SyncFromSlider(value);
    }

    private void SyncFromSlider(float value)
    {
        CurrentHeat = value;
        HeatChanged?.Invoke(value);
    }
}