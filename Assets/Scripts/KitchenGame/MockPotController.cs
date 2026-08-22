using UnityEngine;
using System.Collections.Generic;
using System;

public class MockPotController : MonoBehaviour
{
    [Header("Container Visual")]
    [SerializeField] private SpriteRenderer containerRenderer;
    [SerializeField] private Sprite openContainerSprite;
    [SerializeField] private Sprite closedContainerSprite;
    [SerializeField] private bool startsOpen = false;
    [SerializeField] private bool closeWhenIngredientAdded = true;
    [SerializeField] private Collider2D placementArea;
    [SerializeField] private float placementInset = 0f;

    public List<IngredientSO> currentIngredients = new List<IngredientSO>();
    public event Action<IngredientSO> IngredientAdded;
    public event Action<IngredientSO> IngredientRemoved;
    public Transform LastAddedIngredientTransform { get; private set; }
    public bool HasIngredients => currentIngredients.Count > 0;

    private readonly Dictionary<IngredientInstance, int> activeIngredientContacts = new Dictionary<IngredientInstance, int>();

    private void Awake()
    {
        currentIngredients.Clear();
        activeIngredientContacts.Clear();
        LastAddedIngredientTransform = null;
        SetContainerOpen(startsOpen);
        Debug.Log("Cleared currentIngredients");
    }

    private void Update()
    {
        // A dropped ingredient can settle fully inside the freezer after its first contact.
        // Re-check active contacts so placement does not depend on a single physics callback.
        foreach (IngredientInstance ingredient in new List<IngredientInstance>(activeIngredientContacts.Keys))
            TryAddIngredient(ingredient);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("TRIGGER HIT: " + other.name);
        TryRegisterIngredient(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log("TRIGGER EXIT: " + other.name);
        TryUnregisterIngredient(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("COLLISION HIT: " + collision.collider.name);
        TryRegisterIngredient(collision.collider);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        Debug.Log("COLLISION EXIT: " + collision.collider.name);
        TryUnregisterIngredient(collision.collider);
    }

    private void TryRegisterIngredient(Collider2D other)
    {
        if (!TryGetIngredient(other, out IngredientInstance ingredient))
            return;

        Debug.Log("Found IngredientInstance: " + (ingredient.Data != null ? ingredient.Data.name : "NULL"));
        if (activeIngredientContacts.TryGetValue(ingredient, out int contactCount))
            activeIngredientContacts[ingredient] = contactCount + 1;
        else
            activeIngredientContacts.Add(ingredient, 1);

        TryAddIngredient(ingredient);
    }

    private void TryUnregisterIngredient(Collider2D other)
    {
        if (!TryGetIngredient(other, out IngredientInstance ingredient))
            return;

        TryRemoveIngredient(ingredient);
    }

    private bool TryGetIngredient(Collider2D other, out IngredientInstance ingredient)
    {
        ingredient = other.GetComponent<IngredientInstance>();

        if (ingredient == null)
            ingredient = other.GetComponentInParent<IngredientInstance>();

        if (ingredient == null)
            ingredient = other.GetComponentInChildren<IngredientInstance>();

        if (ingredient != null)
            return true;

        Debug.Log("Still no IngredientInstance found on " + other.name);
        return false;
    }

    private void TryAddIngredient(IngredientInstance ingredientInstance)
    {
        IngredientSO ingredient = ingredientInstance.Data;

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

        if (!IsIngredientFullyInside(ingredientInstance))
            return;

        currentIngredients.Add(ingredient);
        LastAddedIngredientTransform = ingredientInstance.transform;
        if (closeWhenIngredientAdded)
            SetContainerOpen(false);
        Debug.Log("Invoking IngredientAdded for " + ingredient.name);
        IngredientAdded?.Invoke(ingredient);
    }

    private void TryRemoveIngredient(IngredientInstance ingredientInstance)
    {
        if (!activeIngredientContacts.TryGetValue(ingredientInstance, out int contactCount))
            return;

        if (contactCount > 1)
        {
            activeIngredientContacts[ingredientInstance] = contactCount - 1;
            return;
        }

        activeIngredientContacts.Remove(ingredientInstance);

        IngredientSO ingredient = ingredientInstance.Data;
        if (ingredient == null || HasActiveIngredientWithData(ingredient))
            return;

        currentIngredients.Remove(ingredient);

        if (LastAddedIngredientTransform == ingredientInstance.transform)
            LastAddedIngredientTransform = null;

        Debug.Log("Invoking IngredientRemoved for " + ingredient.name);
        IngredientRemoved?.Invoke(ingredient);
    }

    private bool IsIngredientFullyInside(IngredientInstance ingredientInstance)
    {
        Collider2D area = placementArea != null ? placementArea : GetComponent<BoxCollider2D>();
        if (area == null)
            return true;

        Collider2D[] ingredientColliders = ingredientInstance.GetComponentsInChildren<Collider2D>();
        if (ingredientColliders.Length == 0)
            return false;

        Bounds ingredientBounds = ingredientColliders[0].bounds;
        for (int i = 1; i < ingredientColliders.Length; i++)
            ingredientBounds.Encapsulate(ingredientColliders[i].bounds);

        return IngredientPlacementUtility.AreBoundsContained(area.bounds, ingredientBounds, placementInset);
    }

    private bool HasActiveIngredientWithData(IngredientSO ingredient)
    {
        foreach (IngredientInstance activeIngredient in activeIngredientContacts.Keys)
        {
            if (activeIngredient != null && activeIngredient.Data == ingredient)
                return true;
        }

        return false;
    }

    public bool ContainsIngredient(IngredientSO requiredIngredient)
        => requiredIngredient != null && currentIngredients.Contains(requiredIngredient);

    public void SetContainerOpen(bool isOpen)
    {
        if (containerRenderer == null)
            return;

        Sprite targetSprite = isOpen ? openContainerSprite : closedContainerSprite;

        if (targetSprite != null)
            containerRenderer.sprite = targetSprite;
    }
}
