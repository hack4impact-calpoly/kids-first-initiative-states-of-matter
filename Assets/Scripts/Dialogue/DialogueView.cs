using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum DialogueBubblePlacement
{
    Bottom,
    Top
}

public class DialogueView : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private DialogueRoundedRectGraphic shadowGraphic;
    [SerializeField] private DialogueRoundedRectGraphic bubbleGraphic;
    [SerializeField] private DialogueRoundedRectGraphic bubbleFillGraphic;
    [SerializeField] private DialogueRoundedRectGraphic portraitFrameGraphic;
    [SerializeField] private DialogueRoundedRectGraphic nameplateGraphic;
    [SerializeField] private DialogueRoundedRectGraphic continuePromptFrameGraphic;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject continuePrompt;

    [Header("Default Bubble Layout")]
    [SerializeField] private bool buildDefaultLayoutIfMissing = true;
    [SerializeField] private DialogueBubblePlacement placement = DialogueBubblePlacement.Bottom;
    [SerializeField] private bool stretchToParentWidth = true;
    [SerializeField] private Vector2 bubbleSize = new Vector2(980f, 132f);
    [SerializeField] private Vector2 bubbleOffset = new Vector2(0f, 10f);
    [SerializeField] private float horizontalMargin = 16f;
    [SerializeField] private float borderThickness = 3f;
    [SerializeField] private float portraitSize = 90f;
    [SerializeField] private Color shadowColor = new Color(0.03f, 0.03f, 0.08f, 0.24f);
    [SerializeField] private Color bubbleColor = new Color(0.98f, 0.96f, 1f, 0.99f);
    [SerializeField] private Color borderColor = new Color(0.35f, 0.28f, 0.6f, 1f);
    [SerializeField] private Color portraitFrameColor = new Color(1f, 0.98f, 1f, 1f);
    [SerializeField] private Color nameplateColor = new Color(0.89f, 0.83f, 0.96f, 1f);
    [SerializeField] private Color continuePromptFrameColor = new Color(0.91f, 0.86f, 0.98f, 1f);
    [SerializeField] private Color speakerNameColor = new Color(0.14f, 0.11f, 0.31f, 1f);
    [SerializeField] private Color bodyTextColor = new Color(0.08f, 0.06f, 0.11f, 1f);
    [SerializeField] private Color warningBorderColor = new Color(0.82f, 0.32f, 0.08f, 1f);
    [SerializeField] private Color warningNameplateColor = new Color(1f, 0.86f, 0.75f, 1f);
    [SerializeField] private Color warningSpeakerNameColor = new Color(0.82f, 0.32f, 0.08f, 1f);
    [SerializeField] private Color warningBodyTextColor = new Color(0.18f, 0.08f, 0.02f, 1f);

    public event Action ContinueRequested;

    private void Awake()
    {
        if (ShouldBuildDefaultLayout())
            BuildDefaultLayout();

        if (root == null)
            root = gameObject;

        CacheRootGraphics();
        ApplyPlacement();
        Hide();
    }

    private void OnEnable()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(RequestContinue);
    }

    private void OnDisable()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(RequestContinue);
    }

    public void ShowLine(DialogueLine line, bool canContinue)
    {
        if (line == null)
        {
            Hide();
            return;
        }

        ShowContent(line.SpeakerName, line.Text, line.Portrait, false, canContinue);
    }

    public void ShowPrompt(string speakerName, string text, Sprite portrait, bool isWarning, bool canContinue)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Hide();
            return;
        }

        ShowContent(speakerName, text, portrait, isWarning, canContinue);
    }

    public void SetPlacement(DialogueBubblePlacement newPlacement)
    {
        placement = newPlacement;
        ApplyPlacement();
    }

    private void ShowContent(string speakerName, string text, Sprite portrait, bool isWarning, bool canContinue)
    {
        SetRootActive(true);
        ApplyStyle(isWarning);

        if (speakerNameText != null)
        {
            speakerNameText.text = speakerName;
            speakerNameText.gameObject.SetActive(!string.IsNullOrWhiteSpace(speakerName));
        }

        if (nameplateGraphic != null)
            nameplateGraphic.gameObject.SetActive(!string.IsNullOrWhiteSpace(speakerName));

        if (bodyText != null)
            bodyText.text = text;

        if (portraitImage != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
        }

        if (portraitFrameGraphic != null)
            portraitFrameGraphic.gameObject.SetActive(portrait != null);

        ApplyContentLayout(portrait != null);
        SetContinueEnabled(canContinue);
    }

    public void SetContinueEnabled(bool canContinue)
    {
        if (continueButton != null)
            continueButton.interactable = canContinue;

        if (continuePromptFrameGraphic != null)
            continuePromptFrameGraphic.gameObject.SetActive(canContinue);

        if (continuePrompt != null)
            continuePrompt.SetActive(canContinue);
    }

    public void Hide()
    {
        SetRootActive(false);
    }

    private void SetRootActive(bool active)
    {
        if (root != null)
            root.SetActive(active);
    }

    private void RequestContinue()
    {
        ContinueRequested?.Invoke();
    }

    private bool ShouldBuildDefaultLayout()
    {
        return buildDefaultLayoutIfMissing
            && (root == null || speakerNameText == null || bodyText == null || portraitImage == null || continueButton == null);
    }

    private void BuildDefaultLayout()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvas = canvas.rootCanvas;

        if (canvas == null)
        {
            var canvasObject = new GameObject("Dialogue Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject bubbleObject = CreateUiObject("Dialogue Bubble", canvas.transform);
        root = bubbleObject;

        RectTransform bubbleRect = bubbleObject.GetComponent<RectTransform>();
        bubbleRect.sizeDelta = bubbleSize;

        GameObject shadowObject = CreateUiObject("Dialogue Shadow", bubbleRect);
        RectTransform shadowRect = shadowObject.GetComponent<RectTransform>();
        StretchToFill(shadowRect);
        shadowRect.offsetMin = new Vector2(6f, -8f);
        shadowRect.offsetMax = new Vector2(6f, -8f);
        shadowObject.AddComponent<CanvasRenderer>();
        shadowGraphic = shadowObject.AddComponent<DialogueRoundedRectGraphic>();
        shadowGraphic.CornerRadius = 16f;
        shadowGraphic.color = shadowColor;
        shadowGraphic.raycastTarget = false;

        GameObject panelObject = CreateUiObject("Dialogue Panel", bubbleRect);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        StretchToFill(panelRect);
        panelObject.AddComponent<CanvasRenderer>();
        bubbleGraphic = panelObject.AddComponent<DialogueRoundedRectGraphic>();
        bubbleGraphic.CornerRadius = 12f;
        bubbleGraphic.color = borderColor;

        GameObject fillObject = CreateUiObject("Bubble Fill", panelRect);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.one * borderThickness;
        fillRect.offsetMax = -Vector2.one * borderThickness;

        fillObject.AddComponent<CanvasRenderer>();
        bubbleFillGraphic = fillObject.AddComponent<DialogueRoundedRectGraphic>();
        bubbleFillGraphic.CornerRadius = 12f;
        bubbleFillGraphic.color = bubbleColor;
        bubbleFillGraphic.raycastTarget = false;

        continueButton = bubbleObject.AddComponent<Button>();
        continueButton.targetGraphic = bubbleGraphic;

        BuildPortrait(bubbleRect);
        BuildSpeakerName(bubbleRect);
        BuildBodyText(bubbleRect);
        BuildContinuePrompt(bubbleRect);
    }

    private void CacheRootGraphics()
    {
        if (root == null)
            return;

        if (bubbleGraphic == null)
            bubbleGraphic = root.transform.Find("Dialogue Panel")?.GetComponent<DialogueRoundedRectGraphic>()
                ?? root.GetComponent<DialogueRoundedRectGraphic>();

        if (bubbleFillGraphic == null)
            bubbleFillGraphic = root.transform.Find("Dialogue Panel/Bubble Fill")?.GetComponent<DialogueRoundedRectGraphic>()
                ?? root.transform.Find("Bubble Fill")?.GetComponent<DialogueRoundedRectGraphic>();

        if (shadowGraphic == null)
            shadowGraphic = root.transform.Find("Dialogue Shadow")?.GetComponent<DialogueRoundedRectGraphic>();

        if (portraitFrameGraphic == null)
            portraitFrameGraphic = root.transform.Find("Portrait Frame")?.GetComponent<DialogueRoundedRectGraphic>();

        if (nameplateGraphic == null)
            nameplateGraphic = root.transform.Find("Nameplate")?.GetComponent<DialogueRoundedRectGraphic>();

        if (continuePromptFrameGraphic == null)
            continuePromptFrameGraphic = root.transform.Find("Continue Prompt Frame")?.GetComponent<DialogueRoundedRectGraphic>();
    }

    private void ApplyPlacement()
    {
        if (root == null)
            return;

        RectTransform rect = root.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.sizeDelta = stretchToParentWidth ? new Vector2(-horizontalMargin * 2f, bubbleSize.y) : bubbleSize;

        if (placement == DialogueBubblePlacement.Top)
        {
            rect.anchorMin = stretchToParentWidth ? new Vector2(0f, 1f) : new Vector2(0.5f, 1f);
            rect.anchorMax = stretchToParentWidth ? new Vector2(1f, 1f) : new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(bubbleOffset.x, -bubbleOffset.y);
            return;
        }

        rect.anchorMin = stretchToParentWidth ? new Vector2(0f, 0f) : new Vector2(0.5f, 0f);
        rect.anchorMax = stretchToParentWidth ? new Vector2(1f, 0f) : new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = bubbleOffset;
    }

    private void ApplyStyle(bool isWarning)
    {
        Color resolvedBorderColor = isWarning ? warningBorderColor : borderColor;

        if (shadowGraphic != null)
            shadowGraphic.color = shadowColor;

        if (bubbleGraphic != null)
            bubbleGraphic.color = resolvedBorderColor;

        if (bubbleFillGraphic != null)
            bubbleFillGraphic.color = bubbleColor;

        if (portraitFrameGraphic != null)
            portraitFrameGraphic.color = portraitFrameColor;

        if (nameplateGraphic != null)
            nameplateGraphic.color = isWarning ? warningNameplateColor : nameplateColor;

        if (continuePromptFrameGraphic != null)
            continuePromptFrameGraphic.color = isWarning ? warningNameplateColor : continuePromptFrameColor;

        if (speakerNameText != null)
            speakerNameText.color = isWarning ? warningSpeakerNameColor : speakerNameColor;

        if (bodyText != null)
            bodyText.color = isWarning ? warningBodyTextColor : bodyTextColor;

        if (continuePrompt == null)
            return;

        TMP_Text promptText = continuePrompt.GetComponent<TMP_Text>();
        if (promptText != null)
            promptText.color = resolvedBorderColor;
    }

    private void BuildPortrait(RectTransform bubbleRect)
    {
        GameObject frameObject = CreateUiObject("Portrait Frame", bubbleRect);
        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0f, 0.5f);
        frameRect.anchorMax = new Vector2(0f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.anchoredPosition = new Vector2(64f, 0f);
        frameRect.sizeDelta = new Vector2(108f, 102f);

        frameObject.AddComponent<CanvasRenderer>();
        portraitFrameGraphic = frameObject.AddComponent<DialogueRoundedRectGraphic>();
        portraitFrameGraphic.CornerRadius = 10f;
        portraitFrameGraphic.color = portraitFrameColor;
        portraitFrameGraphic.raycastTarget = false;

        GameObject portraitObject = CreateUiObject("Portrait", frameRect);
        RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0.5f, 0.5f);
        portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
        portraitRect.pivot = new Vector2(0.5f, 0.5f);
        portraitRect.anchoredPosition = Vector2.zero;
        portraitRect.sizeDelta = new Vector2(portraitSize, portraitSize);

        portraitImage = portraitObject.AddComponent<Image>();
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;
    }

    private void BuildSpeakerName(RectTransform bubbleRect)
    {
        GameObject nameplateObject = CreateUiObject("Nameplate", bubbleRect);
        RectTransform nameplateRect = nameplateObject.GetComponent<RectTransform>();
        nameplateRect.anchorMin = new Vector2(0f, 1f);
        nameplateRect.anchorMax = new Vector2(0f, 1f);
        nameplateRect.pivot = new Vector2(0f, 1f);
        nameplateRect.anchoredPosition = new Vector2(124f, -12f);
        nameplateRect.sizeDelta = new Vector2(164f, 31f);

        nameplateObject.AddComponent<CanvasRenderer>();
        nameplateGraphic = nameplateObject.AddComponent<DialogueRoundedRectGraphic>();
        nameplateGraphic.CornerRadius = 8f;
        nameplateGraphic.color = nameplateColor;
        nameplateGraphic.raycastTarget = false;

        GameObject textObject = CreateUiObject("Speaker Name", bubbleRect);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.offsetMin = new Vector2(140f, -40f);
        textRect.offsetMax = new Vector2(-72f, -14f);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = 18f;
        text.fontStyle = FontStyles.Bold;
        text.color = speakerNameColor;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = false;
        speakerNameText = text;
    }

    private void BuildBodyText(RectTransform bubbleRect)
    {
        GameObject textObject = CreateUiObject("Body Text", bubbleRect);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(140f, 23f);
        textRect.offsetMax = new Vector2(-82f, -55f);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = 21f;
        text.fontStyle = FontStyles.Italic;
        text.color = bodyTextColor;
        text.alignment = TextAlignmentOptions.Left;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.lineSpacing = 6f;
        text.raycastTarget = false;
        bodyText = text;
    }

    private void BuildContinuePrompt(RectTransform bubbleRect)
    {
        GameObject frameObject = CreateUiObject("Continue Prompt Frame", bubbleRect);
        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(1f, 0.5f);
        frameRect.anchorMax = new Vector2(1f, 0.5f);
        frameRect.pivot = new Vector2(1f, 0.5f);
        frameRect.anchoredPosition = new Vector2(-22f, -2f);
        frameRect.sizeDelta = new Vector2(42f, 42f);

        frameObject.AddComponent<CanvasRenderer>();
        continuePromptFrameGraphic = frameObject.AddComponent<DialogueRoundedRectGraphic>();
        continuePromptFrameGraphic.CornerRadius = 8f;
        continuePromptFrameGraphic.color = continuePromptFrameColor;
        continuePromptFrameGraphic.raycastTarget = false;

        GameObject promptObject = CreateUiObject("Continue Prompt", frameRect);
        RectTransform promptRect = promptObject.GetComponent<RectTransform>();
        promptRect.anchorMin = Vector2.zero;
        promptRect.anchorMax = Vector2.one;
        promptRect.offsetMin = Vector2.zero;
        promptRect.offsetMax = Vector2.zero;

        TextMeshProUGUI promptText = promptObject.AddComponent<TextMeshProUGUI>();
        promptText.text = ">";
        promptText.fontSize = 24f;
        promptText.fontStyle = FontStyles.Bold;
        promptText.color = borderColor;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.raycastTarget = false;
        continuePrompt = promptObject;
    }

    private void ApplyContentLayout(bool hasPortrait)
    {
        float textLeft = hasPortrait ? 140f : 34f;

        if (speakerNameText != null)
        {
            RectTransform speakerRect = speakerNameText.rectTransform;
            speakerRect.offsetMin = new Vector2(textLeft, -40f);
            speakerRect.offsetMax = new Vector2(-72f, -14f);
        }

        if (nameplateGraphic != null)
        {
            RectTransform nameplateRect = nameplateGraphic.rectTransform;
            nameplateRect.anchoredPosition = new Vector2(textLeft - 16f, -12f);
        }

        if (bodyText != null)
        {
            RectTransform bodyRect = bodyText.rectTransform;
            bodyRect.offsetMin = new Vector2(textLeft, 23f);
            bodyRect.offsetMax = new Vector2(-82f, -55f);
        }
    }

    private static void StretchToFill(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        var uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }
}
