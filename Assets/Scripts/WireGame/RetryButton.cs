using UnityEngine;
using UnityEngine.SceneManagement;

public class RetryButton : MonoBehaviour
{
    public void OnRetryButtonPressed()
    {
        // Reload scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}