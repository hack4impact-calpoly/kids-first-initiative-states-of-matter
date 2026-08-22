using NUnit.Framework;
using UnityEngine;

public sealed class MockPotControllerTests
{
    [Test]
    public void AreBoundsContained_RejectsIngredientThatOnlyPartiallyOverlaps()
    {
        Bounds placementArea = new Bounds(Vector3.zero, new Vector3(2f, 1f, 1f));
        Bounds partiallyOverlappingIngredient = new Bounds(
            new Vector3(0f, 0.4f, 0f),
            new Vector3(1f, 1f, 1f));

        Assert.That(
            IngredientPlacementUtility.AreBoundsContained(placementArea, partiallyOverlappingIngredient, 0.02f),
            Is.False);
    }

    [Test]
    public void AreBoundsContained_AcceptsIngredientFullyInsidePlacementArea()
    {
        Bounds placementArea = new Bounds(Vector3.zero, new Vector3(1f, 1f, 1f));
        Bounds containedIngredient = new Bounds(
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 1f, 1f));

        Assert.That(
            IngredientPlacementUtility.AreBoundsContained(placementArea, containedIngredient, 0f),
            Is.True);
    }
}
