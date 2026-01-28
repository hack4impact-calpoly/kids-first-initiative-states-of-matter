using UnityEngine;

public class CreditButton : MonoBehaviour
{
    public GameObject creditsPanel;
    public GameObject menuPanel;

    public void OpenCredits()
    {
        menuPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        creditsPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

}
