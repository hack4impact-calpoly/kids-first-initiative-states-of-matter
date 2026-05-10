using UnityEngine;
using UnityEngine.SceneManagement;

public class CoolingStationLoader : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "Kitchen Game - Freezing";
    [SerializeField] private bool playCoolingStationCutscene = true;
    [SerializeField] private CutsceneDefinition coolingStationCutsceneDefinition;
    [SerializeField] private CutsceneManager cutsceneManager;
    [SerializeField] private StateChangeCutsceneAnimation coolingStationCutscene;
    [SerializeField] private Transform coolingStationCutsceneTargetOverride;

    private bool isLoadingScene;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isLoadingScene)
            return;

        var ingredient = other.GetComponentInParent<IngredientInstance>();

        if (ingredient != null)
        {
            isLoadingScene = true;
            if (!TryPlayCoolingStationCutscene(ingredient.transform))
                LoadNextScene();
        }
    }

    private bool TryPlayCoolingStationCutscene(Transform ingredientTransform)
    {
        if (!playCoolingStationCutscene)
            return false;

        CutsceneManager manager = ResolveCutsceneManager();
        StateChangeCutsceneAnimation animation = ResolveCoolingStationCutscene();

        if (manager == null || animation == null)
            return false;

        animation.Configure(MatterCutsceneKind.LiquidFlow);
        Transform target = coolingStationCutsceneTargetOverride != null ? coolingStationCutsceneTargetOverride : ingredientTransform != null ? ingredientTransform : transform;

        if (coolingStationCutsceneDefinition != null)
            return manager.TryPlay(coolingStationCutsceneDefinition, target, (ICutsceneAnimation)animation, LoadNextScene);

        return manager.TryPlay(target, (ICutsceneAnimation)animation, LoadNextScene);
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

    private void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
