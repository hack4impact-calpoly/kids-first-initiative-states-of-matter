using UnityEngine;
using UnityEngine.SceneManagement;

public class CoolingStationLoader : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "Kitchen Game - Freezing";

    private void OnTriggerEnter2D(Collider2D other)
    {
        var ingredient = other.GetComponentInParent<IngredientInstance>();

        if (ingredient != null)
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
