using UnityEngine;

public class SnapZone : MonoBehaviour
{
    [Tooltip("Optional guide sprite to hide when a device is snapped in")]
    public SpriteRenderer guideSprite;

    [HideInInspector] public DraggableDevice currentDevice;

    public void Snap(DraggableDevice device)
    {
        if (device == null || device.IsInteractionLocked)
            return;

        // If something is already snapped, unsnap it first
        if (currentDevice != null && currentDevice != device)
        {
            if (currentDevice.IsInteractionLocked)
            {
                device.ReturnToStart();
                return;
            }

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
        if (currentDevice != null && currentDevice.IsInteractionLocked)
            return;

        currentDevice = null;

        if (guideSprite != null)
            guideSprite.enabled = true;

        if (Main.Instance != null)
            Main.Instance.DeviceDisconnected();
    }
}
