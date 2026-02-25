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

    public GameState State { get; private set; } = GameState.Playing;

    private bool ingredientAdded;
    private bool maxHeatReached;

    private void Awake()
    {
        SetWin(false);
        SetFail(false);
        State = GameState.Playing;

        ingredientAdded = false;
        maxHeatReached = false;
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
            ingredientAdded = true;

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
        SetWin(true);
        SetFail(false);
    }

    private void Fail()
    {
        State = GameState.Failed;
        SetFail(true);
        SetWin(false);
    }

    private void SetWin(bool on) { if (winText != null) winText.SetActive(on); }
    private void SetFail(bool on) { if (failText != null) failText.SetActive(on); }
}