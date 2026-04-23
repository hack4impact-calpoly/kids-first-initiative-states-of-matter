using UnityEngine;

public class PipeUIController : MonoBehaviour
{
    public GameObject successPanel;
    public GameObject failurePanel;

    public void HideAll()
    {
        if (successPanel) successPanel.SetActive(false);
        if (failurePanel) failurePanel.SetActive(false);
    }

    public void ShowSuccess()
    {
        HideAll();
        if (successPanel) successPanel.SetActive(true);
    }

    public void ShowFailure()
    {
        HideAll();
        if (failurePanel) failurePanel.SetActive(true);
    }
}