using UnityEngine;

public class StartButtonHandler : MonoBehaviour
{
    public PipeUIController ui;
    public GameObject startButton;

    public void OnStartPressed()
    {
        if (startButton != null)
            startButton.SetActive(false);

        if (ui == null)
        {
            Debug.LogError("PipeUIController not assigned.");
            return;
        }

        bool success = CheckEndWater();

        if (success)
            ui.ShowSuccess();
        else
            ui.ShowFailure();
    }

    private bool CheckEndWater()
    {
        PipeObject[] pipes = FindObjectsOfType<PipeObject>();

        if (pipes.Length == 0)
        {
            Debug.LogWarning("No PipeObjects found.");
            return false;
        }

        // Make sure water state is up to date
        pipes[0].recalculateWater();

        foreach (PipeObject pipe in pipes)
        {
            if (pipe.isEnd)
            {
                Debug.Log("End pipe found. Water = " + pipe.water);
                return pipe.water;
            }
        }

        Debug.LogWarning("No PipeObject with isEnd == true found.");
        return false;
    }
}