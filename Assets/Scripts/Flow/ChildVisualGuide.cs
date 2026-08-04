using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChildVisualGuide : MonoBehaviour
{
    private const int SourceMarkerCount = 2;
    private const int PathDotCount = 7;

    private static readonly Color SourceColor = new Color32(255, 205, 35, 226);
    private static readonly Color DestinationColor = new Color32(52, 211, 112, 226);

    private readonly List<GameObject> sourceTargets = new List<GameObject>(SourceMarkerCount);
    private readonly List<MarkerVisual> sourceMarkers = new List<MarkerVisual>(SourceMarkerCount);
    private readonly List<RectTransform> pathDots = new List<RectTransform>(PathDotCount);

    private Canvas guideCanvas;
    private RectTransform canvasRect;
    private MarkerVisual destinationMarker;
    private GameObject destinationTarget;
    private bool isShowing;
    private float emphasisUntil;
    private static Sprite ringSprite;
    private static Sprite arrowSprite;

    private sealed class MarkerVisual
    {
        public RectTransform Root;
        public Image Ring;
        public RectTransform ArrowRoot;
        public Image ArrowHead;
        public Image ArrowStem;
        public RectTransform Chip;
        public TextMeshProUGUI Label;
        public Color Color;
        public bool ShowsArrow;
        public bool PreferLabelBelow;
    }

    private void Awake()
    {
        BuildOverlay();
    }

    private void OnEnable()
    {
        if (guideCanvas != null
            && destinationMarker != null
            && sourceMarkers.Count == SourceMarkerCount)
        {
            return;
        }

        Transform staleCanvas = transform.Find("Child Visual Guide Canvas");
        if (staleCanvas != null)
            Destroy(staleCanvas.gameObject);

        sourceTargets.Clear();
        sourceMarkers.Clear();
        pathDots.Clear();
        destinationTarget = null;
        isShowing = false;
        BuildOverlay();
    }

    private void Update()
    {
        if (!isShowing || canvasRect == null || destinationMarker == null)
            return;

        bool emphasized = Time.unscaledTime < emphasisUntil;
        float pulseSpeed = emphasized ? 8f : 3.6f;
        float pulse = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
        float scale = Mathf.Lerp(1f, emphasized ? 1.16f : 1.07f, pulse);

        for (int i = 0; i < sourceMarkers.Count; i++)
        {
            bool active = i < sourceTargets.Count
                && sourceTargets[i] != null
                && TryUpdateMarker(sourceMarkers[i], sourceTargets[i]);
            sourceMarkers[i].Root.gameObject.SetActive(active);

            if (active)
                sourceMarkers[i].Ring.rectTransform.localScale = Vector3.one * scale;
        }

        bool destinationActive = destinationTarget != null
            && TryUpdateMarker(destinationMarker, destinationTarget);
        destinationMarker.Root.gameObject.SetActive(destinationActive);

        if (destinationActive)
            destinationMarker.Ring.rectTransform.localScale = Vector3.one * scale;

        UpdatePath(destinationActive);
    }

    public void ShowAction(GameObject target, string actionLabel)
    {
        SetSources(target != null ? new[] { target } : null);
        destinationTarget = null;
        sourceMarkers[0].PreferLabelBelow = false;
        ConfigureMarker(sourceMarkers[0], actionLabel, SourceColor, true);
        HideUnusedSourceMarkers();
        destinationMarker.Root.gameObject.SetActive(false);
        isShowing = target != null;
        Emphasize();
    }

    public void ShowDrag(GameObject source, GameObject destination, string actionLabel, string destinationLabel)
    {
        SetSources(source != null ? new[] { source } : null);
        destinationTarget = destination;
        sourceMarkers[0].PreferLabelBelow = false;
        destinationMarker.PreferLabelBelow = false;
        ConfigureMarker(sourceMarkers[0], actionLabel, SourceColor, true);
        ConfigureMarker(destinationMarker, destinationLabel, DestinationColor, false);
        HideUnusedSourceMarkers();
        isShowing = source != null || destination != null;
        Emphasize();
    }

    public void ShowChoices(
        GameObject[] sources,
        GameObject destination,
        string actionLabel,
        string destinationLabel,
        bool preferLabelBelow = false)
    {
        SetSources(sources);
        destinationTarget = destination;

        sourceMarkers[0].PreferLabelBelow = preferLabelBelow;
        ConfigureMarker(sourceMarkers[0], actionLabel, SourceColor, true);
        for (int i = 1; i < sourceMarkers.Count; i++)
        {
            sourceMarkers[i].PreferLabelBelow = false;
            ConfigureMarker(sourceMarkers[i], string.Empty, SourceColor, false);
        }

        destinationMarker.PreferLabelBelow = false;
        ConfigureMarker(destinationMarker, destinationLabel, DestinationColor, false);
        HideUnusedSourceMarkers();
        isShowing = sourceTargets.Count > 0 || destination != null;
        Emphasize();
    }

    public void Emphasize()
    {
        emphasisUntil = Time.unscaledTime + 1.35f;
    }

    public void Hide()
    {
        isShowing = false;
        sourceTargets.Clear();
        destinationTarget = null;

        for (int i = 0; i < sourceMarkers.Count; i++)
            sourceMarkers[i].Root.gameObject.SetActive(false);

        destinationMarker.Root.gameObject.SetActive(false);

        for (int i = 0; i < pathDots.Count; i++)
            pathDots[i].gameObject.SetActive(false);
    }

    private void BuildOverlay()
    {
        EnsureSprites();

        guideCanvas = FlowUiFactory.CreateCanvas("Child Visual Guide Canvas", 550);
        guideCanvas.transform.SetParent(transform, false);
        canvasRect = guideCanvas.GetComponent<RectTransform>();

        for (int i = 0; i < PathDotCount; i++)
        {
            Image dot = CreateImage("Movement Dot " + (i + 1), guideCanvas.transform, FlowUiFactory.GetUiSprite(), SourceColor);
            RectTransform dotRect = dot.rectTransform;
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.sizeDelta = new Vector2(20f, 20f);
            dot.gameObject.SetActive(false);
            pathDots.Add(dotRect);
        }

        for (int i = 0; i < SourceMarkerCount; i++)
            sourceMarkers.Add(CreateMarker("Action Target " + (i + 1), SourceColor, true));

        destinationMarker = CreateMarker("Action Destination", DestinationColor, false);
        Hide();
    }

    private MarkerVisual CreateMarker(string name, Color color, bool showsArrow)
    {
        var rootObject = new GameObject(name, typeof(RectTransform));
        rootObject.transform.SetParent(guideCanvas.transform, false);
        RectTransform root = rootObject.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);

        Image ring = CreateImage("Pulsing Ring", root, ringSprite, color);
        FlowUiFactory.Stretch(ring.rectTransform);

        var arrowObject = new GameObject("Pointer", typeof(RectTransform));
        arrowObject.transform.SetParent(root, false);
        RectTransform arrowRoot = arrowObject.GetComponent<RectTransform>();
        arrowRoot.anchorMin = new Vector2(0.5f, 1f);
        arrowRoot.anchorMax = new Vector2(0.5f, 1f);
        arrowRoot.pivot = new Vector2(0.5f, 0.5f);
        arrowRoot.sizeDelta = new Vector2(54f, 64f);

        Image stem = CreateImage("Pointer Stem", arrowRoot, FlowUiFactory.GetUiSprite(), color);
        RectTransform stemRect = stem.rectTransform;
        stemRect.anchorMin = new Vector2(0.5f, 0.5f);
        stemRect.anchorMax = new Vector2(0.5f, 0.5f);
        stemRect.sizeDelta = new Vector2(16f, 34f);
        stemRect.anchoredPosition = new Vector2(0f, 12f);

        Image head = CreateImage("Pointer Head", arrowRoot, arrowSprite, color);
        RectTransform headRect = head.rectTransform;
        headRect.anchorMin = new Vector2(0.5f, 0.5f);
        headRect.anchorMax = new Vector2(0.5f, 0.5f);
        headRect.sizeDelta = new Vector2(54f, 34f);
        headRect.anchoredPosition = new Vector2(0f, -14f);

        Image chip = FlowUiFactory.CreatePanel(
            root,
            "Action Label",
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-142f, 70f),
            new Vector2(142f, 128f),
            FlowUiFactory.Navy);
        chip.raycastTarget = false;

        TextMeshProUGUI label = FlowUiFactory.CreateText(
            chip.transform,
            "Label",
            string.Empty,
            29f,
            TextAlignmentOptions.Center,
            FlowUiFactory.White);
        label.enableAutoSizing = true;
        label.fontSizeMin = 20f;
        label.fontSizeMax = 29f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;

        var marker = new MarkerVisual
        {
            Root = root,
            Ring = ring,
            ArrowRoot = arrowRoot,
            ArrowHead = head,
            ArrowStem = stem,
            Chip = chip.rectTransform,
            Label = label,
            Color = color,
            ShowsArrow = showsArrow
        };

        ConfigureMarker(marker, string.Empty, color, showsArrow);
        rootObject.SetActive(false);
        return marker;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void ConfigureMarker(MarkerVisual marker, string label, Color color, bool showsArrow)
    {
        marker.Color = color;
        marker.ShowsArrow = showsArrow;
        marker.Ring.color = color;
        marker.ArrowHead.color = color;
        marker.ArrowStem.color = color;
        marker.ArrowRoot.gameObject.SetActive(showsArrow && !string.IsNullOrEmpty(label));
        marker.Chip.gameObject.SetActive(!string.IsNullOrEmpty(label));
        marker.Label.text = label;
    }

    private void SetSources(GameObject[] targets)
    {
        sourceTargets.Clear();
        if (targets == null)
            return;

        for (int i = 0; i < targets.Length && sourceTargets.Count < SourceMarkerCount; i++)
        {
            if (targets[i] != null && !sourceTargets.Contains(targets[i]))
                sourceTargets.Add(targets[i]);
        }
    }

    private void HideUnusedSourceMarkers()
    {
        for (int i = sourceTargets.Count; i < sourceMarkers.Count; i++)
            sourceMarkers[i].Root.gameObject.SetActive(false);
    }

    private bool TryUpdateMarker(MarkerVisual marker, GameObject target)
    {
        if (!TryGetTargetRect(target, out Vector2 center, out Vector2 size))
            return false;

        marker.Root.anchoredPosition = center;
        marker.Root.sizeDelta = size + new Vector2(20f, 20f);

        bool placeAbove = !marker.PreferLabelBelow
            && center.y + marker.Root.sizeDelta.y * 0.5f + 124f
            < canvasRect.rect.yMax - 94f;
        PositionPointerAndChip(marker, placeAbove);

        if (marker.ShowsArrow)
        {
            float bounce = (Mathf.Sin(Time.unscaledTime * 5.4f) + 1f) * 7f;
            Vector2 position = marker.ArrowRoot.anchoredPosition;
            position.y += placeAbove ? -bounce : bounce;
            marker.ArrowRoot.anchoredPosition = position;
        }

        return true;
    }

    private static void PositionPointerAndChip(MarkerVisual marker, bool placeAbove)
    {
        marker.ArrowRoot.anchorMin = placeAbove ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
        marker.ArrowRoot.anchorMax = marker.ArrowRoot.anchorMin;
        marker.ArrowRoot.anchoredPosition = placeAbove ? new Vector2(0f, 36f) : new Vector2(0f, -36f);
        marker.ArrowRoot.localRotation = placeAbove
            ? Quaternion.identity
            : Quaternion.Euler(0f, 0f, 180f);

        marker.Chip.anchorMin = placeAbove ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
        marker.Chip.anchorMax = marker.Chip.anchorMin;
        marker.Chip.pivot = new Vector2(0.5f, 0.5f);
        marker.Chip.sizeDelta = new Vector2(240f, 52f);
        marker.Chip.anchoredPosition = placeAbove ? new Vector2(0f, 90f) : new Vector2(0f, -90f);
    }

    private bool TryGetTargetRect(GameObject target, out Vector2 center, out Vector2 size)
    {
        center = Vector2.zero;
        size = Vector2.zero;
        if (target == null || !target.activeInHierarchy)
            return false;

        Vector2 screenMin;
        Vector2 screenMax;
        RectTransform targetRect = target.transform as RectTransform;
        if (targetRect != null)
        {
            Vector3[] corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);
            Canvas targetCanvas = targetRect.GetComponentInParent<Canvas>();
            Camera eventCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera
                : null;

            screenMin = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
            screenMax = screenMin;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[i]);
                screenMin = Vector2.Min(screenMin, screen);
                screenMax = Vector2.Max(screenMax, screen);
            }
        }
        else if (!TryGetWorldScreenBounds(target, out screenMin, out screenMax))
        {
            return false;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenMin, null, out Vector2 localMin)
            || !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenMax, null, out Vector2 localMax))
        {
            return false;
        }

        center = (localMin + localMax) * 0.5f;
        size = new Vector2(
            Mathf.Clamp(Mathf.Abs(localMax.x - localMin.x), 88f, 440f),
            Mathf.Clamp(Mathf.Abs(localMax.y - localMin.y), 72f, 360f));

        center.x = Mathf.Clamp(center.x, canvasRect.rect.xMin + 58f, canvasRect.rect.xMax - 58f);
        center.y = Mathf.Clamp(center.y, canvasRect.rect.yMin + 58f, canvasRect.rect.yMax - 58f);
        return true;
    }

    private static bool TryGetWorldScreenBounds(GameObject target, out Vector2 screenMin, out Vector2 screenMax)
    {
        screenMin = Vector2.zero;
        screenMax = Vector2.zero;
        Camera camera = Camera.main;
        if (camera == null)
            return false;

        Bounds bounds = default;
        bool hasBounds = false;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null
                || !renderer.enabled
                || renderer.gameObject.name.Contains("Attention Glow"))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
        {
            Collider2D[] colliders = target.GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null || !colliders[i].enabled)
                    continue;

                if (!hasBounds)
                {
                    bounds = colliders[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(colliders[i].bounds);
                }
            }
        }

        if (!hasBounds)
            bounds = new Bounds(target.transform.position, new Vector3(1.2f, 1.2f, 0f));

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3[] corners =
        {
            new Vector3(min.x, min.y, bounds.center.z),
            new Vector3(min.x, max.y, bounds.center.z),
            new Vector3(max.x, min.y, bounds.center.z),
            new Vector3(max.x, max.y, bounds.center.z)
        };

        Vector3 first = camera.WorldToScreenPoint(corners[0]);
        if (first.z < 0f)
            return false;

        screenMin = first;
        screenMax = first;
        for (int i = 1; i < corners.Length; i++)
        {
            Vector3 screen = camera.WorldToScreenPoint(corners[i]);
            if (screen.z < 0f)
                continue;

            screenMin = Vector2.Min(screenMin, screen);
            screenMax = Vector2.Max(screenMax, screen);
        }

        return true;
    }

    private void UpdatePath(bool destinationActive)
    {
        bool pathActive = destinationActive
            && sourceTargets.Count > 0
            && sourceMarkers[0].Root.gameObject.activeSelf;

        for (int i = 0; i < pathDots.Count; i++)
            pathDots[i].gameObject.SetActive(pathActive);

        if (!pathActive)
            return;

        Vector2 start = sourceMarkers[0].Root.anchoredPosition;
        Vector2 end = destinationMarker.Root.anchoredPosition;
        float phase = Mathf.Repeat(Time.unscaledTime * 0.7f, 1f);

        for (int i = 0; i < pathDots.Count; i++)
        {
            float t = (i + 1f) / (pathDots.Count + 1f);
            RectTransform dot = pathDots[i];
            dot.anchoredPosition = Vector2.Lerp(start, end, t);

            float distance = Mathf.Abs(Mathf.Repeat(t - phase + 0.5f, 1f) - 0.5f) * 2f;
            float wave = Mathf.Pow(1f - distance, 4f);
            float dotScale = Mathf.Lerp(0.72f, 1.35f, wave);
            dot.localScale = Vector3.one * dotScale;

            Image image = dot.GetComponent<Image>();
            Color color = Color.Lerp(SourceColor, DestinationColor, t);
            color.a = Mathf.Lerp(0.38f, 1f, wave);
            image.color = color;
        }
    }

    private static void EnsureSprites()
    {
        if (ringSprite == null)
            ringSprite = BuildRingSprite();

        if (arrowSprite == null)
            arrowSprite = BuildArrowSprite();
    }

    private static Sprite BuildRingSprite()
    {
        const int size = 128;
        const float outerRadius = 61f;
        const float innerRadius = 54f;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Child Guide Ring",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        var pixels = new Color32[size * size];
        Vector2 center = new Vector2((size - 1f) * 0.5f, (size - 1f) * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float outer = 1f - Mathf.Clamp01(distance - outerRadius + 1f);
                float inner = Mathf.Clamp01(distance - innerRadius + 1f);
                byte alpha = (byte)Mathf.RoundToInt(255f * outer * inner);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.name = "Child Guide Ring";
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static Sprite BuildArrowSprite()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Child Guide Arrow",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            float halfWidth = Mathf.Lerp(0f, size * 0.48f, y / (size - 1f));
            float center = (size - 1f) * 0.5f;
            for (int x = 0; x < size; x++)
            {
                bool inside = Mathf.Abs(x - center) <= halfWidth;
                pixels[y * size + x] = inside
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.name = "Child Guide Arrow";
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
