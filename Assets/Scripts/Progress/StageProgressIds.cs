using System.Collections.Generic;

public static class StageProgressIds
{
    public const string MatterKitchen = "matter-kitchen";
    public const string PipeRescue = "pipe-rescue";
    public const string StateLab = "state-lab";

    public const string MeltChocolate = "melt-chocolate";
    public const string PourJuice = "pour-juice";
    public const string FreezeJuice = "freeze-juice";

    public const string FreezePipeLeak = "freeze-a-plug";

    public const string MeltWax = "melt-wax";
    public const string IonizeGas = "ionize-gas";

    private static readonly string[] MatterKitchenStages =
    {
        MeltChocolate,
        PourJuice,
        FreezeJuice
    };

    private static readonly string[] PipeRescueStages =
    {
        FreezePipeLeak
    };

    private static readonly string[] StateLabStages =
    {
        MeltWax,
        IonizeGas
    };

    private static readonly string[] RequiredActivities =
    {
        MatterKitchen,
        PipeRescue,
        StateLab
    };

    public static IReadOnlyList<string> Activities => RequiredActivities;

    public static bool TryGetStageSequence(string activityId, out IReadOnlyList<string> stageIds)
    {
        switch (activityId)
        {
            case MatterKitchen:
                stageIds = MatterKitchenStages;
                return true;
            case PipeRescue:
                stageIds = PipeRescueStages;
                return true;
            case StateLab:
                stageIds = StateLabStages;
                return true;
            default:
                stageIds = null;
                return false;
        }
    }

    public static string ToKey(string activityId, string stageId)
    {
        return activityId + "/" + stageId;
    }

    public static bool IsValidSegment(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            char character = value[i];
            bool isLowercaseLetter = character >= 'a' && character <= 'z';
            bool isDigit = character >= '0' && character <= '9';
            if (!isLowercaseLetter && !isDigit && character != '-')
                return false;
        }

        return true;
    }
}
