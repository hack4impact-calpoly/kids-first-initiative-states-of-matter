using System;
using System.Collections.Generic;

public enum ActivityProgressStatus
{
    New,
    InProgress,
    Complete
}

public static class ActivityFlowCatalog
{
    public const string TitleScene = "States of Matter Menu";
    public const string SelectorScene = "GameSelector";
    public const string KitchenSolidScene = "Kitchen Game - Solid";
    public const string KitchenPourScene = "Kitchen Game - Freezing Pour";
    public const string KitchenFreezeScene = "Kitchen Game - Freezing Station";
    public const string PipeScene = "Pipes-Frozen-Level";
    public const string LabScene = "Wires";

    private static readonly string[] ActivityOrder =
    {
        StageProgressIds.MatterKitchen,
        StageProgressIds.PipeRescue,
        StageProgressIds.StateLab
    };

    public static IReadOnlyList<string> Activities => ActivityOrder;

    public static string GetDisplayName(string activityId)
    {
        switch (activityId)
        {
            case StageProgressIds.MatterKitchen:
                return "Matter Kitchen";
            case StageProgressIds.PipeRescue:
                return "Pipe Rescue";
            case StageProgressIds.StateLab:
                return "State Lab";
            default:
                return "Activity";
        }
    }

    public static string GetTheme(string activityId)
    {
        switch (activityId)
        {
            case StageProgressIds.MatterKitchen:
                return "Turn liquid juice into a solid.";
            case StageProgressIds.PipeRescue:
                return "Freeze water to build a route.";
            case StageProgressIds.StateLab:
                return "Use energy to change matter.";
            default:
                return string.Empty;
        }
    }

    public static string GetEntryScene(string activityId)
    {
        string nextStage = StageProgressService.GetNextIncompleteStage(activityId);
        if (string.IsNullOrEmpty(nextStage)
            && StageProgressIds.TryGetStageSequence(activityId, out IReadOnlyList<string> stages)
            && stages.Count > 0)
        {
            nextStage = stages[0];
        }

        return GetSceneForStage(activityId, nextStage);
    }

    public static string GetSceneForStage(string activityId, string stageId)
    {
        if (activityId == StageProgressIds.MatterKitchen)
        {
            switch (stageId)
            {
                case StageProgressIds.MeltChocolate:
                    return KitchenSolidScene;
                case StageProgressIds.PourJuice:
                    return KitchenPourScene;
                case StageProgressIds.FreezeJuice:
                    return KitchenFreezeScene;
            }
        }

        if (activityId == StageProgressIds.PipeRescue)
            return PipeScene;

        if (activityId == StageProgressIds.StateLab)
            return LabScene;

        return SelectorScene;
    }

    public static string GetNextStageScene(string activityId, string completedStageId)
    {
        if (!StageProgressIds.TryGetStageSequence(activityId, out IReadOnlyList<string> stages))
            return null;

        for (int i = 0; i < stages.Count - 1; i++)
        {
            if (stages[i] == completedStageId)
                return GetSceneForStage(activityId, stages[i + 1]);
        }

        return null;
    }

    public static string GetRecommendedActivity()
    {
        for (int i = 0; i < ActivityOrder.Length; i++)
        {
            if (!IsActivityComplete(ActivityOrder[i]))
                return ActivityOrder[i];
        }

        return StageProgressIds.MatterKitchen;
    }

    public static string GetNextActivity(string activityId)
    {
        for (int i = 0; i < ActivityOrder.Length - 1; i++)
        {
            if (ActivityOrder[i] == activityId)
                return ActivityOrder[i + 1];
        }

        return null;
    }

    public static int GetCompletedStageCount(string activityId)
    {
        if (!StageProgressIds.TryGetStageSequence(activityId, out IReadOnlyList<string> stages))
            return 0;

        int completed = 0;
        for (int i = 0; i < stages.Count; i++)
        {
            if (StageProgressService.IsStageComplete(activityId, stages[i]))
                completed++;
        }

        return completed;
    }

    public static int GetStageCount(string activityId)
    {
        return StageProgressIds.TryGetStageSequence(activityId, out IReadOnlyList<string> stages)
            ? stages.Count
            : 0;
    }

    public static bool IsActivityComplete(string activityId)
    {
        int stageCount = GetStageCount(activityId);
        return stageCount > 0 && GetCompletedStageCount(activityId) == stageCount;
    }

    public static ActivityProgressStatus GetStatus(string activityId)
    {
        if (IsActivityComplete(activityId))
            return ActivityProgressStatus.Complete;

        return StageProgressService.HasActivityProgress(activityId)
            ? ActivityProgressStatus.InProgress
            : ActivityProgressStatus.New;
    }

    public static string GetStatusLabel(string activityId)
    {
        switch (GetStatus(activityId))
        {
            case ActivityProgressStatus.Complete:
                return "COMPLETE";
            case ActivityProgressStatus.InProgress:
                return "IN PROGRESS";
            default:
                return "NEW";
        }
    }

    public static string GetActivityForScene(string sceneName)
    {
        switch (sceneName)
        {
            case KitchenSolidScene:
            case KitchenPourScene:
            case KitchenFreezeScene:
                return StageProgressIds.MatterKitchen;
            case PipeScene:
                return StageProgressIds.PipeRescue;
            case LabScene:
                return StageProgressIds.StateLab;
            default:
                return null;
        }
    }

    public static string GetStageForScene(string sceneName)
    {
        switch (sceneName)
        {
            case KitchenSolidScene:
                return StageProgressIds.MeltChocolate;
            case KitchenPourScene:
                return StageProgressIds.PourJuice;
            case KitchenFreezeScene:
                return StageProgressIds.FreezeJuice;
            case PipeScene:
                return StageProgressIds.FreezePipeLeak;
            default:
                return null;
        }
    }

    public static string GetStageTitle(string activityId, string stageId)
    {
        switch (StageProgressIds.ToKey(activityId, stageId))
        {
            case "matter-kitchen/melt-chocolate":
                return "Chocolate Melted!";
            case "matter-kitchen/pour-juice":
                return "Tray Filled!";
            case "matter-kitchen/freeze-juice":
                return "Juice Frozen!";
            case "pipe-rescue/freeze-a-plug":
                return "Water Delivered!";
            case "state-lab/melt-wax":
                return "Wax Melted!";
            case "state-lab/ionize-gas":
                return "Plasma Created!";
            default:
                return "Experiment Complete!";
        }
    }

    public static string GetStageRecap(string activityId, string stageId)
    {
        switch (StageProgressIds.ToKey(activityId, stageId))
        {
            case "matter-kitchen/melt-chocolate":
                return "Heat changed solid chocolate into liquid chocolate.";
            case "matter-kitchen/pour-juice":
                return "The liquid flowed and took the shape of the tray.";
            case "matter-kitchen/freeze-juice":
                return "Cooling changed the liquid juice into a solid.";
            case "pipe-rescue/freeze-a-plug":
                return "Liquid water flowed through the route while solid ice blocked the leaks.";
            case "state-lab/melt-wax":
                return "Electrical energy produced heat that melted solid wax.";
            case "state-lab/ionize-gas":
                return "Electrical energy changed gas into glowing plasma.";
            default:
                return "You observed matter change state.";
        }
    }
}
