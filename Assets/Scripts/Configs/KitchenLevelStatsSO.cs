using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Kitchen Level Stats")]
public class KitchenLevelStatsSO : ScriptableObject
{
    [Header("Melting Logic")]
    [Tooltip("How much progress is added per second at MAX heat.")]
    public float meltSpeed = 20f; 

    [Tooltip("The value needed to win (usually 100).")]
    public float winThreshold = 100f;
}