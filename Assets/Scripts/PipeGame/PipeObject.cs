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

    [Header("Water Overlay")]
    public Sprite waterOverlaySprite;
    public Color waterOverlayColor = Color.white;
    public int waterOverlaySortingOrderOffset = 1;
    public float waterOverlayAlpha = 0.45f;
    public float waterOverlayScalePadding = 1.18f;
    public float waterOverlayScrollSpeed = 0.55f;
    
    [Header("Tint")]
    public Color normalColor = Color.white;
    public Color frozenColor = Color.cyan;


    private SpriteRenderer spriteRenderer;
    private SpriteRenderer waterOverlayRenderer;
    private SpriteMask waterOverlayMask;
    private const string WaterOverlayName = "Water Overlay";
    private const string WaterOverlayMaskName = "Water Overlay Mask";
    private const float WaterOverlayRotationDegrees = 90f;
    private float waterOverlayScrollRange;
    private Sprite ActiveWaterOverlaySprite => waterOverlaySprite != null ? waterOverlaySprite : waterSprite;

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
        if (spriteRenderer != null)
        {
            Sprite targetSprite = water && waterSprite != null ? waterSprite : drySprite;
            if (targetSprite != null)
                spriteRenderer.sprite = targetSprite;
        }

        EnsureWaterOverlay();

        if (waterOverlayRenderer != null)
            waterOverlayRenderer.enabled = water && ActiveWaterOverlaySprite != null;

        if (waterOverlayMask != null)
            waterOverlayMask.enabled = water && waterSprite != null;
    }

    void EnsureWaterOverlay()
    {
        Sprite overlaySprite = ActiveWaterOverlaySprite;

        if (overlaySprite == null || spriteRenderer == null)
            return;

        if (waterOverlayRenderer == null)
        {
            Transform existingOverlay = transform.Find(WaterOverlayName);
            if (existingOverlay != null)
                waterOverlayRenderer = existingOverlay.GetComponent<SpriteRenderer>();

            if (waterOverlayRenderer == null)
            {
                GameObject overlayObject = new GameObject(WaterOverlayName);
                overlayObject.transform.SetParent(transform, false);
                waterOverlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
            }
        }

        EnsureWaterOverlayMask();

        waterOverlayRenderer.sprite = overlaySprite;
        Color overlayColor = waterOverlayColor;
        overlayColor.a *= waterOverlayAlpha;
        waterOverlayRenderer.color = overlayColor;
        waterOverlayRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        waterOverlayRenderer.sortingOrder = spriteRenderer.sortingOrder + waterOverlaySortingOrderOffset;
        waterOverlayRenderer.maskInteraction = waterOverlayMask != null
            ? SpriteMaskInteraction.VisibleInsideMask
            : SpriteMaskInteraction.None;

        FitWaterOverlayToPipe(overlaySprite);
        waterOverlayRenderer.enabled = water;
    }

    void EnsureWaterOverlayMask()
    {
        if (waterSprite == null || spriteRenderer == null)
            return;

        if (waterOverlayMask == null)
        {
            Transform existingMask = transform.Find(WaterOverlayMaskName);
            if (existingMask != null)
                waterOverlayMask = existingMask.GetComponent<SpriteMask>();

            if (waterOverlayMask == null)
            {
                GameObject maskObject = new GameObject(WaterOverlayMaskName);
                maskObject.transform.SetParent(transform, false);
                waterOverlayMask = maskObject.AddComponent<SpriteMask>();
            }
        }

        int overlaySortingOrder = spriteRenderer.sortingOrder + waterOverlaySortingOrderOffset;
        waterOverlayMask.sprite = waterSprite;
        waterOverlayMask.alphaCutoff = 0.08f;
        waterOverlayMask.isCustomRangeActive = true;
        waterOverlayMask.backSortingLayerID = spriteRenderer.sortingLayerID;
        waterOverlayMask.backSortingOrder = overlaySortingOrder - 1;
        waterOverlayMask.frontSortingLayerID = spriteRenderer.sortingLayerID;
        waterOverlayMask.frontSortingOrder = overlaySortingOrder + 1;
        waterOverlayMask.transform.localPosition = Vector3.zero;
        waterOverlayMask.transform.localRotation = Quaternion.identity;
        waterOverlayMask.transform.localScale = GetRendererSizeScale(waterSprite);
        waterOverlayMask.enabled = water;
    }

    void FitWaterOverlayToPipe(Sprite overlaySprite)
    {
        if (waterOverlayRenderer == null || overlaySprite == null || spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        Vector2 pipeSize = GetRendererLocalSize(spriteRenderer.sprite);
        Vector2 overlaySize = overlaySprite.bounds.size;

        if (overlaySize.x <= 0f || overlaySize.y <= 0f)
            return;

        float scale = Mathf.Max(pipeSize.x / overlaySize.y, pipeSize.y / overlaySize.x) * Mathf.Max(1f, waterOverlayScalePadding);
        waterOverlayScrollRange = Mathf.Max(pipeSize.y * 0.35f, overlaySize.x * scale - pipeSize.y);
        AnimateWaterOverlay();
        waterOverlayRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, WaterOverlayRotationDegrees);
        waterOverlayRenderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    Vector2 GetRendererLocalSize(Sprite sprite)
    {
        if (sprite == null)
            return Vector2.one;

        if (spriteRenderer.drawMode != SpriteDrawMode.Simple)
            return spriteRenderer.size;

        return sprite.bounds.size;
    }

    Vector3 GetRendererSizeScale(Sprite sprite)
    {
        if (sprite == null || sprite.bounds.size.x <= 0f || sprite.bounds.size.y <= 0f)
            return Vector3.one;

        Vector2 rendererSize = GetRendererLocalSize(sprite);
        return new Vector3(rendererSize.x / sprite.bounds.size.x, rendererSize.y / sprite.bounds.size.y, 1f);
    }

    void AnimateWaterOverlay()
    {
        if (waterOverlayRenderer == null || !waterOverlayRenderer.enabled || waterOverlayScrollRange <= 0f)
            return;

        float pathCoordinate = Vector2.Dot(transform.position, transform.up);
        float scroll = Mathf.Repeat(Time.time * waterOverlayScrollSpeed + pathCoordinate, waterOverlayScrollRange);
        waterOverlayRenderer.transform.localPosition = Vector3.up * (scroll - waterOverlayScrollRange * 0.5f);
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        EnsureWaterOverlay();
    }
    void Start()
    {
        updateConnections();
        recalculateWater();
    }

    void Update()
    {
        AnimateWaterOverlay();
    }

    /*void OnMouseDown()
    {
        float degrees = 90f;
        transform.Rotate(0f, 0f, degrees);

        updateConnections();
        recalculateWater();
    }*/
}
    
