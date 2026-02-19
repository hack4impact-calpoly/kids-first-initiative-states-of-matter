using UnityEngine;
using UnityEngine.UI;

// Automatically adds a Slider component if one isn't there
[RequireComponent(typeof(Slider))]
public class HeatController : MonoBehaviour
{
    [SerializeField] private Slider heatSlider;
    
    // Public property so we can read the heat level
    public float CurrentHeat { get; private set; }

    private void OnValidate()
    {
        // Auto-assign the reference in the Editor
        if (heatSlider == null) 
            heatSlider = GetComponent<Slider>();
    }

    // This will be linked to the Slider's "On Value Changed" event in Unity
    public void OnHeatChanged()
    {
        CurrentHeat = heatSlider.value;
    }
}