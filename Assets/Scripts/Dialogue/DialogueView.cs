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
    [SerializeField] private DialogueRoundedRectGraphic bubbleGraphic;
    [SerializeField] private DialogueRoundedRectGraphic bubbleFillGraphic;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject continuePrompt;

    [Header("Default Bubble Layout")]
    [SerializeField] private bool buildDefaultLayoutIfMissing = true;
    [SerializeField] private DialogueBubblePlacement placement = DialogueBubblePlacement.Bottom;
    [SerializeField] private bool stretchToParentWidth = true;
    [SerializeField] private Vector2 bubbleSize = new Vector2(980f, 116f);
    [SerializeField] private Vector2 bubbleOffset = new Vector2(0f, 12f);
    [SerializeField] private float horizontalMargin = 24f;
    [SerializeField] private float borderThickness = 2f;
    [SerializeField] private float portraitSize = 88f;
    [SerializeField] private Color bubbleColor = new Color(0.98f, 0.97f, 1f, 0.98f);
    [SerializeField] private Color borderColor = new Color(0.27f, 0.25f, 0.58f, 1f);
    [SerializeField] private Color speakerNameColor = new Color(0.25f, 0.22f, 0.58f, 1f);
    [SerializeField] private Color bodyTextColor = new Color(0.08f, 0.07f, 0.1f, 1f);
    [SerializeField] private Color warningBorderColor = new Color(0.82f, 0.32f, 0.08f, 1f);
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
            speakerNameText.text = speakerName;

        if (bodyText != null)
            bodyText.text = text;

        if (portraitImage != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
        }

        ApplyContentLayout(portrait != null);
        SetContinueEnabled(canContinue);
    }

    public void SetContinueEnabled(bool canContinue)
    {
        if (continueButton != null)
            continueButton.interactable = canContinue;

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

        bubbleObject.AddComponent<CanvasRenderer>();
        bubbleGraphic = bubbleObject.AddComponent<DialogueRoundedRectGraphic>();
        bubbleGraphic.CornerRadius = 14f;
        bubbleGraphic.color = borderColor;

        GameObject fillObject = CreateUiObject("Bubble Fill", bubbleRect);
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
            bubbleGraphic = root.GetComponent<DialogueRoundedRectGraphic>();

        if (bubbleFillGraphic == null)
            bubbleFillGraphic = root.transform.Find("Bubble Fill")?.GetComponent<DialogueRoundedRectGraphic>();
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

        if (bubbleGraphic != null)
            bubbleGraphic.color = resolvedBorderColor;

        if (bubbleFillGraphic != null)
            bubbleFillGraphic.color = bubbleColor;

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
        GameObject portraitObject = CreateUiObject("Portrait", bubbleRect);
        RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0f, 0.5f);
        portraitRect.anchorMax = new Vector2(0f, 0.5f);
        portraitRect.pivot = new Vector2(0.5f, 0.5f);
        portraitRect.anchoredPosition = new Vector2(58f, -1f);
        portraitRect.sizeDelta = new Vector2(portraitSize, portraitSize);

        portraitImage = portraitObject.AddComponent<Image>();
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;
    }

    private void BuildSpeakerName(RectTransform bubbleRect)
    {
        GameObject textObject = CreateUiObject("Speaker Name", bubbleRect);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.offsetMin = new Vector2(120f, -37f);
        textRect.offsetMax = new Vector2(-40f, -11f);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = 22f;
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
        textRect.offsetMin = new Vector2(120f, 14f);
        textRect.offsetMax = new Vector2(-44f, -42f);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = 24f;
        text.fontStyle = FontStyles.Italic;
        text.color = bodyTextColor;
        text.alignment = TextAlignmentOptions.Left;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        bodyText = text;
    }

    private void BuildContinuePrompt(RectTransform bubbleRect)
    {
        GameObject promptObject = CreateUiObject("Continue Prompt", bubbleRect);
        RectTransform promptRect = promptObject.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(1f, 0.5f);
        promptRect.anchorMax = new Vector2(1f, 0.5f);
        promptRect.pivot = new Vector2(1f, 0.5f);
        promptRect.anchoredPosition = new Vector2(-16f, -4f);
        promptRect.sizeDelta = new Vector2(24f, 32f);

        TextMeshProUGUI promptText = promptObject.AddComponent<TextMeshProUGUI>();
        promptText.text = ">";
        promptText.fontSize = 28f;
        promptText.fontStyle = FontStyles.Bold;
        promptText.color = borderColor;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.raycastTarget = false;
        continuePrompt = promptObject;
    }

    private void ApplyContentLayout(bool hasPortrait)
    {
        float textLeft = hasPortrait ? 120f : 28f;

        if (speakerNameText != null)
        {
            RectTransform speakerRect = speakerNameText.rectTransform;
            speakerRect.offsetMin = new Vector2(textLeft, -37f);
            speakerRect.offsetMax = new Vector2(-40f, -11f);
        }

        if (bodyText != null)
        {
            RectTransform bodyRect = bodyText.rectTransform;
            bodyRect.offsetMin = new Vector2(textLeft, 14f);
            bodyRect.offsetMax = new Vector2(-44f, -42f);
        }
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        var uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }
}
