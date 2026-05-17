using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PowerDialController : MonoBehaviour
{
    [SerializeField] private Slider powerSlider;
    [Range(0f, 1f)]
    [SerializeField] private float powerThreshold = 0.95f;
    [SerializeField] private bool snapToOnOnRelease = true;
    [Range(0f, 1f)]
    [SerializeField] private float releaseSnapThreshold = 0.75f;
    [SerializeField] private bool createRuntimeUiIfMissing = true;
    [SerializeField] private Vector2 runtimeAnchoredPosition = new Vector2(-520f, 260f);
    [SerializeField] private bool placeRuntimeUiNearWireBoard = true;
    [SerializeField] private Vector2 wireBoardWorldOffset = new Vector2(6.4f, 2.9f);
    [SerializeField] private float canvasEdgeMargin = 24f;

    private Image fillImage;
    private Image handleImage;
    private TextMeshProUGUI statusLabel;
    private AttentionHighlight guidanceHighlight;
    private Func<bool> canPowerOn;
    private Action powerOnBlocked;
    private bool wasPoweredOn;
    private bool isListening;

    public bool IsPoweredOn => powerSlider != null && GetNormalizedSliderValue() >= powerThreshold;
    public AttentionHighlight GuidanceHighlight => ResolveGuidanceHighlight();
    public event Action<bool> PowerStateChanged;

    private void Awake()
    {
        ResolveSlider();

        if (powerSlider == null && createRuntimeUiIfMissing)
            CreateRuntimeDial();

        EnsureReleaseHandler();
        StartListening();
        SyncFromSlider();
    }

    private void OnValidate()
    {
        if (powerSlider == null)
            powerSlider = GetComponentInChildren<Slider>();
    }

    private void OnDestroy()
    {
        StopListening();
    }

    public void OnPowerChanged(float value)
    {
        SyncFromSlider();
    }

    public void ConfigurePowerOnGate(Func<bool> canPowerOn, Action onPowerOnBlocked)
    {
        this.canPowerOn = canPowerOn;
        powerOnBlocked = onPowerOnBlocked;
        SyncFromSlider();
    }

    public void ForceOff()
    {
        ResetToOff(notifyPowerStateChanged: true);
    }

    public void HandleDialReleased()
    {
        if (!snapToOnOnRelease || powerSlider == null)
            return;

        if (GetNormalizedSliderValue() < releaseSnapThreshold)
            return;

        if (!CanPowerOn())
        {
            RejectPowerOnAttempt();
            return;
        }

        powerSlider.value = powerSlider.maxValue;
    }

    private void ResolveSlider()
    {
        if (powerSlider == null)
            powerSlider = GetComponentInChildren<Slider>();
    }

    private float GetNormalizedSliderValue()
    {
        if (powerSlider == null)
            return 0f;

        return Mathf.InverseLerp(powerSlider.minValue, powerSlider.maxValue, powerSlider.value);
    }

    private void StartListening()
    {
        if (powerSlider == null || isListening)
            return;

        powerSlider.onValueChanged.AddListener(OnPowerChanged);
        isListening = true;
    }

    private void StopListening()
    {
        if (powerSlider == null || !isListening)
            return;

        powerSlider.onValueChanged.RemoveListener(OnPowerChanged);
        isListening = false;
    }

    private void EnsureReleaseHandler()
    {
        if (powerSlider == null)
            return;

        PowerDialReleaseHandler releaseHandler = powerSlider.GetComponent<PowerDialReleaseHandler>();
        if (releaseHandler == null)
            releaseHandler = powerSlider.gameObject.AddComponent<PowerDialReleaseHandler>();

        releaseHandler.Initialize(this);
    }

    private void SyncFromSlider()
    {
        bool isPoweredOn = IsPoweredOn;
        if (isPoweredOn && !CanPowerOn())
        {
            RejectPowerOnAttempt();
            return;
        }

        UpdateVisuals(isPoweredOn);

        if (isPoweredOn == wasPoweredOn)
            return;

        wasPoweredOn = isPoweredOn;
        PowerStateChanged?.Invoke(isPoweredOn);
    }

    private bool CanPowerOn()
    {
        return canPowerOn == null || canPowerOn();
    }

    private void RejectPowerOnAttempt()
    {
        powerOnBlocked?.Invoke();
        ResetToOff(notifyPowerStateChanged: wasPoweredOn);
    }

    private void ResetToOff(bool notifyPowerStateChanged)
    {
        if (powerSlider == null)
            return;

        powerSlider.SetValueWithoutNotify(powerSlider.minValue);
        UpdateVisuals(false);

        if (!wasPoweredOn)
            return;

        wasPoweredOn = false;

        if (notifyPowerStateChanged)
            PowerStateChanged?.Invoke(false);
    }

    private void UpdateVisuals(bool isPoweredOn)
    {
        Color offColor = new Color(0.88f, 0.16f, 0.12f, 1f);
        Color onColor = new Color(0.2f, 0.86f, 0.28f, 1f);
        Color color = isPoweredOn ? onColor : offColor;

        if (fillImage != null)
            fillImage.color = color;

        if (handleImage != null)
            handleImage.color = color;

        if (statusLabel != null)
        {
            statusLabel.text = isPoweredOn ? "ON" : "OFF";
            statusLabel.color = color;
        }
    }

    private void CreateRuntimeDial()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            canvas = CreateRuntimeCanvas();

        Vector2 dialSize = new Vector2(150f, 320f);
        RectTransform root = CreateRect("Power Dial", canvas.transform, dialSize, Vector2.zero);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = ResolveRuntimeDialPosition(canvas, dialSize);
        guidanceHighlight = root.gameObject.AddComponent<AttentionHighlight>();

        Image panel = root.gameObject.AddComponent<Image>();
        panel.color = new Color(0.04f, 0.055f, 0.065f, 0.86f);
        panel.raycastTarget = false;

        TextMeshProUGUI title = CreateLabel("Power Label", root, new Vector2(130f, 34f), new Vector2(0f, 128f), 22f);
        title.text = "POWER";
        title.color = new Color(0.92f, 0.98f, 1f, 1f);

        statusLabel = CreateLabel("Power Status", root, new Vector2(130f, 34f), new Vector2(0f, -132f), 22f);

        RectTransform sliderRoot = CreateRect("Power Slider", root, new Vector2(76f, 220f), new Vector2(0f, -2f));
        powerSlider = sliderRoot.gameObject.AddComponent<Slider>();
        powerSlider.minValue = 0f;
        powerSlider.maxValue = 1f;
        powerSlider.wholeNumbers = false;
        powerSlider.value = 0f;
        powerSlider.direction = Slider.Direction.BottomToTop;

        RectTransform background = CreateImage("Background", sliderRoot, new Vector2(26f, 190f), Vector2.zero, new Color(0.16f, 0.2f, 0.23f, 1f));
        background.anchorMin = new Vector2(0.5f, 0.5f);
        background.anchorMax = new Vector2(0.5f, 0.5f);

        RectTransform fillArea = CreateRect("Fill Area", sliderRoot, new Vector2(26f, 190f), Vector2.zero);
        fillArea.anchorMin = new Vector2(0.5f, 0.5f);
        fillArea.anchorMax = new Vector2(0.5f, 0.5f);

        RectTransform fill = CreateImage("Fill", fillArea, new Vector2(26f, 190f), Vector2.zero, new Color(0.88f, 0.16f, 0.12f, 1f));
        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = new Vector2(1f, 0f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        fillImage = fill.GetComponent<Image>();

        RectTransform handleArea = CreateRect("Handle Slide Area", sliderRoot, new Vector2(72f, 190f), Vector2.zero);
        handleArea.anchorMin = new Vector2(0.5f, 0.5f);
        handleArea.anchorMax = new Vector2(0.5f, 0.5f);

        RectTransform handle = CreateImage("Handle", handleArea, new Vector2(58f, 22f), Vector2.zero, new Color(0.88f, 0.16f, 0.12f, 1f));
        handleImage = handle.GetComponent<Image>();

        powerSlider.fillRect = fill;
        powerSlider.handleRect = handle;
        powerSlider.targetGraphic = handleImage;
    }

    private AttentionHighlight ResolveGuidanceHighlight()
    {
        if (guidanceHighlight != null)
            return guidanceHighlight;

        if (powerSlider != null)
        {
            Transform target = powerSlider.transform.parent != null ? powerSlider.transform.parent : powerSlider.transform;
            guidanceHighlight = target.GetComponent<AttentionHighlight>();

            if (guidanceHighlight == null)
                guidanceHighlight = target.gameObject.AddComponent<AttentionHighlight>();
        }

        return guidanceHighlight;
    }

    private Vector2 ResolveRuntimeDialPosition(Canvas canvas, Vector2 dialSize)
    {
        if (!placeRuntimeUiNearWireBoard || !TryGetWireBoardPosition(out Vector3 boardPosition))
            return runtimeAnchoredPosition;

        Camera sceneCamera = Camera.main;
        if (sceneCamera == null)
            return runtimeAnchoredPosition;

        Vector3 worldPosition = boardPosition + new Vector3(wireBoardWorldOffset.x, wireBoardWorldOffset.y, 0f);
        Vector2 screenPoint = sceneCamera.WorldToScreenPoint(worldPosition);
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, eventCamera, out Vector2 canvasPosition))
            return ClampToCanvas(canvasPosition, canvasRect, dialSize);

        return runtimeAnchoredPosition;
    }

    private bool TryGetWireBoardPosition(out Vector3 boardPosition)
    {
        GameObject left = GameObject.Find("Left");
        GameObject right = GameObject.Find("Right");

        if (left != null && right != null)
        {
            boardPosition = (left.transform.position + right.transform.position) * 0.5f;
            return true;
        }

        if (right != null)
        {
            boardPosition = right.transform.position;
            return true;
        }

        if (left != null)
        {
            boardPosition = left.transform.position;
            return true;
        }

        GameObject connectionWire = GameObject.Find("ConnectionWire");
        if (connectionWire != null)
        {
            boardPosition = connectionWire.transform.position + new Vector3(-4f, 0f, 0f);
            return true;
        }

        boardPosition = Vector3.zero;
        return false;
    }

    private Vector2 ClampToCanvas(Vector2 position, RectTransform canvasRect, Vector2 dialSize)
    {
        Vector2 canvasSize = canvasRect.rect.size;
        if (canvasSize.x <= 0f || canvasSize.y <= 0f)
            canvasSize = new Vector2(Screen.width, Screen.height);

        Vector2 halfCanvas = canvasSize * 0.5f;
        Vector2 halfDial = dialSize * 0.5f;
        float minX = -halfCanvas.x + halfDial.x + canvasEdgeMargin;
        float maxX = halfCanvas.x - halfDial.x - canvasEdgeMargin;
        float minY = -halfCanvas.y + halfDial.y + canvasEdgeMargin;
        float maxY = halfCanvas.y - halfDial.y - canvasEdgeMargin;

        if (minX <= maxX)
            position.x = Mathf.Clamp(position.x, minX, maxX);

        if (minY <= maxY)
            position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }

    private Canvas CreateRuntimeCanvas()
    {
        var canvasObject = new GameObject("Power Dial Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private RectTransform CreateImage(string objectName, Transform parent, Vector2 size, Vector2 position, Color color)
    {
        RectTransform rect = CreateRect(objectName, parent, size, position);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return rect;
    }

    private TextMeshProUGUI CreateLabel(string objectName, Transform parent, Vector2 size, Vector2 position, float fontSize)
    {
        RectTransform rect = CreateRect(objectName, parent, size, position);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = fontSize;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    private RectTransform CreateRect(string objectName, Transform parent, Vector2 size, Vector2 position)
    {
        var rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);

        RectTransform rect = rectObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private sealed class PowerDialReleaseHandler : MonoBehaviour, IPointerUpHandler, IEndDragHandler
    {
        private PowerDialController owner;

        public void Initialize(PowerDialController controller)
        {
            owner = controller;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            owner?.HandleDialReleased();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            owner?.HandleDialReleased();
        }
    }
}
