using UnityEngine;
using UnityEngine.SceneManagement;

public class PipeGameDialogueAdapter : DialogueFlowAdapterBase
{
    public const string WaterIntroKey = "pipe.water_flow.intro";
    public const string WaterHintKey = "pipe.water_flow.hint";
    public const string WaterActiveKey = "pipe.water_flow.active";
    public const string WaterWinKey = "pipe.water_flow.win";
    public const string FrozenIntroKey = "pipe.frozen_flow.intro";
    public const string FrozenSolidHintKey = "pipe.frozen_flow.solid_hint";
    public const string FrozenWinKey = "pipe.frozen_flow.win";
    public const string FailureKey = "pipe.flow.failure";

    [SerializeField] private StartButtonHandler startButtonHandler;
    [SerializeField] private bool playIntroOnStart = true;

    private StartButtonHandler subscribedStartButtonHandler;
    private bool defaultsRegistered;
    private bool introPlayed;

    private void Awake()
    {
        ResolveStartButtonHandler();
        EnsureFlowController();
        RegisterDefaultFlowsIfNeeded();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        PlayIntroIfNeeded();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Initialize(StartButtonHandler handler)
    {
        startButtonHandler = handler;
        EnsureFlowController();
        RegisterDefaultFlowsIfNeeded();
        Subscribe();
    }

    public void PlayIntroIfNeeded()
    {
        if (!playIntroOnStart || introPlayed)
            return;

        string introKey = IsFrozenLevel() ? FrozenIntroKey : WaterIntroKey;
        if (TryPlayFlow(introKey))
            introPlayed = true;
    }

    private void Subscribe()
    {
        ResolveStartButtonHandler();
        Unsubscribe();

        if (startButtonHandler != null)
        {
            startButtonHandler.StartPressed += OnStartPressed;
            startButtonHandler.ValidationSucceeded += OnValidationSucceeded;
            startButtonHandler.ValidationFailed += OnValidationFailed;
            subscribedStartButtonHandler = startButtonHandler;
        }

        RotateOnClick.PipeRotated += OnPipeRotated;
        FreezeOnClick.PipeFreezeToggled += OnPipeFreezeToggled;
    }

    private void Unsubscribe()
    {
        if (subscribedStartButtonHandler != null)
        {
            subscribedStartButtonHandler.StartPressed -= OnStartPressed;
            subscribedStartButtonHandler.ValidationSucceeded -= OnValidationSucceeded;
            subscribedStartButtonHandler.ValidationFailed -= OnValidationFailed;
            subscribedStartButtonHandler = null;
        }

        RotateOnClick.PipeRotated -= OnPipeRotated;
        FreezeOnClick.PipeFreezeToggled -= OnPipeFreezeToggled;
    }

    private void OnStartPressed()
    {
        TryPlayFlow(IsFrozenLevel() ? FrozenSolidHintKey : WaterActiveKey);
    }

    private void OnValidationSucceeded(MatterCutsceneKind cutsceneKind)
    {
        TryPlayFlow(cutsceneKind == MatterCutsceneKind.PipeFreezing ? FrozenWinKey : WaterWinKey);
    }

    private void OnValidationFailed()
    {
        TryPlayFlowNow(FailureKey);
    }

    private void OnPipeRotated(PipeObject pipe)
    {
        if (!IsFrozenLevel())
            TryPlayFlow(WaterHintKey);
    }

    private void OnPipeFreezeToggled(PipeObject pipe)
    {
        if (IsFrozenLevel())
            TryPlayFlow(FrozenSolidHintKey);
    }

    private void RegisterDefaultFlowsIfNeeded()
    {
        if (!registerDefaultFlows || defaultsRegistered)
            return;

        EnsureFlowController();
        if (flowController == null)
            return;

        DialogueSpeaker lucy = ResolveSpeaker("Lucy");
        DialogueSpeaker gary = ResolveSpeaker("Gary");
        DialogueSpeaker sam = ResolveSpeaker("Sam");

        RegisterPipeLine(WaterIntroKey, "pipe.water_flow.intro.1", lucy, "Liquid water flows through open, connected pipes.", "liquid");
        RegisterPipeLine(WaterHintKey, "pipe.water_flow.hint.1", lucy, "Rotate pipes so openings connect into one path.", "liquid");
        RegisterPipeLine(WaterActiveKey, "pipe.water_flow.active.1", gary, "Water flows only where pipe openings touch.", "liquid");
        RegisterPipeLine(WaterWinKey, "pipe.water_flow.win.1", lucy, "Connected pipes let liquid water reach the end.", "liquid", "state-change");
        RegisterPipeLine(FrozenIntroKey, "pipe.frozen_flow.intro.1", sam, "Freeze water into solid ice to guide the liquid.", "solid", "liquid", "freezing");
        RegisterPipeLine(FrozenSolidHintKey, "pipe.frozen_flow.solid_hint.1", sam, "Solid ice does not flow, so it blocks leaks.", "solid", "freezing");
        RegisterPipeLine(FrozenWinKey, "pipe.frozen_flow.win.1", sam, "Solid ice and liquid water worked together.", "solid", "liquid", "state-change");
        RegisterPipeLine(FailureKey, "pipe.flow.failure.1", lucy, "The liquid path is broken. Turn or freeze a pipe.", "state-change", playOnce: false);

        defaultsRegistered = true;
    }

    private void RegisterPipeLine(
        string key,
        string lineId,
        DialogueSpeaker speaker,
        string text,
        params string[] tags)
    {
        RegisterLine(key, lineId, speaker, text, tags);
    }

    private void RegisterPipeLine(
        string key,
        string lineId,
        DialogueSpeaker speaker,
        string text,
        string tag,
        bool playOnce)
    {
        RegisterLine(key, lineId, speaker, text, new[] { tag }, playOnce);
    }

    private bool IsFrozenLevel()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrWhiteSpace(sceneName) && sceneName.Contains("Frozen"))
            return true;

        return FindAnyObjectByType<FrozenFlowValidator>() != null;
    }

    private void ResolveStartButtonHandler()
    {
        if (startButtonHandler == null)
            startButtonHandler = FindAnyObjectByType<StartButtonHandler>();
    }
}
