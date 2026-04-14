using UnityEngine;

public class TJuncConnection : PipeObject
{
    public override void updateConnections()
    {
        int rotation = getRotation();

        northConnection = true;
        southConnection = true;
        eastConnection = true;
        westConnection = true;

        if (rotation == 0)
        {
            eastConnection = false;
        }
        else if (rotation == 90)
        {
            northConnection = false;
        }
        else if (rotation == 180)
        {
            westConnection = false;
        }
        else if (rotation == 270)
        {
            southConnection = false;
        }
    }

}
