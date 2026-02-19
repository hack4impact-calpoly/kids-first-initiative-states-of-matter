using UnityEngine;
using System.Collections.Generic;

public class MockPotController : MonoBehaviour
{
    // Explicitly visible list of ingredients in the Inspector
    public List<IngredientSO> currentIngredients = new List<IngredientSO>();

    // Helper method to check if specific data exists
    public bool ContainsIngredient(IngredientSO requiredIngredient)
    {
        return currentIngredients.Contains(requiredIngredient);
    }
}