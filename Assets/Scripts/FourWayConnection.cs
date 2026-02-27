using UnityEngine;

public class FourWayConnection : PipeObject
{
    public override void updateConnections()
    {
        northConnection = true;
        southConnection = true;
        eastConnection = true;
        westConnection = true;
    }
}
