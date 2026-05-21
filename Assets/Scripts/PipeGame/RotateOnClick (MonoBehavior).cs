using System;
using UnityEngine;

public class RotateOnClick : MonoBehaviour
{
    public float degrees = 90f;
    PipeObject pipe;
    public static event Action<PipeObject> PipeRotated;

    void Awake() => pipe = GetComponent<PipeObject>();

    void OnMouseDown()
    {
        transform.Rotate(0f, 0f, degrees);
        pipe.updateConnections();
        pipe.recalculateWater();
        PipeRotated?.Invoke(pipe);
    }
}
