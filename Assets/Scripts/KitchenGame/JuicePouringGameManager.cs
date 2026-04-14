using UnityEngine;
using UnityEngine.SceneManagement;

public class JuicePouringGameManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private MockPotController fridge;

    [Header("Scene Transition")]
    [SerializeField] private string nextSceneName = "Kitchen Game - Freezing Pour";
    [SerializeField] private float sceneLoadDelay = 0.5f;

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
        Invoke(nameof(LoadNextScene), sceneLoadDelay);
    }

    private void LoadNextScene()
    {
        Debug.Log("LoadNextScene called");
        Debug.Log("Trying to load scene: " + nextSceneName);

        SceneManager.LoadScene(nextSceneName);
    }
}