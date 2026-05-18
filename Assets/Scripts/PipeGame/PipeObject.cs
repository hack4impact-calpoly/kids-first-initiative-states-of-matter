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

    [Header("Steam")]
    public bool isSteamPipe = false;

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
    public bool steam;
    public bool isSource;
    public bool isEnd;

    [Header("Sprites")]
    public Sprite drySprite;
    public Sprite waterSprite;
    public Sprite steamSprite;

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
            p.steam = false;
            map[new Vector2Int(p.xPos, p.yPos)] = p;
            if (p.isSource) source = p;
        }

        if (source != null)
        {
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
        }

        recalculateSteam(pipes, map);

        foreach (var p in pipes) p.updateVisual();
    }

    void recalculateSteam(PipeObject[] pipes, Dictionary<Vector2Int, PipeObject> map)
    {
        var steamDist = new Dictionary<PipeObject, int>();
        var q = new Queue<PipeObject>();

        foreach (var p in pipes)
        {
            if (!p.isSteamPipe) continue;

            bool touchesWater = false;
            foreach (var neighbor in GetConnectedNeighbors(p, map))
            {
                if (neighbor.water) { touchesWater = true; break; }
            }

            if (touchesWater && !steamDist.ContainsKey(p))
            {
                steamDist[p] = 0;
                q.Enqueue(p);
            }
        }

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            int d = steamDist[cur];

            foreach (var next in GetSteamNeighbors(cur, map))
            {
                if (!steamDist.ContainsKey(next))
                {
                    steamDist[next] = d + 1;
                    q.Enqueue(next);
                }
            }
        }

        foreach (var kv in steamDist)
        {
            var p = kv.Key;
            p.steam = true;
            p.water = false;
        }
    }

    IEnumerable<PipeObject> GetConnectedNeighbors(PipeObject p, Dictionary<Vector2Int, PipeObject> map)
    {
        if (p.northConnection && map.TryGetValue(new Vector2Int(p.xPos, p.yPos + 1), out var n) && n.southConnection)
            yield return n;

        if (p.southConnection && map.TryGetValue(new Vector2Int(p.xPos, p.yPos - 1), out var s) && s.northConnection)
            yield return s;

        if (p.eastConnection && map.TryGetValue(new Vector2Int(p.xPos + 1, p.yPos), out var e) && e.westConnection)
            yield return e;

        if (p.westConnection && map.TryGetValue(new Vector2Int(p.xPos - 1, p.yPos), out var w) && w.eastConnection)
            yield return w;
    }

    IEnumerable<PipeObject> GetSteamNeighbors(PipeObject p, Dictionary<Vector2Int, PipeObject> map)
    {
        if (p.eastConnection && map.TryGetValue(new Vector2Int(p.xPos + 1, p.yPos), out var e) && e.westConnection)
            yield return e;

        if (p.westConnection && map.TryGetValue(new Vector2Int(p.xPos - 1, p.yPos), out var w) && w.eastConnection)
            yield return w;

        if (p.southConnection && map.TryGetValue(new Vector2Int(p.xPos, p.yPos - 1), out var s) && s.northConnection)
            yield return s;

        // find nearest, northern pipe and return only if its a SteamPipe with an open southConnection
        if (p.northConnection)
        {
            PipeObject nearest = null;
            int nearestY = int.MaxValue;

            foreach (var kv in map)
            {
                var candidate = kv.Value;
                if (candidate.xPos == p.xPos && candidate.yPos > p.yPos && candidate.yPos < nearestY)
                {
                    nearest = candidate;
                    nearestY = candidate.yPos;
                }
            }

            if (nearest != null && nearest.isSteamPipe && nearest.southConnection)
                yield return nearest;
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
        if (steam && steamSprite != null)
            spriteRenderer.sprite = steamSprite;
        else if (water)
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