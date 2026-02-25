using UnityEngine;
using System.Collections.Generic;
using System;

public class MockPotController : MonoBehaviour
{
    // Explicitly visible list of ingredients in the Inspector
    public List<IngredientSO> currentIngredients = new List<IngredientSO>();
    public event Action<IngredientSO> IngredientAdded;
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryRegisterIngredient(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryRegisterIngredient(collision.collider);
    }

    private void TryRegisterIngredient(Collider2D other)
    {
        var ingredient = other.GetComponentInParent<IngredientInstance>();
        if (ingredient == null) return;
        TryAddIngredient(ingredient.Data);
    }

    private void TryAddIngredient(IngredientSO ingredient)
    {
        if (ingredient == null) return;
        if (currentIngredients.Contains(ingredient)) return;

        currentIngredients.Add(ingredient);
        IngredientAdded?.Invoke(ingredient);
    }

    // Helper method to check if specific data exists
    public bool ContainsIngredient(IngredientSO requiredIngredient)
        => requiredIngredient != null && currentIngredients.Contains(requiredIngredient);
}