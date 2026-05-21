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
    public Sprite frozenSprite;
    public Sprite[] frozenCrystalSprites;
    public Sprite[] frozenSnowflakeSprites;

    [Header("Water Overlay")]
    public Color waterOverlayColor = Color.white;
    public int waterOverlaySortingOrderOffset = 1;
    public float waterOverlayAlpha = 0.45f;
    public float waterOverlayScrollSpeed = 0.55f;
    public int waterOverlayBubbleCount = 7;
    public float waterOverlayBubbleSize = 0.22f;
    
    [Header("Visual")]
    public Color normalColor = Color.white;
    public int frozenCrystalCount = 2;
    public int frozenSnowflakeCount = 4;
    public float frozenCrystalScale = 0.32f;
    public float frozenSnowflakeScale = 0.16f;
    public float frozenSnowflakeDriftDistance = 0.48f;
    public float frozenSnowflakeDriftSpeed = 0.42f;
    public int frozenDecorSortingOrderOffset = 2;


    private SpriteRenderer spriteRenderer;
    private Transform waterOverlayRoot;
    private readonly List<SpriteRenderer> waterBubbleRenderers = new List<SpriteRenderer>();
    private SpriteMask waterOverlayMask;
    private Transform frozenDecorRoot;
    private readonly List<SpriteRenderer> frozenCrystalRenderers = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> frozenSnowflakeRenderers = new List<SpriteRenderer>();
    private readonly List<Vector2> frozenDecorDirections = new List<Vector2>(4);
    private const string WaterOverlayName = "Water Overlay";
    private const string WaterOverlayMaskName = "Water Overlay Mask";
    private const string FrozenDecorName = "Frozen Decorations";
    private const int WaterBubbleTextureSize = 32;
    private Vector2 waterFlowDirection = Vector2.up;
    private float waterOverlayPathLength = 1f;
    private float waterOverlayLaneHalfWidth = 0.2f;
    private float waterOverlayBubbleDiameter = 0.1f;
    private static Sprite waterBubbleSprite;

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
            p.waterFlowDirection = p.GetDefaultWaterFlowDirection();
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

            if (d <= sinkDist)
            {
                p.water = true;
                p.waterFlowDirection = p.ResolveWaterFlowDirection(map, dist, d, sinkDist);
            }
        }

        foreach (var p in pipes) p.updateVisual();
    }

    Vector2 ResolveWaterFlowDirection(Dictionary<Vector2Int, PipeObject> map, Dictionary<PipeObject, int> dist, int distance, int sinkDist)
    {
        PipeObject downstream = null;
        int downstreamDistance = int.MaxValue;

        foreach (PipeObject neighbor in GetConnectedNeighbors(this, map))
        {
            if (dist.TryGetValue(neighbor, out int neighborDistance) &&
                neighborDistance > distance &&
                neighborDistance <= sinkDist &&
                neighborDistance < downstreamDistance)
            {
                downstream = neighbor;
                downstreamDistance = neighborDistance;
            }
        }

        if (downstream != null)
            return GetGridDirectionTo(downstream);

        PipeObject upstream = null;
        int upstreamDistance = int.MinValue;

        foreach (PipeObject neighbor in GetConnectedNeighbors(this, map))
        {
            if (dist.TryGetValue(neighbor, out int neighborDistance) &&
                neighborDistance < distance &&
                neighborDistance > upstreamDistance)
            {
                upstream = neighbor;
                upstreamDistance = neighborDistance;
            }
        }

        if (upstream != null)
            return upstream.GetGridDirectionTo(this);

        return GetDefaultWaterFlowDirection();
    }

    Vector2 GetGridDirectionTo(PipeObject pipe)
    {
        Vector2 direction = new Vector2(pipe.xPos - xPos, pipe.yPos - yPos);
        return direction.sqrMagnitude > 0.001f ? direction.normalized : GetDefaultWaterFlowDirection();
    }

    Vector2 GetDefaultWaterFlowDirection()
    {
        if (eastConnection)
            return Vector2.right;

        if (northConnection)
            return Vector2.up;

        if (westConnection)
            return Vector2.left;

        if (southConnection)
            return Vector2.down;

        return Vector2.up;
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
            Sprite targetSprite = ResolveVisualSprite();
            if (targetSprite != null)
                spriteRenderer.sprite = targetSprite;

            spriteRenderer.color = normalColor;
        }

        EnsureWaterOverlay();

        bool showWaterOverlay = water && !isFrozen && waterSprite != null;
        SetWaterOverlayEnabled(showWaterOverlay);

        if (waterOverlayMask != null)
            waterOverlayMask.enabled = showWaterOverlay;

        EnsureFrozenDecor();
        SetFrozenDecorEnabled(isFrozen && HasFrozenDecorSprites());
    }

    Sprite ResolveVisualSprite()
    {
        if (isFrozen && frozenSprite != null)
            return frozenSprite;

        if (water && waterSprite != null)
            return waterSprite;

        return drySprite;
    }

    void EnsureFrozenDecor()
    {
        if (!HasFrozenDecorSprites() || spriteRenderer == null)
        {
            SetFrozenDecorEnabled(false);
            return;
        }

        EnsureFrozenDecorRoot();
        EnsureFrozenCrystals();
        EnsureFrozenSnowflakes();
        AnimateFrozenDecor();
    }

    void EnsureFrozenCrystals()
    {
        int targetCount = HasSprites(frozenCrystalSprites) ? Mathf.Max(0, frozenCrystalCount) : 0;

        while (frozenCrystalRenderers.Count < targetCount)
        {
            GameObject decorObject = new GameObject($"Frozen Crystal {frozenCrystalRenderers.Count + 1}");
            decorObject.transform.SetParent(frozenDecorRoot, false);
            frozenCrystalRenderers.Add(decorObject.AddComponent<SpriteRenderer>());
        }

        Vector2 pipeSize = GetRendererLocalSize(spriteRenderer.sprite);
        for (int i = 0; i < frozenCrystalRenderers.Count; i++)
        {
            SpriteRenderer decor = frozenCrystalRenderers[i];
            bool active = i < targetCount;
            Sprite decorSprite = active ? frozenCrystalSprites[PickFrozenDecorSpriteIndex(frozenCrystalSprites, i)] : null;

            decor.sprite = decorSprite;
            decor.sortingLayerID = spriteRenderer.sortingLayerID;
            decor.sortingOrder = spriteRenderer.sortingOrder + frozenDecorSortingOrderOffset;
            decor.color = Color.white;
            decor.enabled = active && isFrozen && decorSprite != null;

            if (decorSprite != null)
                PositionFrozenCrystal(decor, decorSprite, pipeSize, i);
        }
    }

    void EnsureFrozenSnowflakes()
    {
        int targetCount = HasSprites(frozenSnowflakeSprites) ? Mathf.Max(0, frozenSnowflakeCount) : 0;

        while (frozenSnowflakeRenderers.Count < targetCount)
        {
            GameObject decorObject = new GameObject($"Frozen Snowflake {frozenSnowflakeRenderers.Count + 1}");
            decorObject.transform.SetParent(frozenDecorRoot, false);
            frozenSnowflakeRenderers.Add(decorObject.AddComponent<SpriteRenderer>());
        }

        for (int i = 0; i < frozenSnowflakeRenderers.Count; i++)
        {
            SpriteRenderer snowflake = frozenSnowflakeRenderers[i];
            bool active = i < targetCount;
            Sprite snowflakeSprite = active ? frozenSnowflakeSprites[PickFrozenDecorSpriteIndex(frozenSnowflakeSprites, i)] : null;

            snowflake.sprite = snowflakeSprite;
            snowflake.sortingLayerID = spriteRenderer.sortingLayerID;
            snowflake.sortingOrder = spriteRenderer.sortingOrder + frozenDecorSortingOrderOffset + 1;
            snowflake.enabled = active && isFrozen && snowflakeSprite != null;
        }
    }

    void EnsureFrozenDecorRoot()
    {
        if (frozenDecorRoot != null)
            return;

        Transform existingDecor = transform.Find(FrozenDecorName);
        if (existingDecor != null)
            frozenDecorRoot = existingDecor;

        if (frozenDecorRoot == null)
        {
            GameObject decorObject = new GameObject(FrozenDecorName);
            decorObject.transform.SetParent(transform, false);
            frozenDecorRoot = decorObject.transform;
        }

        frozenDecorRoot.localPosition = Vector3.zero;
        frozenDecorRoot.localRotation = Quaternion.identity;
        frozenDecorRoot.localScale = Vector3.one;
    }

    void PositionFrozenCrystal(SpriteRenderer decor, Sprite decorSprite, Vector2 pipeSize, int index)
    {
        GetFrozenDecorAnchor(pipeSize, index, out Vector2 flowDirection, out Vector2 outwardNormal, out float along, out float edge);

        float targetHeight = Mathf.Min(pipeSize.x, pipeSize.y) * frozenCrystalScale * Mathf.Lerp(0.85f, 1.2f, FrozenDecorSeed(index, 5));
        float spriteHeight = decorSprite.bounds.size.y;
        float scale = spriteHeight > 0f ? targetHeight / spriteHeight : 1f;
        Vector2 localPosition = flowDirection * along + outwardNormal * (edge + targetHeight * 0.35f);
        float angle = Vector2.SignedAngle(Vector2.up, outwardNormal) + Mathf.Lerp(-12f, 12f, FrozenDecorSeed(index, 6));

        decor.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
        decor.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        decor.transform.localScale = new Vector3(scale, scale, 1f);
    }

    void AnimateFrozenDecor()
    {
        if (!isFrozen || !HasSprites(frozenSnowflakeSprites) || spriteRenderer == null)
            return;

        Vector2 pipeSize = GetRendererLocalSize(spriteRenderer.sprite);
        int targetCount = Mathf.Max(0, frozenSnowflakeCount);

        for (int i = 0; i < frozenSnowflakeRenderers.Count; i++)
        {
            SpriteRenderer snowflake = frozenSnowflakeRenderers[i];
            if (snowflake == null || i >= targetCount || snowflake.sprite == null)
                continue;

            PositionFrozenSnowflake(snowflake, snowflake.sprite, pipeSize, i);
        }
    }

    void PositionFrozenSnowflake(SpriteRenderer snowflake, Sprite snowflakeSprite, Vector2 pipeSize, int index)
    {
        int seedIndex = index + 17;
        GetFrozenDecorAnchor(pipeSize, seedIndex, out Vector2 flowDirection, out Vector2 outwardNormal, out float along, out float edge);

        float progress = Mathf.Repeat(Time.time * frozenSnowflakeDriftSpeed + FrozenDecorSeed(index, 8), 1f);
        float targetSize = Mathf.Min(pipeSize.x, pipeSize.y) * frozenSnowflakeScale * Mathf.Lerp(0.75f, 1.28f, FrozenDecorSeed(index, 9));
        float spriteSize = Mathf.Max(snowflakeSprite.bounds.size.x, snowflakeSprite.bounds.size.y);
        float scale = spriteSize > 0f ? targetSize / spriteSize : 1f;
        float tangentDrift = Mathf.Sin(progress * Mathf.PI * 2f + FrozenDecorSeed(index, 10) * Mathf.PI * 2f) * targetSize * 0.35f;
        Vector2 start = flowDirection * along + outwardNormal * (edge + targetSize * 0.2f);
        Vector2 localPosition = start + outwardNormal * (progress * frozenSnowflakeDriftDistance) + flowDirection * tangentDrift;
        float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.18f)) *
            (1f - Mathf.SmoothStep(0.38f, 1f, progress));
        Color color = Color.white;
        color.a = alpha * 0.85f;

        snowflake.color = color;
        snowflake.enabled = isFrozen && alpha > 0.02f;
        snowflake.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
        snowflake.transform.localRotation = Quaternion.Euler(0f, 0f, FrozenDecorSeed(index, 11) * 360f + Time.time * 28f);
        snowflake.transform.localScale = Vector3.one * scale * Mathf.Lerp(0.75f, 1.08f, progress);
    }

    void GetFrozenDecorAnchor(Vector2 pipeSize, int index, out Vector2 flowDirection, out Vector2 outwardNormal, out float along, out float edge)
    {
        FillFrozenDecorDirections();

        flowDirection = frozenDecorDirections.Count > 0
            ? frozenDecorDirections[index % frozenDecorDirections.Count]
            : Vector2.up;

        if (flowDirection.sqrMagnitude <= 0.001f)
            flowDirection = Vector2.up;

        flowDirection.Normalize();
        outwardNormal = new Vector2(-flowDirection.y, flowDirection.x);
        if (FrozenDecorSeed(index, 1) < 0.5f)
            outwardNormal = -outwardNormal;

        float length = Mathf.Abs(flowDirection.x) * pipeSize.x + Mathf.Abs(flowDirection.y) * pipeSize.y;
        float width = Mathf.Abs(outwardNormal.x) * pipeSize.x + Mathf.Abs(outwardNormal.y) * pipeSize.y;
        float minAlong = Mathf.Min(length * 0.14f, length * 0.36f);
        float maxAlong = Mathf.Max(length * 0.2f, length * 0.42f);
        along = Mathf.Lerp(minAlong, maxAlong, FrozenDecorSeed(index, 2));
        edge = width * 0.34f;
    }

    void FillFrozenDecorDirections()
    {
        frozenDecorDirections.Clear();
        AddFrozenDecorDirection(northConnection, Vector2.up);
        AddFrozenDecorDirection(southConnection, Vector2.down);
        AddFrozenDecorDirection(eastConnection, Vector2.right);
        AddFrozenDecorDirection(westConnection, Vector2.left);

        if (frozenDecorDirections.Count == 0)
            frozenDecorDirections.Add(GetWaterFlowLocalDirection());
    }

    void AddFrozenDecorDirection(bool connected, Vector2 worldDirection)
    {
        if (!connected)
            return;

        Vector3 local = transform.InverseTransformDirection(new Vector3(worldDirection.x, worldDirection.y, 0f));
        Vector2 localDirection = SnapLocalDirection(new Vector2(local.x, local.y));

        for (int i = 0; i < frozenDecorDirections.Count; i++)
        {
            if (Vector2.Dot(frozenDecorDirections[i], localDirection) > 0.99f)
                return;
        }

        frozenDecorDirections.Add(localDirection);
    }

    Vector2 SnapLocalDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return Vector2.up;

        direction.Normalize();
        return Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? new Vector2(Mathf.Sign(direction.x), 0f)
            : new Vector2(0f, Mathf.Sign(direction.y));
    }

    int PickFrozenDecorSpriteIndex(Sprite[] sprites, int index)
    {
        int seed = Mathf.Abs(xPos * 31 + yPos * 17 + index * 13);
        return seed % sprites.Length;
    }

    float FrozenDecorSeed(int index, int salt)
    {
        float value = Mathf.Sin((xPos + 1) * 19.19f + (yPos + 1) * 37.37f + index * 53.53f + salt * 71.71f) * 43758.5453f;
        return value - Mathf.Floor(value);
    }

    void SetFrozenDecorEnabled(bool enabled)
    {
        for (int i = 0; i < frozenCrystalRenderers.Count; i++)
        {
            if (frozenCrystalRenderers[i] != null)
                frozenCrystalRenderers[i].enabled = enabled && i < frozenCrystalCount && frozenCrystalRenderers[i].sprite != null;
        }

        for (int i = 0; i < frozenSnowflakeRenderers.Count; i++)
        {
            if (frozenSnowflakeRenderers[i] != null)
                frozenSnowflakeRenderers[i].enabled = enabled && i < frozenSnowflakeCount && frozenSnowflakeRenderers[i].sprite != null;
        }
    }

    bool HasFrozenDecorSprites()
    {
        return HasSprites(frozenCrystalSprites) || HasSprites(frozenSnowflakeSprites);
    }

    bool HasSprites(Sprite[] sprites)
    {
        return sprites != null && sprites.Length > 0;
    }

    void EnsureWaterOverlay()
    {
        if (waterSprite == null || spriteRenderer == null)
            return;

        EnsureWaterOverlayRoot();
        EnsureWaterOverlayMask();
        EnsureWaterBubbleSprite();
        EnsureWaterBubbles();
        FitWaterOverlayToPipe();
        SetWaterOverlayEnabled(water && !isFrozen);
    }

    void EnsureWaterOverlayRoot()
    {
        if (waterOverlayRoot != null)
            return;

        Transform existingOverlay = transform.Find(WaterOverlayName);
        if (existingOverlay != null)
            waterOverlayRoot = existingOverlay;

        if (waterOverlayRoot == null)
        {
            GameObject overlayObject = new GameObject(WaterOverlayName);
            overlayObject.transform.SetParent(transform, false);
            waterOverlayRoot = overlayObject.transform;
        }

        SpriteRenderer legacyRenderer = waterOverlayRoot.GetComponent<SpriteRenderer>();
        if (legacyRenderer != null)
            legacyRenderer.enabled = false;
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
        waterOverlayMask.enabled = water && !isFrozen;
    }

    void EnsureWaterBubbles()
    {
        int targetCount = Mathf.Max(0, waterOverlayBubbleCount);
        while (waterBubbleRenderers.Count < targetCount)
        {
            GameObject bubbleObject = new GameObject($"Water Bubble {waterBubbleRenderers.Count + 1}");
            bubbleObject.transform.SetParent(waterOverlayRoot, false);
            waterBubbleRenderers.Add(bubbleObject.AddComponent<SpriteRenderer>());
        }

        for (int i = 0; i < waterBubbleRenderers.Count; i++)
        {
            SpriteRenderer bubble = waterBubbleRenderers[i];
            bool active = i < targetCount;

            bubble.sprite = waterBubbleSprite;
            bubble.sortingLayerID = spriteRenderer.sortingLayerID;
            bubble.sortingOrder = spriteRenderer.sortingOrder + waterOverlaySortingOrderOffset;
            bubble.maskInteraction = waterOverlayMask != null
                ? SpriteMaskInteraction.VisibleInsideMask
                : SpriteMaskInteraction.None;

            Color bubbleColor = waterOverlayColor;
            bubbleColor.a *= Mathf.Lerp(0.5f, 1f, BubbleSeed(i, 3)) * waterOverlayAlpha;
            bubble.color = bubbleColor;
            bubble.enabled = active && water && !isFrozen;
        }
    }

    void FitWaterOverlayToPipe()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        Vector2 pipeSize = GetRendererLocalSize(spriteRenderer.sprite);
        Vector2 flowDirection = GetWaterFlowLocalDirection();
        Vector2 lateralDirection = new Vector2(-flowDirection.y, flowDirection.x);

        waterOverlayPathLength = Mathf.Max(
            0.1f,
            Mathf.Abs(flowDirection.x) * pipeSize.x + Mathf.Abs(flowDirection.y) * pipeSize.y);
        float laneWidth = Mathf.Abs(lateralDirection.x) * pipeSize.x + Mathf.Abs(lateralDirection.y) * pipeSize.y;
        waterOverlayLaneHalfWidth = laneWidth * 0.26f;
        waterOverlayBubbleDiameter = Mathf.Max(0.04f, laneWidth * waterOverlayBubbleSize);

        waterOverlayRoot.localPosition = Vector3.zero;
        waterOverlayRoot.localRotation = Quaternion.identity;
        waterOverlayRoot.localScale = Vector3.one;
        AnimateWaterOverlay();
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
        if (!water || waterBubbleRenderers.Count == 0 || waterOverlayPathLength <= 0f || waterBubbleSprite == null)
            return;

        Vector2 flowDirection = GetWaterFlowLocalDirection();
        Vector2 lateralDirection = new Vector2(-flowDirection.y, flowDirection.x);
        Vector2 worldFlowDirection = waterFlowDirection.sqrMagnitude > 0.001f
            ? waterFlowDirection.normalized
            : Vector2.up;
        float pathCoordinate = Vector2.Dot(transform.position, worldFlowDirection);
        float travelLength = waterOverlayPathLength + waterOverlayBubbleDiameter * 2f;
        int activeCount = Mathf.Max(1, waterOverlayBubbleCount);
        float spacing = travelLength / activeCount;

        for (int i = 0; i < waterBubbleRenderers.Count; i++)
        {
            SpriteRenderer bubble = waterBubbleRenderers[i];
            if (bubble == null || !bubble.enabled)
                continue;

            float progress = Mathf.Repeat(Time.time * waterOverlayScrollSpeed + pathCoordinate + spacing * i, travelLength);
            float along = progress - travelLength * 0.5f;
            float lane = Mathf.Lerp(-waterOverlayLaneHalfWidth, waterOverlayLaneHalfWidth, BubbleSeed(i, 1));
            lane += Mathf.Sin(Time.time * 1.6f + BubbleSeed(i, 2) * Mathf.PI * 2f) * waterOverlayLaneHalfWidth * 0.12f;

            Vector2 localPosition = flowDirection * along + lateralDirection * lane;
            float diameter = waterOverlayBubbleDiameter * Mathf.Lerp(0.48f, 1.15f, BubbleSeed(i, 4));
            float spriteSize = waterBubbleSprite.bounds.size.x;
            float scale = spriteSize > 0f ? diameter / spriteSize : 1f;

            bubble.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            bubble.transform.localRotation = Quaternion.identity;
            bubble.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    Vector2 GetWaterFlowLocalDirection()
    {
        Vector2 worldDirection = waterFlowDirection.sqrMagnitude > 0.001f
            ? waterFlowDirection.normalized
            : GetDefaultWaterFlowDirection();
        Vector3 local = transform.InverseTransformDirection(new Vector3(worldDirection.x, worldDirection.y, 0f));
        Vector2 localDirection = new Vector2(local.x, local.y);

        if (localDirection.sqrMagnitude <= 0.001f)
            return Vector2.up;

        localDirection.Normalize();
        return Mathf.Abs(localDirection.x) > Mathf.Abs(localDirection.y)
            ? new Vector2(Mathf.Sign(localDirection.x), 0f)
            : new Vector2(0f, Mathf.Sign(localDirection.y));
    }

    void SetWaterOverlayEnabled(bool enabled)
    {
        for (int i = 0; i < waterBubbleRenderers.Count; i++)
        {
            if (waterBubbleRenderers[i] != null)
                waterBubbleRenderers[i].enabled = enabled && i < waterOverlayBubbleCount;
        }
    }

    static float BubbleSeed(int index, int salt)
    {
        float value = Mathf.Sin((index + 1) * 12.9898f + salt * 78.233f) * 43758.5453f;
        return value - Mathf.Floor(value);
    }

    static void EnsureWaterBubbleSprite()
    {
        if (waterBubbleSprite != null)
            return;

        var texture = new Texture2D(WaterBubbleTextureSize, WaterBubbleTextureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color[WaterBubbleTextureSize * WaterBubbleTextureSize];
        Vector2 center = new Vector2((WaterBubbleTextureSize - 1f) * 0.5f, (WaterBubbleTextureSize - 1f) * 0.5f);
        float radius = WaterBubbleTextureSize * 0.42f;

        for (int y = 0; y < WaterBubbleTextureSize; y++)
        {
            for (int x = 0; x < WaterBubbleTextureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius + 1f - distance);
                pixels[y * WaterBubbleTextureSize + x] = new Color(0.82f, 0.95f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        waterBubbleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, WaterBubbleTextureSize, WaterBubbleTextureSize),
            new Vector2(0.5f, 0.5f),
            100f);
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
        AnimateFrozenDecor();
    }

    /*void OnMouseDown()
    {
        float degrees = 90f;
        transform.Rotate(0f, 0f, degrees);

        updateConnections();
        recalculateWater();
    }*/
}
    
