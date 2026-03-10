using UnityEngine;

public class FreezeOnClick : MonoBehaviour
{
    PipeObject pipe;
    FrozenFlowValidator validator;

    void Awake()
    {
        pipe = GetComponent<PipeObject>();
        validator = FindFirstObjectByType<FrozenFlowValidator>();
    }

    void OnMouseDown()
    {
        pipe.ToggleFreeze();          // updates connections + recalc water visuals
        validator?.Validate();        // checks leak rules + end rules
    }
}