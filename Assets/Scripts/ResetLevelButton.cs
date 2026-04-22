using UnityEngine;
using UnityEngine.SceneManagement;

public class ResetLevelButton : MonoBehaviour
{
    public void ResetLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}