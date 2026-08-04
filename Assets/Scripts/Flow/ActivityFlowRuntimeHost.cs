using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ActivityFlowRuntimeHost : MonoBehaviour
{
    private int lastSceneHandle = -1;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ActivityFlowBootstrap.EnsureCurrentScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded || activeScene.handle == lastSceneHandle)
            return;

        lastSceneHandle = activeScene.handle;
        ActivityFlowBootstrap.EnsureCurrentScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        lastSceneHandle = scene.handle;
        ActivityFlowBootstrap.EnsureCurrentScene();
    }
}
