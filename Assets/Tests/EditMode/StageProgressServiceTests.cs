using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class StageProgressServiceTests
{
    private const string SaveKey = "KFI.StatesOfMatter.StageProgress.v1";

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
            UnityEngine.Object.DestroyImmediate(StageProgressService.Instance.gameObject);
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

    [Test]
    public void GameCompletion_DoesNotEmitWhileARequiredStageIsIncomplete()
    {
        CompleteAllRequiredStagesExcept(StageProgressIds.StateLab, StageProgressIds.IonizeGas);
        var payloads = CaptureProgressPayloads(() =>
            Assert.That(StageProgressService.ReportGameCompletion(), Is.False));

        Assert.That(StageProgressService.IsGameComplete(), Is.False);
        Assert.That(payloads, Is.Empty);
        Assert.That(payloads.Exists(payload => payload.gameCompleted), Is.False);
    }

    [Test]
    public void GameCompletion_FinalPayloadContainsEveryRequiredStage()
    {
        CompleteAllRequiredStagesExcept(StageProgressIds.StateLab, StageProgressIds.IonizeGas);

        var stagePayloads = CaptureProgressPayloads(() =>
            StageProgressService.CompleteStage(StageProgressIds.StateLab, StageProgressIds.IonizeGas));
        var completionPayloads = CaptureProgressPayloads(() =>
            Assert.That(StageProgressService.ReportGameCompletion(), Is.True));

        Assert.That(StageProgressService.IsGameComplete(), Is.True);
        Assert.That(StageProgressService.CanReportGameCompletion(), Is.False);
        Assert.That(stagePayloads, Has.Count.EqualTo(1));
        Assert.That(stagePayloads[0].gameCompleted, Is.False);
        Assert.That(completionPayloads, Has.Count.EqualTo(1));
        Assert.That(completionPayloads[0].gameCompleted, Is.True);
        Assert.That(completionPayloads[0].completedStageIds, Is.EqualTo(new[]
        {
            "matter-kitchen/freeze-juice",
            "matter-kitchen/melt-chocolate",
            "matter-kitchen/pour-juice",
            "pipe-rescue/freeze-a-plug",
            "state-lab/ionize-gas",
            "state-lab/melt-wax"
        }));
    }

    [Test]
    public void GameCompletion_RepeatedStageCompletionDoesNotEmitAgain()
    {
        CompleteAllRequiredStagesExcept(null, null);

        var payloads = CaptureProgressPayloads(() =>
        {
            Assert.That(StageProgressService.ReportGameCompletion(), Is.True);
            Assert.That(StageProgressService.ReportGameCompletion(), Is.False);

            UnityEngine.Object.DestroyImmediate(StageProgressService.Instance.gameObject);
            Assert.That(StageProgressService.ReportGameCompletion(), Is.False);
        });

        Assert.That(payloads.FindAll(payload => payload.gameCompleted), Has.Count.EqualTo(1));
        Assert.That(StageProgressService.CanReportGameCompletion(), Is.False);
    }

    [Test]
    public void GameCompletion_LegacyCompletedSaveWaitsForExplicitReport()
    {
        CompleteAllRequiredStagesExcept(null, null);

        StageProgressSaveData currentData = JsonUtility.FromJson<StageProgressSaveData>(PlayerPrefs.GetString(SaveKey));
        var legacyData = new LegacyStageProgressSaveData
        {
            saveVersion = currentData.saveVersion,
            stages = currentData.stages
        };
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(legacyData));
        PlayerPrefs.Save();

        MethodInfo load = typeof(StageProgressService).GetMethod("Load", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(load, Is.Not.Null);
        load.Invoke(StageProgressService.Instance, null);

        var replayPayloads = CaptureProgressPayloads(() =>
        {
            StageProgressService.BeginStage(StageProgressIds.MatterKitchen, StageProgressIds.MeltChocolate);
        });
        var completionPayloads = CaptureProgressPayloads(() =>
            Assert.That(StageProgressService.ReportGameCompletion(), Is.True));

        Assert.That(replayPayloads, Has.Count.EqualTo(1));
        Assert.That(replayPayloads[0].gameCompleted, Is.False);
        Assert.That(completionPayloads, Has.Count.EqualTo(1));
        Assert.That(completionPayloads[0].gameCompleted, Is.True);
        Assert.That(completionPayloads[0].completedStageIds, Has.Length.EqualTo(6));
    }

    private static void CompleteAllRequiredStagesExcept(string excludedActivityId, string excludedStageId)
    {
        IReadOnlyList<string> activities = StageProgressIds.Activities;
        for (int activityIndex = 0; activityIndex < activities.Count; activityIndex++)
        {
            Assert.That(
                StageProgressIds.TryGetStageSequence(activities[activityIndex], out IReadOnlyList<string> stages),
                Is.True);

            for (int stageIndex = 0; stageIndex < stages.Count; stageIndex++)
            {
                if (activities[activityIndex] == excludedActivityId && stages[stageIndex] == excludedStageId)
                    continue;

                StageProgressService.CompleteStage(activities[activityIndex], stages[stageIndex]);
            }
        }
    }

    private static List<GameCompletionProbe> CaptureProgressPayloads(Action action)
    {
        var payloads = new List<GameCompletionProbe>();
        Application.LogCallback handler = (message, _, _) =>
        {
            const string prefix = "[StageProgress] Web payload: ";
            if (message.StartsWith(prefix, StringComparison.Ordinal))
                payloads.Add(JsonUtility.FromJson<GameCompletionProbe>(message.Substring(prefix.Length)));
        };

        Application.logMessageReceived += handler;
        try
        {
            action();
        }
        finally
        {
            Application.logMessageReceived -= handler;
        }

        return payloads;
    }

    [Serializable]
    private sealed class GameCompletionProbe
    {
        public string[] completedStageIds;
        public bool gameCompleted;
    }

    [Serializable]
    private sealed class LegacyStageProgressSaveData
    {
        public int saveVersion = 1;
        public List<StageProgressRecord> stages = new List<StageProgressRecord>();
    }
}
