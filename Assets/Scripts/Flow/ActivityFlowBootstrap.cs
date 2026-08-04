using UnityEngine;
using UnityEngine.SceneManagement;

public static class ActivityFlowBootstrap
{
    private static bool installed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        installed = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (installed)
            return;

        installed = true;
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (Object.FindAnyObjectByType<ActivityFlowRuntimeHost>() == null)
        {
            var hostObject = new GameObject("Activity Flow Runtime");
            Object.DontDestroyOnLoad(hostObject);
            hostObject.AddComponent<ActivityFlowRuntimeHost>();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConfigureInitialScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.isLoaded)
            OnSceneLoaded(activeScene, LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!Application.isPlaying || mode != LoadSceneMode.Single)
            return;

        CreateController<SceneVisualPolishController>("Scene Visual Polish");

        if (scene.name == ActivityFlowCatalog.TitleScene)
        {
            CreateController<StatesMenuFlowController>("States Menu Flow");
            return;
        }

        if (scene.name == ActivityFlowCatalog.SelectorScene)
        {
            CreateController<GameSelectorFlowController>("Game Selector Flow");
            return;
        }

        if (!string.IsNullOrEmpty(ActivityFlowCatalog.GetActivityForScene(scene.name)))
            CreateController<ActivityFlowController>("Activity Flow");
    }

    public static void EnsureCurrentScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.isLoaded)
            OnSceneLoaded(activeScene, LoadSceneMode.Single);
    }

    private static void CreateController<T>(string objectName) where T : MonoBehaviour
    {
        if (Object.FindAnyObjectByType<T>() != null)
            return;

        T controller = new GameObject(objectName).AddComponent<T>();
        if (controller is IFlowSceneController flowController)
            flowController.InitializeFlow();
    }
}
