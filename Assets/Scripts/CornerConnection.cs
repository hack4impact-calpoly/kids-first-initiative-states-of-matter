using UnityEngine;

public class CornerConnection : PipeObject
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
            northConnection = false;
            eastConnection = false;
        }
        else if (rotation == 90)
        {
            northConnection = false;
            westConnection = false;
        }
        else if (rotation == 180)
        {
            southConnection = false;
            westConnection = false;
        }
        else if (rotation == 270)
        {
            eastConnection = false;
            southConnection = false;
        }
    }
}
