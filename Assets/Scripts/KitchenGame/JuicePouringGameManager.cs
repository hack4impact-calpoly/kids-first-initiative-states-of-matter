using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class JuicePouringGameManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private MockPotController fridge;
    [SerializeField] private JuiceCoolingController juiceCoolingController;
    [SerializeField] private bool requireColdEnough = true;

    [Header("Scene Transition")]
    [SerializeField] private string nextSceneName = "States of Matter Menu";
    [SerializeField] private float sceneLoadDelay = 0.5f;

    [Header("Freezing Cutscene")]
    [FormerlySerializedAs("playCoolingStationCutscene")]
    [SerializeField] private bool playFreezingCutscene = true;
    [FormerlySerializedAs("coolingStationCutsceneDefinition")]
    [SerializeField] private CutsceneDefinition freezingCutsceneDefinition;
    [SerializeField] private CutsceneManager cutsceneManager;
    [FormerlySerializedAs("coolingStationCutscene")]
    [SerializeField] private StateChangeCutsceneAnimation freezingCutscene;
    [FormerlySerializedAs("coolingStationCutsceneTargetOverride")]
    [SerializeField] private Transform freezingCutsceneTargetOverride;

    private bool ingredientInFridge;
    private bool isCompletingStep;

    private void OnEnable()
    {
        Debug.Log("JuicePouringGameManager OnEnable");

        if (fridge != null)
        {
            Debug.Log("Subscribed to fridge IngredientAdded");
            fridge.IngredientAdded += OnIngredientAdded;
            fridge.IngredientRemoved += OnIngredientRemoved;
        }
        else
        {
            Debug.Log("fridge is NULL");
        }
    }

    private void OnDisable()
    {
        if (fridge != null)
        {
            fridge.IngredientAdded -= OnIngredientAdded;
            fridge.IngredientRemoved -= OnIngredientRemoved;
        }
    }

    private void OnIngredientAdded(IngredientSO ing)
    {
        Debug.Log("OnIngredientAdded fired");
        Debug.Log("Ingredient: " + (ing != null ? ing.name : "NULL"));
        Debug.Log("nextSceneName = " + nextSceneName);

        if (isCompletingStep) return;

        ingredientInFridge = true;
        EvaluateCompletion();
    }

    private void OnIngredientRemoved(IngredientSO ing)
    {
        Debug.Log("OnIngredientRemoved fired");
        ingredientInFridge = IsIngredientCurrentlyInFridge();
    }

    private void Update()
    {
        EvaluateCompletion();
    }

    private void EvaluateCompletion()
    {
        if (isCompletingStep || !IsIngredientCurrentlyInFridge())
            return;

        if (requireColdEnough && !IsColdEnough())
            return;

        isCompletingStep = true;
        if (TryPlayFreezingCutscene())
            return;

        CompleteFreezingStep();
    }

    private bool IsIngredientCurrentlyInFridge()
    {
        if (fridge == null)
            return ingredientInFridge;

        ingredientInFridge = fridge.HasIngredients;
        return ingredientInFridge;
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
            return;

        Debug.Log("LoadNextScene called");
        Debug.Log("Trying to load scene: " + nextSceneName);

        SceneManager.LoadScene(nextSceneName);
    }

    private void CompleteFreezingStep()
    {
        Debug.Log("Freezing Station Complete!");

        if (!string.IsNullOrWhiteSpace(nextSceneName))
            Invoke(nameof(LoadNextScene), sceneLoadDelay);
    }

    private bool IsColdEnough()
    {
        JuiceCoolingController controller = ResolveJuiceCoolingController();

        if (controller == null)
        {
            Debug.LogWarning("Freezing station has no JuiceCoolingController; completing without a freeze slider.");
            return true;
        }

        return controller.IsColdEnough;
    }

    private bool TryPlayFreezingCutscene()
    {
        if (!playFreezingCutscene)
            return false;

        CutsceneManager manager = ResolveCutsceneManager();
        StateChangeCutsceneAnimation animation = ResolveFreezingCutscene();

        if (manager == null || animation == null)
            return false;

        animation.Configure(MatterCutsceneKind.LiquidFreezing);
        Transform target = ResolveCutsceneTarget();

        if (freezingCutsceneDefinition != null)
            return manager.TryPlay(freezingCutsceneDefinition, target, (ICutsceneAnimation)animation, CompleteFreezingStep);

        return manager.TryPlay(target, (ICutsceneAnimation)animation, CompleteFreezingStep);
    }

    private Transform ResolveCutsceneTarget()
    {
        if (freezingCutsceneTargetOverride != null)
            return freezingCutsceneTargetOverride;

        if (fridge != null && fridge.LastAddedIngredientTransform != null)
            return fridge.LastAddedIngredientTransform;

        return fridge != null ? fridge.transform : transform;
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

    private JuiceCoolingController ResolveJuiceCoolingController()
    {
        if (juiceCoolingController != null)
            return juiceCoolingController;

        juiceCoolingController = FindAnyObjectByType<JuiceCoolingController>();
        return juiceCoolingController;
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
}
