using UnityEngine;

[CreateAssetMenu(menuName = "Kitchen/Level Config")]
public class KitchenLevelConfigSO : ScriptableObject
{
    [Header("Required Ingredient")]
    [Tooltip("The ingredient that must be in the pot for success (e.g. Chocolate for Solid level).")]
    public IngredientSO requiredIngredient;

    [Header("Win Condition")]
    [Tooltip("If true, win only when heat is at max AND required ingredient is in the pot.")]
    public bool requireMaxHeat = true;

    [Header("Fail Condition")]
    [Tooltip("If true, turning heat to max before the ingredient is in the pot counts as failure.")]
    public bool failIfMaxHeatBeforeIngredient = true;
}
