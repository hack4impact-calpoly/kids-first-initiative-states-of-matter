using UnityEngine;
using System.Collections.Generic;
using System;

public class MockPotController : MonoBehaviour
{
    public List<IngredientSO> currentIngredients = new List<IngredientSO>();
    public event Action<IngredientSO> IngredientAdded;
    public Transform LastAddedIngredientTransform { get; private set; }

    private void Awake()
    {
        currentIngredients.Clear();
        LastAddedIngredientTransform = null;
        Debug.Log("Cleared currentIngredients");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("TRIGGER HIT: " + other.name);
        TryRegisterIngredient(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("COLLISION HIT: " + collision.collider.name);
        TryRegisterIngredient(collision.collider);
    }

    private void TryRegisterIngredient(Collider2D other)
    {
        var ingredient = other.GetComponent<IngredientInstance>();

        if (ingredient == null)
            ingredient = other.GetComponentInParent<IngredientInstance>();

        if (ingredient == null)
            ingredient = other.GetComponentInChildren<IngredientInstance>();

        if (ingredient == null)
        {
            Debug.Log("Still no IngredientInstance found on " + other.name);
            return;
        }

        Debug.Log("Found IngredientInstance: " + ingredient.Data.name);
        TryAddIngredient(ingredient.Data, ingredient.transform);
    }

    private void TryAddIngredient(IngredientSO ingredient, Transform ingredientTransform)
    {
        if (ingredient == null)
        {
            Debug.Log("IngredientSO is null");
            return;
        }

        if (currentIngredients.Contains(ingredient))
        {
            Debug.Log("Ingredient already added: " + ingredient.name);
            return;
        }

        currentIngredients.Add(ingredient);
        LastAddedIngredientTransform = ingredientTransform;
        Debug.Log("Invoking IngredientAdded for " + ingredient.name);
        IngredientAdded?.Invoke(ingredient);
    }

    public bool ContainsIngredient(IngredientSO requiredIngredient)
        => requiredIngredient != null && currentIngredients.Contains(requiredIngredient);
}
