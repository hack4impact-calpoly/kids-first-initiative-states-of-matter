using System;
using UnityEngine;

public class FreezeOnClick : MonoBehaviour
{
    PipeObject pipe;
    FrozenFlowValidator validator;
    public static event Action<PipeObject> PipeFreezeToggled;

    void Awake()
    {
        pipe = GetComponent<PipeObject>();
        validator = FindAnyObjectByType<FrozenFlowValidator>();
    }

    void OnMouseDown()
    {
        pipe.ToggleFreeze();          // updates connections + recalc water visuals
        PipeFreezeToggled?.Invoke(pipe);
        validator?.Validate();        // checks leak rules + end rules
    }
}
