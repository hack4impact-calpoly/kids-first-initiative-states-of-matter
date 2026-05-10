using UnityEngine;

public class JuiceFreezingManager : MonoBehaviour
{
    public enum GameState { Playing, Won }

    [Header("Scene References")]
    [SerializeField] private JuiceCoolingController juiceCoolingController;
    [SerializeField] private IceTray tray;
    [SerializeField] private GameObject winText;

    [Header("Win Cutscene")]
    [SerializeField] private bool playFreezingCutsceneOnWin = true;
    [SerializeField] private CutsceneDefinition freezingCutsceneDefinition;
    [SerializeField] private CutsceneManager cutsceneManager;
    [SerializeField] private StateChangeCutsceneAnimation freezingCutscene;
    [SerializeField] private Transform freezingCutsceneTargetOverride;

    public GameState State { get; private set; } = GameState.Playing;

    private bool trayFilled;
    private bool coldEnough;

    private void Awake()
    {
        SetWin(false);
        State = GameState.Playing;
    }

    private void Update()
    {
        if (State != GameState.Playing) return;

        if (tray != null)
            trayFilled = tray.IsFull();

        if (juiceCoolingController != null)
            coldEnough = juiceCoolingController.IsColdEnough;

        Evaluate();
    }

    private void Evaluate()
    {
        if (trayFilled && coldEnough)
        {
            Win();
        }
    }

    private void Win()
    {
        State = GameState.Won;
        if (TryPlayFreezingCutscene())
        {
            SetWin(false);
            Debug.Log("Freezing Level Complete!");
            return;
        }

        SetWin(true);
        Debug.Log("Freezing Level Complete!");
    }

    private void SetWin(bool on) { if (winText != null) winText.SetActive(on); }

    private bool TryPlayFreezingCutscene()
    {
        if (!playFreezingCutsceneOnWin)
            return false;

        CutsceneManager manager = ResolveCutsceneManager();
        StateChangeCutsceneAnimation animation = ResolveFreezingCutscene();

        if (manager == null || animation == null)
            return false;

        animation.Configure(MatterCutsceneKind.LiquidFreezing);
        Transform target = freezingCutsceneTargetOverride != null ? freezingCutsceneTargetOverride : tray != null ? tray.transform : transform;

        if (freezingCutsceneDefinition != null)
            return manager.TryPlay(freezingCutsceneDefinition, target, (ICutsceneAnimation)animation, OnFreezingCutsceneFinished);

        return manager.TryPlay(target, (ICutsceneAnimation)animation, OnFreezingCutsceneFinished);
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

    private StateChangeCutsceneAnimation ResolveFreezingCutscene()
    {
        if (freezingCutscene != null)
            return freezingCutscene;

        freezingCutscene = GetComponent<StateChangeCutsceneAnimation>();

        if (freezingCutscene == null)
            freezingCutscene = gameObject.AddComponent<StateChangeCutsceneAnimation>();

        return freezingCutscene;
    }

    private void OnFreezingCutsceneFinished()
    {
        SetWin(true);
    }
}
