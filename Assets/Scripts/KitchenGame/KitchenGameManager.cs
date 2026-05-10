using UnityEngine;

public class KitchenGameManager : MonoBehaviour
{
    public enum GameState { Playing, Won, Failed }

    [Header("Config")]
    [SerializeField] private KitchenLevelConfigSO levelConfig;

    [Header("Scene References")]
    [SerializeField] private HeatController heatSource;
    [SerializeField] private MockPotController pot;
    [SerializeField] private GameObject winText;
    [SerializeField] private GameObject failText;

    [Header("Win Cutscene")]
    [SerializeField] private bool playChocolateMeltCutsceneOnWin = true;
    [SerializeField] private CutsceneDefinition chocolateMeltCutsceneDefinition;
    [SerializeField] private CutsceneManager cutsceneManager;
    [SerializeField] private StateChangeCutsceneAnimation chocolateMeltCutscene;
    [SerializeField] private Transform chocolateCutsceneTargetOverride;

    public GameState State { get; private set; } = GameState.Playing;

    private bool ingredientAdded;
    private bool maxHeatReached;
    private Transform requiredIngredientTransform;

    private void Awake()
    {
        SetWin(false);
        SetFail(false);
        State = GameState.Playing;

        ingredientAdded = false;
        maxHeatReached = false;
        requiredIngredientTransform = null;
    }

    private void OnEnable()
    {
        if (pot != null) pot.IngredientAdded += OnIngredientAdded;
        if (heatSource != null) heatSource.HeatChanged += OnHeatChanged;
    }

    private void OnDisable()
    {
        if (pot != null) pot.IngredientAdded -= OnIngredientAdded;
        if (heatSource != null) heatSource.HeatChanged -= OnHeatChanged;
    }

    private void OnIngredientAdded(IngredientSO ing)
    {
        if (State != GameState.Playing || levelConfig == null) return;

        // Only care about the required ingredient for this level
        if (ing != null && ing == levelConfig.requiredIngredient)
        {
            ingredientAdded = true;
            requiredIngredientTransform = pot != null ? pot.LastAddedIngredientTransform : null;
        }

        Evaluate();
    }

    private void OnHeatChanged(float _)
    {
        if (State != GameState.Playing || levelConfig == null) return;

        if (heatSource != null && heatSource.IsMaxHeat)
            maxHeatReached = true;

        Evaluate();
    }

    private void Evaluate()
    {
        // Fail if they hit max heat before adding the required ingredient
        if (levelConfig.failIfMaxHeatBeforeIngredient && maxHeatReached && !ingredientAdded)
        {
            Fail();
            return;
        }

        // Win if required ingredient is added and max heat is reached
        if (ingredientAdded && (!levelConfig.requireMaxHeat || maxHeatReached))
        {
            Win();
        }
    }

    private void Win()
    {
        State = GameState.Won;
        SetFail(false);

        if (TryPlayChocolateMeltCutscene())
        {
            SetWin(false);
            return;
        }

        SetWin(true);
    }

    private void Fail()
    {
        State = GameState.Failed;
        SetFail(true);
        SetWin(false);
    }

    private void SetWin(bool on) { if (winText != null) winText.SetActive(on); }
    private void SetFail(bool on) { if (failText != null) failText.SetActive(on); }

    private bool TryPlayChocolateMeltCutscene()
    {
        if (!playChocolateMeltCutsceneOnWin)
            return false;

        CutsceneManager manager = ResolveCutsceneManager();
        StateChangeCutsceneAnimation animation = ResolveChocolateMeltCutscene();

        if (manager == null || animation == null)
            return false;

        animation.Configure(MatterCutsceneKind.ChocolateMelting);
        Transform focusTarget = ResolveChocolateCutsceneTarget();
        if (chocolateMeltCutsceneDefinition != null)
            return manager.TryPlay(chocolateMeltCutsceneDefinition, focusTarget, (ICutsceneAnimation)animation, OnChocolateMeltCutsceneFinished);

        return manager.TryPlay(focusTarget, (ICutsceneAnimation)animation, OnChocolateMeltCutsceneFinished);
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

    private StateChangeCutsceneAnimation ResolveChocolateMeltCutscene()
    {
        if (chocolateMeltCutscene != null)
            return chocolateMeltCutscene;

        chocolateMeltCutscene = GetComponent<StateChangeCutsceneAnimation>();

        if (chocolateMeltCutscene == null)
            chocolateMeltCutscene = gameObject.AddComponent<StateChangeCutsceneAnimation>();

        return chocolateMeltCutscene;
    }

    private Transform ResolveChocolateCutsceneTarget()
    {
        if (chocolateCutsceneTargetOverride != null)
            return chocolateCutsceneTargetOverride;

        if (requiredIngredientTransform != null)
            return requiredIngredientTransform;

        return pot != null ? pot.transform : null;
    }

    private void OnChocolateMeltCutsceneFinished()
    {
        SetWin(true);
    }
}
