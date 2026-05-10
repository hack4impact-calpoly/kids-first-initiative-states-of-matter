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

    [Header("Frozen level only")]
    public bool isSink;

    public bool CanTransmitWater()
    {
        return water && (!isSink || isFrozen);
    }

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
        var pipes = FindObjectsByType<PipeObject>(FindObjectsSortMode.None);
        var map = new Dictionary<Vector2Int, PipeObject>();
        PipeObject source = null;

        foreach (var p in pipes)
        {
            p.water = false;
            map[new Vector2Int(p.xPos, p.yPos)] = p;
            if (p.isSource) source = p;
        }

        if (source == null) return;

        var dist = new Dictionary<PipeObject, int>();
        var q = new Queue<PipeObject>();

        dist[source] = 0;
        q.Enqueue(source);

        int sinkDist = int.MaxValue;

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            int d = dist[cur];

            if (d >= sinkDist) continue;

            if (cur.isSink && !cur.isFrozen)
            {
                sinkDist = d;
                continue;
            }

            foreach (var next in GetConnectedNeighbors(cur, map))
            {

                if (!dist.ContainsKey(next))
                {
                    dist[next] = d + 1;
                    q.Enqueue(next);
                }
            }
        }

        foreach (var kv in dist)
        {
            var p = kv.Key;
            int d = kv.Value;

            if (d <= sinkDist) p.water = true;
        }

        foreach (var p in pipes) p.updateVisual();
    }

        IEnumerable<PipeObject> GetConnectedNeighbors(PipeObject p, Dictionary<Vector2Int, PipeObject> map)
    {
        // north (y+1)
        if (p.northConnection && map.TryGetValue(new Vector2Int(p.xPos, p.yPos + 1), out var n) && n.southConnection)
            yield return n;

        // south (y-1)
        if (p.southConnection && map.TryGetValue(new Vector2Int(p.xPos, p.yPos - 1), out var s) && s.northConnection)
            yield return s;

        // east (x+1)
        if (p.eastConnection && map.TryGetValue(new Vector2Int(p.xPos + 1, p.yPos), out var e) && e.westConnection)
            yield return e;

        // west (x-1)
        if (p.westConnection && map.TryGetValue(new Vector2Int(p.xPos - 1, p.yPos), out var w) && w.eastConnection)
            yield return w;
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
            if (!pipe.CanTransmitWater())
                continue;

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
    
