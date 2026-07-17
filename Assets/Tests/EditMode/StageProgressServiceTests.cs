using NUnit.Framework;
using UnityEngine;

public sealed class StageProgressServiceTests
{
    [SetUp]
    public void SetUp()
    {
        StageProgressService.ResetAllProgress();
    }

    [TearDown]
    public void TearDown()
    {
        StageProgressService.ResetAllProgress();

        if (StageProgressService.Instance != null)
            Object.DestroyImmediate(StageProgressService.Instance.gameObject);
    }

    [Test]
    public void CompleteStage_StoresSnapshotAndSuppressesDuplicateCompletion()
    {
        StageProgressService.BeginStage(StageProgressIds.MatterKitchen, StageProgressIds.MeltChocolate);

        Assert.That(
            StageProgressService.CompleteStage(StageProgressIds.MatterKitchen, StageProgressIds.MeltChocolate),
            Is.True);
        Assert.That(
            StageProgressService.CompleteStage(StageProgressIds.MatterKitchen, StageProgressIds.MeltChocolate),
            Is.False);
        Assert.That(
            StageProgressService.IsStageComplete(StageProgressIds.MatterKitchen, StageProgressIds.MeltChocolate),
            Is.True);
        Assert.That(
            StageProgressService.GetAttempts(StageProgressIds.MatterKitchen, StageProgressIds.MeltChocolate),
            Is.EqualTo(1));
        Assert.That(
            StageProgressService.GetCompletedStageIds(),
            Is.EqualTo(new[] { "matter-kitchen/melt-chocolate" }));
    }

    [Test]
    public void BeginStage_CountsEachAttempt()
    {
        StageProgressService.BeginStage(StageProgressIds.PipeRescue, StageProgressIds.FreezePipeLeak);
        StageProgressService.BeginStage(StageProgressIds.PipeRescue, StageProgressIds.FreezePipeLeak);

        Assert.That(
            StageProgressService.GetAttempts(StageProgressIds.PipeRescue, StageProgressIds.FreezePipeLeak),
            Is.EqualTo(2));
    }

    [Test]
    public void IsStageUnlocked_RequiresThePreviousStage()
    {
        Assert.That(
            StageProgressService.IsStageUnlocked(StageProgressIds.MatterKitchen, StageProgressIds.MeltChocolate),
            Is.True);
        Assert.That(
            StageProgressService.IsStageUnlocked(StageProgressIds.MatterKitchen, StageProgressIds.PourJuice),
            Is.False);

        StageProgressService.CompleteStage(StageProgressIds.MatterKitchen, StageProgressIds.MeltChocolate);

        Assert.That(
            StageProgressService.IsStageUnlocked(StageProgressIds.MatterKitchen, StageProgressIds.PourJuice),
            Is.True);
        Assert.That(
            StageProgressService.GetNextIncompleteStage(StageProgressIds.MatterKitchen),
            Is.EqualTo(StageProgressIds.PourJuice));
    }
}
