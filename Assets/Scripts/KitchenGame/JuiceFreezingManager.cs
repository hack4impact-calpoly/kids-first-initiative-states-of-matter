using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class JuiceFreezingManager : MonoBehaviour
{
    public enum GameState { Playing, Won }

    [Header("Scene References")]
    [SerializeField] private JuiceCoolingController juiceCoolingController;
    [SerializeField] private IceTray tray;
    [SerializeField] private GameObject winText;

    [Header("Scene Transition")]
    [SerializeField] private bool loadNextSceneOnTrayFull = true;
    [SerializeField] private string nextSceneName = "Kitchen Game - Freezing Station";
    [SerializeField] private float sceneLoadDelay = 0.5f;
    [SerializeField] private bool requireColdEnough = false;

    [Header("Tray Full Cutscene")]
    [FormerlySerializedAs("playFreezingCutsceneOnWin")]
    [SerializeField] private bool playLiquidFlowCutsceneOnTrayFull = true;
    [FormerlySerializedAs("freezingCutsceneDefinition")]
    [SerializeField] private CutsceneDefinition liquidFlowCutsceneDefinition;
    [SerializeField] private CutsceneManager cutsceneManager;
    [FormerlySerializedAs("freezingCutscene")]
    [SerializeField] private StateChangeCutsceneAnimation liquidFlowCutscene;
    [FormerlySerializedAs("freezingCutsceneTargetOverride")]
    [SerializeField] private Transform liquidFlowCutsceneTargetOverride;

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
        if (!trayFilled)
            return;

        if (requireColdEnough && !coldEnough)
            return;

        if (loadNextSceneOnTrayFull)
        {
            CompletePourStep();
            return;
        }

        Win();
    }

    private void CompletePourStep()
    {
        State = GameState.Won;
        SetWin(false);

        if (TryPlayLiquidFlowCutscene(LoadNextScene))
        {
            Debug.Log("Pour Step Complete!");
            return;
        }

        Invoke(nameof(LoadNextScene), sceneLoadDelay);
        Debug.Log("Pour Step Complete!");
    }

    private void Win()
    {
        State = GameState.Won;
        SetWin(true);
        Debug.Log("Freezing Level Complete!");
    }

    private void SetWin(bool on) { if (winText != null) winText.SetActive(on); }

    private bool TryPlayLiquidFlowCutscene(System.Action finished)
    {
        if (!playLiquidFlowCutsceneOnTrayFull)
            return false;

        CutsceneManager manager = ResolveCutsceneManager();
        StateChangeCutsceneAnimation animation = ResolveLiquidFlowCutscene();

        if (manager == null || animation == null)
            return false;

        animation.Configure(MatterCutsceneKind.LiquidFlow);
        Transform target = liquidFlowCutsceneTargetOverride != null ? liquidFlowCutsceneTargetOverride : tray != null ? tray.transform : transform;

        if (liquidFlowCutsceneDefinition != null)
            return manager.TryPlay(liquidFlowCutsceneDefinition, target, (ICutsceneAnimation)animation, finished);

        return manager.TryPlay(target, (ICutsceneAnimation)animation, finished);
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

    private StateChangeCutsceneAnimation ResolveLiquidFlowCutscene()
    {
        if (liquidFlowCutscene != null)
            return liquidFlowCutscene;

        liquidFlowCutscene = GetComponent<StateChangeCutsceneAnimation>();

        if (liquidFlowCutscene == null)
            liquidFlowCutscene = gameObject.AddComponent<StateChangeCutsceneAnimation>();

        return liquidFlowCutscene;
    }

    private void LoadNextScene()
    {
        if (!string.IsNullOrWhiteSpace(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }
}
