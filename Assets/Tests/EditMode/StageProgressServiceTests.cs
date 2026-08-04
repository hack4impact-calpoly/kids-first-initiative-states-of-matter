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

    [Test]
    public void ActivityFlowCatalog_RoutesToFirstIncompleteKitchenStage()
    {
        Assert.That(
            ActivityFlowCatalog.GetEntryScene(StageProgressIds.MatterKitchen),
            Is.EqualTo(ActivityFlowCatalog.KitchenSolidScene));

        StageProgressService.CompleteStage(StageProgressIds.MatterKitchen, StageProgressIds.MeltChocolate);

        Assert.That(
            ActivityFlowCatalog.GetEntryScene(StageProgressIds.MatterKitchen),
            Is.EqualTo(ActivityFlowCatalog.KitchenPourScene));
        Assert.That(
            ActivityFlowCatalog.GetRecommendedActivity(),
            Is.EqualTo(StageProgressIds.MatterKitchen));
    }

    [Test]
    public void ActivityStatus_TracksAttemptsAndCompletion()
    {
        Assert.That(StageProgressService.HasAnyProgress(), Is.False);
        Assert.That(
            ActivityFlowCatalog.GetStatus(StageProgressIds.PipeRescue),
            Is.EqualTo(ActivityProgressStatus.New));

        StageProgressService.BeginStage(StageProgressIds.PipeRescue, StageProgressIds.FreezePipeLeak);

        Assert.That(StageProgressService.HasAnyProgress(), Is.True);
        Assert.That(
            ActivityFlowCatalog.GetStatus(StageProgressIds.PipeRescue),
            Is.EqualTo(ActivityProgressStatus.InProgress));

        StageProgressService.CompleteStage(StageProgressIds.PipeRescue, StageProgressIds.FreezePipeLeak);

        Assert.That(
            ActivityFlowCatalog.GetStatus(StageProgressIds.PipeRescue),
            Is.EqualTo(ActivityProgressStatus.Complete));
    }

    [Test]
    public void StageFinished_FiresForNewAndReplayedCompletion()
    {
        int finishCount = 0;
        System.Action<string, string> handler = (_, _) => finishCount++;
        StageProgressService.StageFinished += handler;

        try
        {
            StageProgressService.CompleteStage(StageProgressIds.PipeRescue, StageProgressIds.FreezePipeLeak);
            StageProgressService.CompleteStage(StageProgressIds.PipeRescue, StageProgressIds.FreezePipeLeak);
        }
        finally
        {
            StageProgressService.StageFinished -= handler;
        }

        Assert.That(finishCount, Is.EqualTo(2));
    }
}
