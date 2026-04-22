using UnityEngine;
using UnityEngine.SceneManagement;

public class Main : MonoBehaviour
{
    static public Main Instance;

    public int wiresCount; // wires total
    public GameObject block;
    public GameObject youDidItText;
    [HideInInspector] public bool isLocked = true;
    [HideInInspector] public DraggableDevice connectedDevice;

    private int count = 0; // number of wires connected
    private Renderer blockRenderer;

    private void Awake()
    {
        Instance = this;
        blockRenderer = block.GetComponent<Renderer>();
        
        // Check which scene we're in
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Wire")
        {
            // Scene without outputs - unlock wires immediately
            isLocked = false;
        }
        else
        {
            // Scene with outputs - keep wires locked until device connected
            isLocked = true;
        }
    }

    public void DeviceConnected(DraggableDevice device)
    {
        isLocked = false;
        connectedDevice = device;
        
        // Clear the prompt when device is connected
        if (WireGameUIManager.Instance != null)
        {
            WireGameUIManager.Instance.ClearPrompt();
        }
    }

    public void DeviceDisconnected()
    {
        isLocked = true;
        connectedDevice = null;
    }

    public void LightOn(int points)
    {
        count += points;  // Simpler than: count = count + points
        
        if (count == wiresCount)
        {
            WinGame();
        }
    }

    void WinGame()
    {
        block.SetActive(true);
        blockRenderer.material.color = Color.green;

        if (connectedDevice != null)
        {
            DeviceEffect[] effects = connectedDevice.GetComponentsInChildren<DeviceEffect>();
            foreach (DeviceEffect effect in effects)
            {
                effect.Activate();
            }
        }
        
        // Show congratulations message
        if (WireGameUIManager.Instance != null)
        {
            WireGameUIManager.Instance.ShowMessage("Congratulations! You completed the wire game!", isWarning: false);
        }
        
        if (youDidItText != null)
            youDidItText.SetActive(true);
    }
}
