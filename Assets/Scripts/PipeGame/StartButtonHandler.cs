using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButtonHandler : MonoBehaviour
{
    public PipeUIController ui;
    public GameObject startButton;

    [Header("Success Cutscene")]
    [SerializeField] private bool playSuccessCutscene = true;
    [SerializeField] private CutsceneDefinition successCutsceneDefinition;
    [SerializeField] private CutsceneManager cutsceneManager;
    [SerializeField] private StateChangeCutsceneAnimation successCutscene;
    [SerializeField] private Transform cutsceneTargetOverride;

    [Header("Dialogue Flow")]
    [SerializeField] private bool createDialogueAdapterIfMissing = true;
    [SerializeField] private PipeGameDialogueAdapter dialogueAdapter;

    public event Action StartPressed;
    public event Action<MatterCutsceneKind> ValidationSucceeded;
    public event Action ValidationFailed;

    private PipeObject lastEndPipe;

    private void Awake()
    {
        ResolveDialogueAdapter();
    }

    public void OnStartPressed()
    {
        if (startButton != null)
            startButton.SetActive(false);

        StartPressed?.Invoke();

        if (ui == null)
        {
            Debug.LogError("PipeUIController not assigned.");
            return;
        }

        bool success = CheckEndWater();

        if (success)
        {
            MatterCutsceneKind cutsceneKind = ResolveCutsceneKind();
            ValidationSucceeded?.Invoke(cutsceneKind);

            if (TryPlaySuccessCutscene(cutsceneKind))
                return;

            ui.ShowSuccess();
        }
        else
        {
            ValidationFailed?.Invoke();
            ui.ShowFailure();
        }
    }

    private bool CheckEndWater()
    {
        PipeObject[] pipes = FindObjectsByType<PipeObject>(FindObjectsSortMode.None);

        if (pipes.Length == 0)
        {
            Debug.LogWarning("No PipeObjects found.");
            return false;
        }

        // Make sure water state is up to date
        pipes[0].recalculateWater();

        foreach (PipeObject pipe in pipes)
        {
            if (pipe.isEnd)
            {
                Debug.Log("End pipe found. Water = " + pipe.water);
                lastEndPipe = pipe;
                return pipe.water;
            }
        }

        Debug.LogWarning("No PipeObject with isEnd == true found.");
        return false;
    }

    private bool TryPlaySuccessCutscene(MatterCutsceneKind cutsceneKind)
    {
        if (!playSuccessCutscene)
            return false;

        CutsceneManager manager = ResolveCutsceneManager();
        StateChangeCutsceneAnimation animation = ResolveSuccessCutscene();

        if (manager == null || animation == null)
            return false;

        animation.Configure(cutsceneKind);
        Transform target = ResolveCutsceneTarget();

        if (successCutsceneDefinition != null)
            return manager.TryPlay(successCutsceneDefinition, target, (ICutsceneAnimation)animation, ShowSuccess);

        return manager.TryPlay(target, (ICutsceneAnimation)animation, ShowSuccess);
    }

    private MatterCutsceneKind ResolveCutsceneKind()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.Contains("Frozen"))
            return MatterCutsceneKind.PipeFreezing;

        PipeObject[] pipes = FindObjectsByType<PipeObject>(FindObjectsSortMode.None);
        for (int i = 0; i < pipes.Length; i++)
        {
            if (pipes[i].isSink || pipes[i].isFrozen)
                return MatterCutsceneKind.PipeFreezing;
        }

        return MatterCutsceneKind.PipeWaterFlow;
    }

    private Transform ResolveCutsceneTarget()
    {
        if (cutsceneTargetOverride != null)
            return cutsceneTargetOverride;

        return lastEndPipe != null ? lastEndPipe.transform : transform;
    }

    private CutsceneManager ResolveCutsceneManager()
    {
        if (cutsceneManager != null)
            return cutsceneManager;

        cutsceneManager = FindAnyObjectByType<CutsceneManager>();

        if (cutsceneManager == null)
            cutsceneManager = gameObject.AddComponent<CutsceneManager>();

        return cutsceneManager;
    }

    private StateChangeCutsceneAnimation ResolveSuccessCutscene()
    {
        if (successCutscene != null)
            return successCutscene;

        successCutscene = GetComponent<StateChangeCutsceneAnimation>();

        if (successCutscene == null)
            successCutscene = gameObject.AddComponent<StateChangeCutsceneAnimation>();

        return successCutscene;
    }

    private void ShowSuccess()
    {
        if (ui != null)
            ui.ShowSuccess();
    }

    private void ResolveDialogueAdapter()
    {
        if (dialogueAdapter == null)
            dialogueAdapter = FindAnyObjectByType<PipeGameDialogueAdapter>(FindObjectsInactive.Include);

        if (dialogueAdapter == null && createDialogueAdapterIfMissing)
            dialogueAdapter = gameObject.AddComponent<PipeGameDialogueAdapter>();

        if (dialogueAdapter != null)
            dialogueAdapter.Initialize(this);
    }
}
