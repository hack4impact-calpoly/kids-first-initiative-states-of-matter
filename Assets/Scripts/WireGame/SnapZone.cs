using UnityEngine;

public class SnapZone : MonoBehaviour
{
    [Tooltip("Optional guide sprite to hide when a device is snapped in")]
    public SpriteRenderer guideSprite;

    [HideInInspector] public DraggableDevice currentDevice;

    public void Snap(DraggableDevice device)
    {
        // If something is already snapped, unsnap it first
        if (currentDevice != null && currentDevice != device)
        {
            currentDevice.ReturnToStart();
        }

        currentDevice = device;
        device.transform.position = transform.position;

        if (guideSprite != null)
            guideSprite.enabled = false;

        if (Main.Instance != null)
            Main.Instance.DeviceConnected(device);
    }

    public void Unsnap()
    {
        currentDevice = null;

        if (guideSprite != null)
            guideSprite.enabled = true;

        if (Main.Instance != null)
            Main.Instance.DeviceDisconnected();
    }
}
