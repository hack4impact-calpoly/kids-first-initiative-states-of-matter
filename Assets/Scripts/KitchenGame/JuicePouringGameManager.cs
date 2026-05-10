using UnityEngine;
using UnityEngine.SceneManagement;

public class JuicePouringGameManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private MockPotController fridge;

    [Header("Scene Transition")]
    [SerializeField] private string nextSceneName = "Kitchen Game - Freezing Pour";
    [SerializeField] private float sceneLoadDelay = 0.5f;

    [Header("Transition Cutscene")]
    [SerializeField] private bool playCoolingStationCutscene = true;
    [SerializeField] private CutsceneDefinition coolingStationCutsceneDefinition;
    [SerializeField] private CutsceneManager cutsceneManager;
    [SerializeField] private StateChangeCutsceneAnimation coolingStationCutscene;
    [SerializeField] private Transform coolingStationCutsceneTargetOverride;

    private bool isLoadingScene = false;

    private void OnEnable()
    {
        Debug.Log("JuicePouringGameManager OnEnable");

        if (fridge != null)
        {
            Debug.Log("Subscribed to fridge IngredientAdded");
            fridge.IngredientAdded += OnIngredientAdded;
        }
        else
        {
            Debug.Log("fridge is NULL");
        }
    }

    private void OnDisable()
    {
        if (fridge != null)
            fridge.IngredientAdded -= OnIngredientAdded;
    }

    private void OnIngredientAdded(IngredientSO ing)
    {
        Debug.Log("OnIngredientAdded fired");
        Debug.Log("Ingredient: " + (ing != null ? ing.name : "NULL"));
        Debug.Log("nextSceneName = " + nextSceneName);

        if (isLoadingScene) return;

        isLoadingScene = true;
        if (TryPlayCoolingStationCutscene())
            return;

        Invoke(nameof(LoadNextScene), sceneLoadDelay);
    }

    private void LoadNextScene()
    {
        Debug.Log("LoadNextScene called");
        Debug.Log("Trying to load scene: " + nextSceneName);

        SceneManager.LoadScene(nextSceneName);
    }

    private bool TryPlayCoolingStationCutscene()
    {
        if (!playCoolingStationCutscene)
            return false;

        CutsceneManager manager = ResolveCutsceneManager();
        StateChangeCutsceneAnimation animation = ResolveCoolingStationCutscene();

        if (manager == null || animation == null)
            return false;

        animation.Configure(MatterCutsceneKind.LiquidFlow);
        Transform target = ResolveCutsceneTarget();

        if (coolingStationCutsceneDefinition != null)
            return manager.TryPlay(coolingStationCutsceneDefinition, target, (ICutsceneAnimation)animation, LoadNextScene);

        return manager.TryPlay(target, (ICutsceneAnimation)animation, LoadNextScene);
    }

    private Transform ResolveCutsceneTarget()
    {
        if (coolingStationCutsceneTargetOverride != null)
            return coolingStationCutsceneTargetOverride;

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

    private StateChangeCutsceneAnimation ResolveCoolingStationCutscene()
    {
        if (coolingStationCutscene != null)
            return coolingStationCutscene;

        coolingStationCutscene = GetComponent<StateChangeCutsceneAnimation>();

        if (coolingStationCutscene == null)
            coolingStationCutscene = gameObject.AddComponent<StateChangeCutsceneAnimation>();

        return coolingStationCutscene;
    }
}
