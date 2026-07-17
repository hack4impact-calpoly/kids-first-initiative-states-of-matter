using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class WireUndoButton : MonoBehaviour
{
    private Button button;
    private Main gameManager;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(UndoSelectedOutput);
    }

    private void Start()
    {
        gameManager = Main.Instance;
        if (gameManager == null)
            gameManager = FindAnyObjectByType<Main>();

        if (gameManager != null)
        {
            gameManager.DeviceConnectedChanged += OnDeviceConnected;
            gameManager.DeviceDisconnectedChanged += OnDeviceDisconnected;
            gameManager.CircuitCompleted += OnCircuitCompleted;
        }

        RefreshInteractable();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(UndoSelectedOutput);

        if (gameManager == null)
            return;

        gameManager.DeviceConnectedChanged -= OnDeviceConnected;
        gameManager.DeviceDisconnectedChanged -= OnDeviceDisconnected;
        gameManager.CircuitCompleted -= OnCircuitCompleted;
    }

    private void UndoSelectedOutput()
    {
        if (gameManager != null)
            gameManager.UndoSelectedOutput();

        RefreshInteractable();
    }

    private void OnDeviceConnected(DraggableDevice device)
    {
        RefreshInteractable();
    }

    private void OnDeviceDisconnected()
    {
        RefreshInteractable();
    }

    private void OnCircuitCompleted(DraggableDevice device)
    {
        RefreshInteractable();
    }

    private void RefreshInteractable()
    {
        button.interactable = gameManager != null
            && gameManager.HasOutputConnected
            && !gameManager.HasWon;
    }
}
