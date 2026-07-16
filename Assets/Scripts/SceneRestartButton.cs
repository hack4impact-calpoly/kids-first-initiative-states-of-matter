using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class SceneRestartButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(RestartScene);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(RestartScene);
    }

    public void RestartScene()
    {
        button.interactable = false;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }
}
