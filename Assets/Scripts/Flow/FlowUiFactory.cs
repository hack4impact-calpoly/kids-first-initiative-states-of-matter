using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class FlowUiFactory
{
    public static readonly Color Navy = new Color32(20, 51, 94, 244);
    public static readonly Color Blue = new Color32(24, 157, 232, 255);
    public static readonly Color Green = new Color32(54, 174, 91, 255);
    public static readonly Color Orange = new Color32(245, 143, 36, 255);
    public static readonly Color Purple = new Color32(133, 75, 190, 255);
    public static readonly Color Gold = new Color32(255, 210, 54, 255);
    public static readonly Color White = new Color32(255, 255, 255, 255);
    public static readonly Color Ink = new Color32(23, 39, 59, 255);

    private static Sprite uiSprite;
    private static Sprite buttonSprite;

    public static Canvas CreateCanvas(string name, int sortingOrder)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static Image CreatePanel(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        Color color)
    {
        var panelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Image image = panelObject.GetComponent<Image>();
        image.sprite = GetUiSprite();
        image.type = Image.Type.Sliced;
        image.color = color;
        return image;
    }

    public static TextMeshProUGUI CreateText(
        Transform parent,
        string name,
        string value,
        float fontSize,
        TextAlignmentOptions alignment,
        Color color)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontMaterial = new Material(text.fontSharedMaterial)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(12f, fontSize * 0.62f);
        text.fontSizeMax = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.color = color;
        text.faceColor = color;
        text.fontMaterial.SetColor(ShaderUtilities.ID_FaceColor, color);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.margin = new Vector4(8f, 4f, 8f, 4f);
        text.raycastTarget = false;
        Stretch(text.rectTransform);
        return text;
    }

    public static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Color color,
        UnityAction onClick)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = GetButtonSprite();
        image.type = Image.Type.Sliced;
        image.color = color;

        Outline outline = buttonObject.GetComponent<Outline>();
        outline.effectColor = new Color32(8, 48, 103, 220);
        outline.effectDistance = new Vector2(3f, -3f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.86f);
        colors.pressedColor = new Color(0.78f, 0.88f, 1f, 1f);
        colors.selectedColor = Color.white;
        colors.colorMultiplier = 1f;
        button.colors = colors;

        if (onClick != null)
            button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, 28f, TextAlignmentOptions.Center, White);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.outlineColor = new Color32(10, 45, 97, 255);
        text.outlineWidth = 0.2f;
        return button;
    }

    public static RectTransform SetRect(
        Component component,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        RectTransform rect = component.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void AddProgressDots(Transform parent, string activityId, Color activeColor)
    {
        int stageCount = ActivityFlowCatalog.GetStageCount(activityId);
        int completeCount = ActivityFlowCatalog.GetCompletedStageCount(activityId);
        if (stageCount <= 0)
            return;

        var rowObject = new GameObject("Progress Dots", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowObject.transform.SetParent(parent, false);

        RectTransform row = rowObject.GetComponent<RectTransform>();
        row.anchorMin = new Vector2(0.5f, 0f);
        row.anchorMax = new Vector2(0.5f, 0f);
        row.pivot = new Vector2(0.5f, 0f);
        row.anchoredPosition = new Vector2(0f, 12f);
        row.sizeDelta = new Vector2(stageCount * 30f, 20f);

        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        for (int i = 0; i < stageCount; i++)
        {
            var dot = new GameObject("Dot " + (i + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            dot.transform.SetParent(rowObject.transform, false);
            RectTransform dotRect = dot.GetComponent<RectTransform>();
            dotRect.sizeDelta = new Vector2(18f, 18f);

            LayoutElement element = dot.GetComponent<LayoutElement>();
            element.preferredWidth = 18f;
            element.preferredHeight = 18f;

            Image image = dot.GetComponent<Image>();
            image.sprite = GetUiSprite();
            image.type = Image.Type.Sliced;
            image.color = i < completeCount ? activeColor : new Color(1f, 1f, 1f, 0.46f);
            image.raycastTarget = false;
        }
    }

    public static Sprite GetUiSprite()
    {
        if (uiSprite == null)
        {
            const int size = 64;
            const float radius = 14f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Activity Flow Panel",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[size * size];
            Vector2 center = new Vector2((size - 1f) * 0.5f, (size - 1f) * 0.5f);
            Vector2 straight = new Vector2(size * 0.5f - radius, size * 0.5f - radius);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 delta = new Vector2(
                        Mathf.Max(Mathf.Abs(x - center.x) - straight.x, 0f),
                        Mathf.Max(Mathf.Abs(y - center.y) - straight.y, 0f));
                    pixels[y * size + x] = delta.magnitude <= radius
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            uiSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(16f, 16f, 16f, 16f));
            uiSprite.name = "Activity Flow Panel";
            uiSprite.hideFlags = HideFlags.HideAndDontSave;
        }

        return uiSprite;
    }

    public static Sprite GetButtonSprite()
    {
        if (buttonSprite != null)
            return buttonSprite;

        const int width = 256;
        const int height = 64;
        const float radius = 29f;
        const float border = 5f;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "Activity Flow Button",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color32 borderColor = new Color32(72, 72, 72, 255);
        Color32 bottomColor = new Color32(224, 224, 224, 255);
        Color32 topColor = new Color32(255, 255, 255, 255);
        var pixels = new Color32[width * height];
        Vector2 center = new Vector2((width - 1f) * 0.5f, (height - 1f) * 0.5f);
        Vector2 straight = new Vector2(width * 0.5f - radius, height * 0.5f - radius);

        for (int y = 0; y < height; y++)
        {
            float vertical = y / (height - 1f);
            for (int x = 0; x < width; x++)
            {
                Vector2 delta = new Vector2(
                    Mathf.Max(Mathf.Abs(x - center.x) - straight.x, 0f),
                    Mathf.Max(Mathf.Abs(y - center.y) - straight.y, 0f));
                float distance = delta.magnitude;

                if (distance > radius)
                {
                    pixels[y * width + x] = new Color32(0, 0, 0, 0);
                    continue;
                }

                if (distance > radius - border)
                {
                    pixels[y * width + x] = borderColor;
                    continue;
                }

                Color fill = Color.Lerp(bottomColor, topColor, vertical);
                if (y > height * 0.72f)
                    fill = Color.Lerp(fill, Color.white, 0.18f);
                pixels[y * width + x] = fill;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        buttonSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(32f, 32f, 32f, 32f));
        buttonSprite.name = "Activity Flow Button";
        buttonSprite.hideFlags = HideFlags.HideAndDontSave;
        return buttonSprite;
    }
}
