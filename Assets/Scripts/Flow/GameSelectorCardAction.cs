using UnityEngine;

public sealed class GameSelectorCardAction : MonoBehaviour
{
    private string activityId;
    private GameSelectorFlowController owner;

    public void Initialize(GameSelectorFlowController flowOwner, string id)
    {
        owner = flowOwner;
        activityId = id;
    }

    public void Open()
    {
        if (owner != null)
            owner.OpenActivity(activityId);
    }
}
