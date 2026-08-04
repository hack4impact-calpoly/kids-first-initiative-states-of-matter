using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SceneVisualPolishController : MonoBehaviour, IFlowSceneController
{
    private const float ReferenceAspect = 16f / 9f;

    private string sceneName;
    private Camera sceneCamera;
    private SpriteRenderer backgroundRenderer;
    private Vector3 backgroundBaseScale;
    private float baseOrthographicSize;
    private float lastAspect = -1f;
    private int startupPolishFrames;
    private bool initialized;

    public void InitializeFlow()
    {
        if (initialized)
            return;

        initialized = true;
        sceneName = SceneManager.GetActiveScene().name;
        sceneCamera = Camera.main;

        if (sceneCamera != null)
        {
            baseOrthographicSize = sceneCamera.orthographicSize;
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = ResolveClearColor();
        }

        backgroundRenderer = FindBackgroundRenderer();
        if (backgroundRenderer != null)
            backgroundBaseScale = backgroundRenderer.transform.localScale;

        PolishLegacyControls();
        PolishActivityObjects();
        startupPolishFrames = 120;
        ApplyFraming(true);
    }

    private void LateUpdate()
    {
        if (backgroundRenderer == null && startupPolishFrames > 0)
        {
            backgroundRenderer = FindBackgroundRenderer();
            if (backgroundRenderer != null)
            {
                backgroundBaseScale = backgroundRenderer.transform.localScale;
                lastAspect = -1f;
            }
        }

        if (startupPolishFrames > 0)
        {
            PolishLegacyControls();
            PolishActivityObjects();
            startupPolishFrames--;
        }

        if (sceneName == ActivityFlowCatalog.LabScene)
            StyleLabPowerControl();

        if (sceneName == ActivityFlowCatalog.PipeScene)
            HidePipeCoordinateLabels();

        ApplyFraming(false);
    }

    private void ApplyFraming(bool force)
    {
        if (sceneCamera == null)
            return;

        float aspect = sceneCamera.pixelHeight > 0
            ? sceneCamera.pixelWidth / (float)sceneCamera.pixelHeight
            : sceneCamera.aspect;
        if (aspect <= 0f || (!force && Mathf.Abs(aspect - lastAspect) < 0.001f))
            return;

        lastAspect = aspect;

        if (sceneCamera.orthographic && IsGameplayScene())
        {
            float aspectScale = aspect < ReferenceAspect ? ReferenceAspect / aspect : 1f;
            sceneCamera.orthographicSize = baseOrthographicSize * aspectScale;
        }

        if (backgroundRenderer == null || sceneName == ActivityFlowCatalog.PipeScene)
            return;

        if (sceneName == ActivityFlowCatalog.SelectorScene)
            ApplySelectorFraming(aspect);
        else
            ScaleBackgroundToCover();
    }

    private void ApplySelectorFraming(float aspect)
    {
        backgroundRenderer.transform.localScale = backgroundBaseScale;
        sceneCamera.orthographicSize = baseOrthographicSize;

        Bounds bounds = backgroundRenderer.bounds;
        float backgroundAspect = bounds.size.y > 0.01f
            ? bounds.size.x / bounds.size.y
            : ReferenceAspect;
        if (aspect >= backgroundAspect)
        {
            ScaleBackgroundToCover();
            return;
        }

        float requiredHalfHeight = bounds.size.x / (2f * aspect);
        sceneCamera.orthographicSize = Mathf.Max(
            baseOrthographicSize,
            requiredHalfHeight * 1.04f);
    }

    private void ScaleBackgroundToCover()
    {
        backgroundRenderer.transform.localScale = backgroundBaseScale;
        Bounds bounds = backgroundRenderer.bounds;
        if (bounds.size.x <= 0.01f || bounds.size.y <= 0.01f)
            return;

        float cameraHeight = sceneCamera.orthographic
            ? sceneCamera.orthographicSize * 2f
            : bounds.size.y;
        float cameraWidth = cameraHeight * sceneCamera.aspect;
        float coverScale = Mathf.Max(
            1f,
            Mathf.Max(cameraWidth / bounds.size.x, cameraHeight / bounds.size.y));

        Vector3 scale = backgroundBaseScale;
        scale.x *= coverScale;
        scale.y *= coverScale;
        backgroundRenderer.transform.localScale = scale;
    }

    private bool IsGameplayScene()
    {
        return !string.IsNullOrEmpty(ActivityFlowCatalog.GetActivityForScene(sceneName));
    }

    private Color ResolveClearColor()
    {
        if (sceneName == ActivityFlowCatalog.PipeScene)
            return new Color32(190, 220, 224, 255);

        if (sceneName == ActivityFlowCatalog.LabScene)
            return new Color32(55, 103, 153, 255);

        if (sceneName == ActivityFlowCatalog.KitchenSolidScene
            || sceneName == ActivityFlowCatalog.KitchenPourScene
            || sceneName == ActivityFlowCatalog.KitchenFreezeScene
            || sceneName == ActivityFlowCatalog.SelectorScene)
        {
            return new Color32(228, 226, 191, 255);
        }

        return new Color32(119, 207, 238, 255);
    }

    private static SpriteRenderer FindBackgroundRenderer()
    {
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        SpriteRenderer best = null;
        float bestArea = 0f;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null
                || renderer.sprite == null
                || renderer.gameObject.name.Contains("Attention Glow"))
            {
                continue;
            }

            float area = renderer.bounds.size.x * renderer.bounds.size.y;
            if (area <= bestArea)
                continue;

            best = renderer;
            bestArea = area;
        }

        return best;
    }

    private void PolishLegacyControls()
    {
        if (!IsGameplayScene())
            return;

        Button[] buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].GetComponentInParent<ActivityFlowController>() != null)
                continue;

            string label = ResolveButtonLabel(buttons[i]);
            if (label == "BACK"
                || label == "MENU"
                || label == "RETRY"
                || label == "UNDO"
                || label == "RESTART")
            {
                buttons[i].gameObject.SetActive(false);
            }
        }

        if (sceneName != ActivityFlowCatalog.PipeScene)
            return;

        HidePipeCoordinateLabels();
    }

    private static void HidePipeCoordinateLabels()
    {
        TMP_Text[] labels = FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < labels.Length; i++)
        {
            string value = labels[i].text;
            string objectName = labels[i].gameObject.name;
            bool coordinateLabel = objectName == "x-axis"
                || objectName == "y-axis"
                || objectName == "GameLevel";
            bool singleNumber = !string.IsNullOrWhiteSpace(value)
                && int.TryParse(value.Trim(), out int number)
                && number >= 1
                && number <= 8;
            if (coordinateLabel || singleNumber)
            {
                labels[i].gameObject.SetActive(false);
            }
        }
    }

    private static string ResolveButtonLabel(Button button)
    {
        if (button == null)
            return string.Empty;

        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
            return tmp.text.Trim().ToUpperInvariant();

        Text legacy = button.GetComponentInChildren<Text>(true);
        return legacy != null && !string.IsNullOrWhiteSpace(legacy.text)
            ? legacy.text.Trim().ToUpperInvariant()
            : string.Empty;
    }

    private void PolishActivityObjects()
    {
        if (!IsGameplayScene())
            return;

        StyleIngredientTray();
        StyleHeatControl();
        StyleCanvasBackdrop();
        StrengthenContainerArt();
    }

    private static void StyleCanvasBackdrop()
    {
        GameObject backdropObject = GameObject.Find("Canvas/Image");
        if (backdropObject == null)
            return;

        Image backdrop = backdropObject.GetComponent<Image>();
        RectTransform rect = backdropObject.GetComponent<RectTransform>();
        RectTransform parent = rect != null ? rect.parent as RectTransform : null;
        if (backdrop == null
            || backdrop.sprite == null
            || rect == null
            || parent == null
            || rect.rect.width < 800f
            || rect.rect.height < 600f)
        {
            return;
        }

        float spriteAspect = backdrop.sprite.rect.height > 0.01f
            ? backdrop.sprite.rect.width / backdrop.sprite.rect.height
            : ReferenceAspect;
        Vector2 parentSize = parent.rect.size;
        float parentAspect = parentSize.y > 0.01f
            ? parentSize.x / parentSize.y
            : ReferenceAspect;

        Vector2 coverSize;
        if (parentAspect > spriteAspect)
            coverSize = new Vector2(parentSize.x, parentSize.x / spriteAspect);
        else
            coverSize = new Vector2(parentSize.y * spriteAspect, parentSize.y);

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = coverSize;
        rect.localScale = Vector3.one;
        backdrop.preserveAspect = false;
    }

    private static void StyleIngredientTray()
    {
        IngredientBarDragToWorld2D ingredient = FindAnyObjectByType<IngredientBarDragToWorld2D>();
        if (ingredient == null)
            return;

        Image trayPanel = null;
        Transform current = ingredient.transform.parent;
        while (current != null && current.GetComponent<Canvas>() == null)
        {
            Image image = current.GetComponent<Image>();
            RectTransform rect = current as RectTransform;
            if (image != null && rect != null && rect.rect.height > 300f)
            {
                trayPanel = image;
                break;
            }

            current = current.parent;
        }

        if (trayPanel == null)
            return;

        RectTransform panelRect = trayPanel.rectTransform;
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(150f, -112f);
        panelRect.sizeDelta = new Vector2(190f, 320f);

        RectTransform ingredientRect = ingredient.transform as RectTransform;
        if (ingredientRect != null)
        {
            ingredientRect.anchorMin = new Vector2(0.5f, 1f);
            ingredientRect.anchorMax = new Vector2(0.5f, 1f);
            ingredientRect.pivot = new Vector2(0.5f, 0.5f);
            ingredientRect.anchoredPosition = new Vector2(0f, -160f);
        }

        trayPanel.sprite = FlowUiFactory.GetUiSprite();
        trayPanel.type = Image.Type.Sliced;
        trayPanel.color = new Color32(20, 58, 82, 218);
        trayPanel.raycastTarget = false;

        Outline outline = trayPanel.GetComponent<Outline>();
        if (outline == null)
            outline = trayPanel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(113, 202, 230, 190);
        outline.effectDistance = new Vector2(3f, -3f);

        if (panelRect.Find("Ingredient Label") == null)
        {
            TextMeshProUGUI label = FlowUiFactory.CreateText(
                panelRect,
                "Ingredient Label",
                "INGREDIENT",
                22f,
                TextAlignmentOptions.Center,
                FlowUiFactory.White);
            label.rectTransform.anchorMin = new Vector2(0f, 1f);
            label.rectTransform.anchorMax = new Vector2(1f, 1f);
            label.rectTransform.pivot = new Vector2(0.5f, 1f);
            label.rectTransform.offsetMin = new Vector2(12f, -62f);
            label.rectTransform.offsetMax = new Vector2(-12f, -16f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }

    private static void StyleHeatControl()
    {
        HeatController heat = FindAnyObjectByType<HeatController>();
        Slider slider = heat != null ? heat.GetComponent<Slider>() : null;
        if (slider == null)
            return;

        RectTransform root = slider.GetComponent<RectTransform>();
        root.sizeDelta = new Vector2(420f, 52f);

        Transform backgroundTransform = slider.transform.Find("Background");
        Image background = backgroundTransform != null
            ? backgroundTransform.GetComponent<Image>()
            : null;
        if (background != null)
        {
            background.sprite = FlowUiFactory.GetUiSprite();
            background.type = Image.Type.Sliced;
            background.color = new Color32(22, 55, 78, 245);

            Outline outline = background.GetComponent<Outline>();
            if (outline == null)
                outline = background.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(8, 36, 62, 220);
            outline.effectDistance = new Vector2(3f, -3f);
        }

        Image fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
        if (fill != null)
        {
            fill.sprite = FlowUiFactory.GetUiSprite();
            fill.type = Image.Type.Sliced;
            fill.color = FlowUiFactory.Orange;
        }

        Image handle = slider.handleRect != null ? slider.handleRect.GetComponent<Image>() : null;
        if (handle != null)
        {
            handle.sprite = FlowUiFactory.GetUiSprite();
            handle.type = Image.Type.Sliced;
            handle.color = FlowUiFactory.Gold;
            handle.rectTransform.sizeDelta = new Vector2(36f, 58f);
            slider.targetGraphic = handle;
        }

        CreateSliderLabel(root, "Low Label", "LOW", new Vector2(0f, 1f), TextAlignmentOptions.Left, FlowUiFactory.White);
        CreateSliderLabel(root, "Hot Label", "HOT", new Vector2(1f, 1f), TextAlignmentOptions.Right, FlowUiFactory.Orange);
    }

    private static void CreateSliderLabel(
        RectTransform root,
        string objectName,
        string value,
        Vector2 anchor,
        TextAlignmentOptions alignment,
        Color color)
    {
        if (root.Find(objectName) != null)
            return;

        TextMeshProUGUI label = FlowUiFactory.CreateText(
            root,
            objectName,
            value,
            22f,
            alignment,
            color);
        label.rectTransform.anchorMin = anchor;
        label.rectTransform.anchorMax = anchor;
        label.rectTransform.pivot = new Vector2(anchor.x, 0f);
        label.rectTransform.sizeDelta = new Vector2(110f, 34f);
        label.rectTransform.anchoredPosition = new Vector2(0f, 30f);
        label.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private static bool StyleLabPowerControl()
    {
        GameObject dialObject = GameObject.Find("Power Dial");
        if (dialObject == null)
            return false;

        RectTransform dial = dialObject.GetComponent<RectTransform>();
        Image panel = dialObject.GetComponent<Image>();
        Slider slider = dialObject.GetComponentInChildren<Slider>(true);
        if (dial == null || panel == null || slider == null)
            return false;

        dial.anchorMin = new Vector2(0.5f, 1f);
        dial.anchorMax = new Vector2(0.5f, 1f);
        dial.pivot = new Vector2(0.5f, 1f);
        dial.anchoredPosition = new Vector2(-24f, -108f);
        dial.sizeDelta = new Vector2(540f, 100f);

        panel.sprite = FlowUiFactory.GetUiSprite();
        panel.type = Image.Type.Sliced;
        panel.color = new Color32(20, 51, 94, 235);

        Outline panelOutline = dialObject.GetComponent<Outline>();
        if (panelOutline == null)
            panelOutline = dialObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color32(8, 36, 62, 220);
        panelOutline.effectDistance = new Vector2(3f, -3f);

        TMP_Text powerLabel = dial.Find("Power Label")?.GetComponent<TMP_Text>();
        StyleDialLabel(
            powerLabel,
            new Vector2(-198f, -50f),
            new Vector2(124f, 52f),
            FlowUiFactory.Gold);

        TMP_Text statusLabel = dial.Find("Power Status")?.GetComponent<TMP_Text>();
        StyleDialLabel(
            statusLabel,
            new Vector2(218f, -50f),
            new Vector2(100f, 52f),
            FlowUiFactory.White);

        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(24f, -50f);
        sliderRect.sizeDelta = new Vector2(260f, 54f);
        slider.direction = Slider.Direction.LeftToRight;

        RectTransform backgroundRect = slider.transform.Find("Background") as RectTransform;
        if (backgroundRect != null)
        {
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.offsetMin = new Vector2(0f, -12f);
            backgroundRect.offsetMax = new Vector2(0f, 12f);

            Image background = backgroundRect.GetComponent<Image>();
            if (background != null)
            {
                background.sprite = FlowUiFactory.GetUiSprite();
                background.type = Image.Type.Sliced;
                background.color = new Color32(8, 36, 62, 255);
            }
        }

        RectTransform fillArea = slider.transform.Find("Fill Area") as RectTransform;
        SetHorizontalSliderArea(fillArea, 8f, 10f);

        RectTransform handleArea = slider.transform.Find("Handle Slide Area") as RectTransform;
        SetHorizontalSliderArea(handleArea, 14f, 0f);

        Image fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
        if (fill != null)
        {
            fill.sprite = FlowUiFactory.GetUiSprite();
            fill.type = Image.Type.Sliced;
            fill.color = FlowUiFactory.Orange;
        }

        Image handle = slider.handleRect != null ? slider.handleRect.GetComponent<Image>() : null;
        if (handle != null)
        {
            handle.sprite = FlowUiFactory.GetUiSprite();
            handle.type = Image.Type.Sliced;
            handle.color = FlowUiFactory.Gold;
            handle.rectTransform.sizeDelta = new Vector2(36f, 58f);
            slider.targetGraphic = handle;
        }

        return true;
    }

    private static void StyleDialLabel(
        TMP_Text label,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        if (label == null)
            return;

        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        label.fontSize = 24f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 16f;
        label.fontSizeMax = 24f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
    }

    private static void SetHorizontalSliderArea(
        RectTransform area,
        float horizontalInset,
        float verticalInset)
    {
        if (area == null)
            return;

        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(horizontalInset, verticalInset);
        area.offsetMax = new Vector2(-horizontalInset, -verticalInset);
    }

    private static void StrengthenContainerArt()
    {
        MockPotController[] containers = FindObjectsByType<MockPotController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < containers.Length; i++)
        {
            SpriteRenderer[] renderers = containers[i].GetComponentsInChildren<SpriteRenderer>(true);
            for (int j = 0; j < renderers.Length; j++)
                renderers[j].color = Color.white;
        }
    }
}
