using UnityEngine;

public class StraightConnection : PipeObject
{
    public override void updateConnections()
    {
        int rotation = getRotation();

        northConnection = true;
        southConnection = true;
        eastConnection = true;
        westConnection = true;

        if (rotation == 0 || rotation == 180)
        {
            eastConnection = false;
            westConnection = false;
        }
        else if (rotation == 90 || rotation == 270)
        {
            northConnection = false;
            southConnection = false;
        }
    }
}
