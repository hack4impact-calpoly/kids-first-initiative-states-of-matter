using System;
using UnityEngine;

public class FreezeOnClick : MonoBehaviour
{
    PipeObject pipe;
    public static event Action<PipeObject> PipeFreezeToggled;

    void Awake()
    {
        pipe = GetComponent<PipeObject>();
    }

    void OnMouseDown()
    {
        pipe.ToggleFreeze();          // updates connections + recalc water visuals
        PipeFreezeToggled?.Invoke(pipe);
    }
}
