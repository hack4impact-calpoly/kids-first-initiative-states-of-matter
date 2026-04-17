using UnityEngine;

public class Main : MonoBehaviour
{
    static public Main Instance;

    public int wiresCount; // wires total
    public GameObject block;
    [HideInInspector] public bool isLocked = true;
    [HideInInspector] public DraggableDevice connectedDevice;

    private int count = 0; // number of wires connected
    private Renderer blockRenderer;

    private void Awake()
    {
        Instance = this;
        blockRenderer = block.GetComponent<Renderer>();
    }

    public void DeviceConnected(DraggableDevice device)
    {
        isLocked = false;
        connectedDevice = device;
    }

    public void DeviceDisconnected()
    {
        isLocked = true;
        connectedDevice = null;
        // TODO: optionally reset any in-progress wire connections
    }

    public void LightOn(int points) {
        count = count + points;
        if (count == wiresCount)
        {
            block.SetActive(true);
            if (blockRenderer != null)
                blockRenderer.material.color = Color.green;
            
            // Activate effects on the connected device
            if (connectedDevice != null)
            {
                DeviceEffect[] effects = connectedDevice.GetComponentsInChildren<DeviceEffect>();
                foreach (DeviceEffect effect in effects)
                {
                    effect.Activate();
                }
            }
        }
    }
}
