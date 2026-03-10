using UnityEngine;
using System.Collections.Generic;

public class PipeObject : MonoBehaviour
{
    public int xPos;
    public int yPos;

    [Header("Current connections")]
    public bool northConnection;
    public bool southConnection;
    public bool eastConnection;
    public bool westConnection;

    [Header("Freeze")]
    public bool isFreezable = true;
    public bool isFrozen;

    [Header("Frozen connections")]
    public bool frozenNorthConnection;
    public bool frozenSouthConnection;
    public bool frozenEastConnection;
    public bool frozenWestConnection;

    [Header("State")]
    public bool water;
    public bool isSource;
    public bool isEnd;

    [Header("Sprites")]
    public Sprite drySprite;
    public Sprite waterSprite;
    
    [Header("Tint")]
    public Color normalColor = Color.white;
    public Color frozenColor = Color.cyan;


    private SpriteRenderer spriteRenderer;

    public void setIsSource()
    {
        isSource = (xPos == 1 && yPos == 4);
    }

    public void setIsEnd()
    {
        isEnd = (xPos == 8 && yPos == 1);
    }
    public virtual void updateConnections()
    {  
    }

    protected int getRotation()
    {
        int snapped = Mathf.RoundToInt(transform.eulerAngles.z / 90f) * 90;
        snapped %= 360;

        if (snapped < 0)
        {
            snapped += 360;
        }

        return snapped;
    }

    void ApplyFrozenOverrides()
    {
        if (!isFrozen) return;

        northConnection = frozenNorthConnection;
        southConnection = frozenSouthConnection;
        eastConnection = frozenEastConnection;
        westConnection = frozenWestConnection;
    }

    public void ToggleFreeze()
    {
        if (!isFreezable) return;

        isFrozen = !isFrozen;

        updateConnections();
        ApplyFrozenOverrides();

        if (spriteRenderer != null)
            spriteRenderer.color = isFrozen ? frozenColor : normalColor;
        
        recalculateWater();
    }

    public void recalculateWater()
    {
        PipeObject[] pipes = FindObjectsOfType<PipeObject>();
        foreach (PipeObject pipe in pipes)
        {
           pipe.water = false;
        }
        

        bool changed;
        do
        {
            changed = false;

            foreach (PipeObject pipe in pipes)
            {
                bool oldWater = pipe.water;

                pipe.checkForWater(pipes);

                if (pipe.water != oldWater)
                    changed = true;
            }

        } while (changed);

        foreach (PipeObject pipe in pipes)
        {
            pipe.updateVisual();
        }
    }

    public void checkForWater(PipeObject[] pipes)
    {
        if (isSource)
        {
            water = true;
            return;
        }

        List<PipeObject> adjacentPipes = new List<PipeObject>();

        foreach (PipeObject pipe in pipes)
        {
            if ((pipe.xPos == xPos && Mathf.Abs(pipe.yPos - yPos) == 1) || 
            (pipe.yPos == yPos && Mathf.Abs(pipe.xPos - xPos) == 1))
            {
                adjacentPipes.Add(pipe);
            }
        }

        water = false;
        foreach (PipeObject pipe in adjacentPipes)
        {
            if (pipe.water)
            {
                if (pipe.xPos > xPos && pipe.westConnection && eastConnection ||
                    pipe.xPos < xPos && pipe.eastConnection && westConnection ||
                    pipe.yPos > yPos && pipe.southConnection && northConnection ||
                    pipe.yPos < yPos && pipe.northConnection && southConnection)
                {
                    water = true;
                    break;
                }
            }
        }
    }

    public void updateVisual()
    {
        if (water)
            spriteRenderer.sprite = waterSprite;
        else
            spriteRenderer.sprite = drySprite;
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    void Start()
    {
        updateConnections();
        recalculateWater();
    }

    /*void OnMouseDown()
    {
        float degrees = 90f;
        transform.Rotate(0f, 0f, degrees);

        updateConnections();
        recalculateWater();
    }*/
}
    
