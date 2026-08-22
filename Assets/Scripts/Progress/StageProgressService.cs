using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class StageProgressService : MonoBehaviour
{
    private const string SaveKey = "KFI.StatesOfMatter.StageProgress.v1";
    private const int SaveVersion = 1;

    public static StageProgressService Instance { get; private set; }
    public static event Action<string, string> StageCompleted;
    public static event Action<string, string> StageFinished;

    private StageProgressSaveData saveData;
    private bool gameCompletionReported;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        StageCompleted = null;
        StageFinished = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void BeginStage(string activityId, string stageId)
    {
        if (!ValidateIds(activityId, stageId))
            return;

        EnsureInstance().BeginInternal(activityId, stageId);
    }

    public static bool CompleteStage(string activityId, string stageId)
    {
        if (!ValidateIds(activityId, stageId))
            return false;

        return EnsureInstance().CompleteInternal(activityId, stageId);
    }

    public static bool IsStageComplete(string activityId, string stageId)
    {
        if (!ValidateIds(activityId, stageId))
            return false;

        StageProgressRecord record = EnsureInstance().FindRecord(activityId, stageId);
        return record != null && record.completed;
    }

    public static bool IsStageUnlocked(string activityId, string stageId)
    {
        if (!ValidateIds(activityId, stageId))
            return false;

        if (!StageProgressIds.TryGetStageSequence(activityId, out IReadOnlyList<string> stages))
            return true;

        int stageIndex = -1;
        for (int i = 0; i < stages.Count; i++)
        {
            if (stages[i] == stageId)
            {
                stageIndex = i;
                break;
            }
        }

        if (stageIndex <= 0)
            return stageIndex == 0;

        return IsStageComplete(activityId, stages[stageIndex - 1]);
    }

    public static string GetNextIncompleteStage(string activityId)
    {
        if (!StageProgressIds.IsValidSegment(activityId))
            return null;

        if (!StageProgressIds.TryGetStageSequence(activityId, out IReadOnlyList<string> stages))
            return null;

        for (int i = 0; i < stages.Count; i++)
        {
            if (!IsStageComplete(activityId, stages[i]))
                return stages[i];
        }

        return null;
    }

    public static int GetAttempts(string activityId, string stageId)
    {
        if (!ValidateIds(activityId, stageId))
            return 0;

        StageProgressRecord record = EnsureInstance().FindRecord(activityId, stageId);
        return record != null ? record.attempts : 0;
    }

    public static string[] GetCompletedStageIds()
    {
        return EnsureInstance().BuildCompletedStageIds();
    }

    public static bool HasAnyProgress()
    {
        return EnsureInstance().HasAnyProgressInternal();
    }

    public static bool HasActivityProgress(string activityId)
    {
        if (!StageProgressIds.IsValidSegment(activityId))
            return false;

        return EnsureInstance().HasActivityProgressInternal(activityId);
    }

    public static bool IsGameComplete()
    {
        return EnsureInstance().IsGameCompleteInternal();
    }

    public static bool CanReportGameCompletion()
    {
        StageProgressService service = EnsureInstance();
        service.EnsureSaveData();
        return !service.gameCompletionReported && service.IsGameCompleteInternal();
    }

    public static bool ReportGameCompletion()
    {
        return EnsureInstance().ReportGameCompletionInternal();
    }

    public static void ResetAllProgress()
    {
        StageProgressService service = EnsureInstance();
        service.saveData = new StageProgressSaveData();
        service.gameCompletionReported = false;
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }

    private static StageProgressService EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        StageProgressService existing = FindAnyObjectByType<StageProgressService>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject serviceObject = new GameObject(nameof(StageProgressService));
        return serviceObject.AddComponent<StageProgressService>();
    }

    private static bool ValidateIds(string activityId, string stageId)
    {
        bool isValid = StageProgressIds.IsValidSegment(activityId)
            && StageProgressIds.IsValidSegment(stageId);

        if (!isValid)
            Debug.LogWarning($"[StageProgress] Invalid stage ID '{activityId}/{stageId}'. Use lowercase kebab case.");

        return isValid;
    }

    private void BeginInternal(string activityId, string stageId)
    {
        StageProgressRecord record = FindOrCreateRecord(activityId, stageId);
        record.attempts += 1;
        record.lastStartedAt = DateTime.UtcNow.ToString("O");
        Save();
        PostProgress();
    }

    private bool CompleteInternal(string activityId, string stageId)
    {
        StageProgressRecord record = FindOrCreateRecord(activityId, stageId);
        if (record.completed)
        {
            StageFinished?.Invoke(activityId, stageId);
            PostProgress();
            return false;
        }

        if (record.attempts == 0)
            record.attempts = 1;

        record.completed = true;
        record.completedAt = DateTime.UtcNow.ToString("O");
        Save();

        StageCompleted?.Invoke(activityId, stageId);
        StageFinished?.Invoke(activityId, stageId);
        PostProgress(record);
        return true;
    }

    private bool HasAnyProgressInternal()
    {
        EnsureSaveData();

        for (int i = 0; i < saveData.stages.Count; i++)
        {
            StageProgressRecord record = saveData.stages[i];
            if (record != null && (record.completed || record.attempts > 0))
                return true;
        }

        return false;
    }

    private bool HasActivityProgressInternal(string activityId)
    {
        EnsureSaveData();

        for (int i = 0; i < saveData.stages.Count; i++)
        {
            StageProgressRecord record = saveData.stages[i];
            if (record != null
                && record.activityId == activityId
                && (record.completed || record.attempts > 0))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsGameCompleteInternal()
    {
        IReadOnlyList<string> activities = StageProgressIds.Activities;
        for (int activityIndex = 0; activityIndex < activities.Count; activityIndex++)
        {
            if (!StageProgressIds.TryGetStageSequence(
                    activities[activityIndex],
                    out IReadOnlyList<string> stages)
                || stages.Count == 0)
            {
                return false;
            }

            for (int stageIndex = 0; stageIndex < stages.Count; stageIndex++)
            {
                StageProgressRecord record = FindRecord(activities[activityIndex], stages[stageIndex]);
                if (record == null || !record.completed)
                    return false;
            }
        }

        return activities.Count > 0;
    }

    private bool ReportGameCompletionInternal()
    {
        EnsureSaveData();

        if (!CanReportGameCompletion())
            return false;

        gameCompletionReported = true;

        var payload = new StageProgressSnapshotPayload
        {
            saveVersion = SaveVersion,
            completedStageIds = BuildCompletedStageIds(),
            gameCompleted = true
        };

        StageProgressWebBridge.Post(JsonUtility.ToJson(payload));
        return true;
    }

    private StageProgressRecord FindOrCreateRecord(string activityId, string stageId)
    {
        StageProgressRecord record = FindRecord(activityId, stageId);
        if (record != null)
            return record;

        record = new StageProgressRecord
        {
            activityId = activityId,
            stageId = stageId
        };
        saveData.stages.Add(record);
        return record;
    }

    private StageProgressRecord FindRecord(string activityId, string stageId)
    {
        EnsureSaveData();

        for (int i = 0; i < saveData.stages.Count; i++)
        {
            StageProgressRecord record = saveData.stages[i];
            if (record == null)
                continue;

            if (record.activityId == activityId && record.stageId == stageId)
                return record;
        }

        return null;
    }

    private string[] BuildCompletedStageIds()
    {
        EnsureSaveData();
        var completedIds = new List<string>();

        for (int i = 0; i < saveData.stages.Count; i++)
        {
            StageProgressRecord record = saveData.stages[i];
            if (record != null && record.completed)
                completedIds.Add(StageProgressIds.ToKey(record.activityId, record.stageId));
        }

        completedIds.Sort(StringComparer.Ordinal);
        return completedIds.ToArray();
    }

    private void Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            saveData = new StageProgressSaveData();
            return;
        }

        try
        {
            saveData = JsonUtility.FromJson<StageProgressSaveData>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[StageProgress] Could not read saved progress; starting clean. " + exception.Message);
            saveData = new StageProgressSaveData();
        }

        EnsureSaveData();
    }

    private void Save()
    {
        EnsureSaveData();
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saveData));
        PlayerPrefs.Save();
    }

    private void EnsureSaveData()
    {
        if (saveData == null)
            saveData = new StageProgressSaveData();

        if (saveData.stages == null)
            saveData.stages = new List<StageProgressRecord>();
    }

    private void PostProgress(StageProgressRecord completedRecord = null)
    {
        string[] completedStageIds = BuildCompletedStageIds();

        if (completedRecord == null)
        {
            var snapshotPayload = new StageProgressSnapshotPayload
            {
                saveVersion = SaveVersion,
                completedStageIds = completedStageIds
            };

            StageProgressWebBridge.Post(JsonUtility.ToJson(snapshotPayload));
            return;
        }

        var completionPayload = new StageProgressCompletionPayload
        {
            saveVersion = SaveVersion,
            completedStageIds = completedStageIds,
            stageCompleted = new StageCompletionPayload
            {
                activityId = completedRecord.activityId,
                stageId = completedRecord.stageId,
                attempts = completedRecord.attempts,
                completedAt = completedRecord.completedAt
            }
        };

        StageProgressWebBridge.Post(JsonUtility.ToJson(completionPayload));
    }
}

[Serializable]
public sealed class StageProgressSaveData
{
    public int saveVersion = 1;
    public List<StageProgressRecord> stages = new List<StageProgressRecord>();
}

[Serializable]
public sealed class StageProgressRecord
{
    public string activityId;
    public string stageId;
    public bool completed;
    public int attempts;
    public string lastStartedAt;
    public string completedAt;
}

[Serializable]
public sealed class StageProgressSnapshotPayload
{
    public int saveVersion;
    public string[] completedStageIds;
    public bool gameCompleted;
}

[Serializable]
public sealed class StageProgressCompletionPayload
{
    public int saveVersion;
    public string[] completedStageIds;
    public bool gameCompleted;
    public StageCompletionPayload stageCompleted;
}

[Serializable]
public sealed class StageCompletionPayload
{
    public string activityId;
    public string stageId;
    public int attempts;
    public string completedAt;
}
