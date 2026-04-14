using UnityEngine;

public class JuiceFreezingManager : MonoBehaviour
{
    public enum GameState { Playing, Won }

    [Header("Scene References")]
    [SerializeField] private JuiceCoolingController juiceCoolingController;
    [SerializeField] private IceTray tray;
    [SerializeField] private GameObject winText;


    public GameState State { get; private set; } = GameState.Playing;

    private bool trayFilled;
    private bool coldEnough;

    private void Awake()
    {
        SetWin(false);
        State = GameState.Playing;
    }

    private void Update()
    {
        if (State != GameState.Playing) return;

        if (tray != null)
            trayFilled = tray.IsFull();

        if (juiceCoolingController != null)
            coldEnough = juiceCoolingController.IsColdEnough;

        Evaluate();
    }

    private void Evaluate()
    {
        if (trayFilled && coldEnough)
        {
            Win();
        }
    }

    private void Win()
    {
        State = GameState.Won;
        SetWin(true);
        Debug.Log("Freezing Level Complete!");
    }

    private void SetWin(bool on) { if (winText != null) winText.SetActive(on); }
}