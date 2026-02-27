using UnityEngine;

public class QuitButton : MonoBehaviour
{
    public GameObject menuPanel;

    public void QuitGame()
    {
        menuPanel.SetActive(false);
    }
}
