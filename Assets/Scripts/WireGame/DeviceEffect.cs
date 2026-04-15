using UnityEngine;

/// Base class for effects that trigger when all wires are connected.
/// Put on (or as a child of) a DraggableDevice GameObject.
public abstract class DeviceEffect : MonoBehaviour
{
    public abstract void Activate();
}
