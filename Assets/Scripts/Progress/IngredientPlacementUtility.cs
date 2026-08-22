using UnityEngine;

public static class IngredientPlacementUtility
{
    public static bool AreBoundsContained(Bounds containerBounds, Bounds ingredientBounds, float inset)
    {
        containerBounds.Expand(-2f * Mathf.Max(0f, inset));

        return ingredientBounds.min.x >= containerBounds.min.x
            && ingredientBounds.max.x <= containerBounds.max.x
            && ingredientBounds.min.y >= containerBounds.min.y
            && ingredientBounds.max.y <= containerBounds.max.y;
    }
}
