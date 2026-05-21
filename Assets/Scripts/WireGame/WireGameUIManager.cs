using UnityEngine;
using TMPro;

public class WireGameUIManager : MonoBehaviour
{
    public static WireGameUIManager Instance { get; private set; }

    private const string AttachOutputPrompt = "Please attach an output before connecting the wires";
    
    public TextMeshProUGUI promptText;
    [SerializeField] private bool positionPromptAtTop = true;
    [SerializeField] private float topPromptInset = 48f;
    [SerializeField] private float topPromptSideMargin = 120f;
    [SerializeField] private float topPromptHeight = 150f;
    [SerializeField] private float topPromptMaxFontSize = 56f;
    [SerializeField] private float topPromptMinFontSize = 28f;

    private float messageDisplayTime = 3f;
    private float messageTimer = 0f;
    private string persistentMessage = "";
    private bool persistentIsWarning = false;
    private Color warningColor = Color.red;
    private Color normalColor = Color.white;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ConfigurePromptLayout();
        ResetPrompt();
    }

    void Update()
    {
        if (messageTimer > 0)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0)
            {
                SetPromptText(persistentMessage, persistentIsWarning);
            }
        }
    }

    public void ShowMessage(string message, bool isWarning = false)
    {
        SetPromptText(message, isWarning);
        messageTimer = messageDisplayTime;
    }

    public void SetPersistentPrompt(string message, bool isWarning = false)
    {
        persistentMessage = message;
        persistentIsWarning = isWarning;
        messageTimer = 0f;
        SetPromptText(persistentMessage, persistentIsWarning);
    }

    public void ResetPrompt()
    {
        SetPersistentPrompt(AttachOutputPrompt, false);
    }

    public void ClearPrompt()
    {
        SetPersistentPrompt("", false);
    }

    private void ConfigurePromptLayout()
    {
        if (!positionPromptAtTop || promptText == null)
            return;

        RectTransform promptRect = promptText.rectTransform;
        promptRect.anchorMin = new Vector2(0f, 1f);
        promptRect.anchorMax = new Vector2(1f, 1f);
        promptRect.pivot = new Vector2(0.5f, 1f);
        promptRect.anchoredPosition = new Vector2(0f, -topPromptInset);
        promptRect.sizeDelta = new Vector2(-topPromptSideMargin * 2f, topPromptHeight);

        promptText.alignment = TextAlignmentOptions.Center;
        promptText.enableAutoSizing = true;
        promptText.fontSizeMax = topPromptMaxFontSize;
        promptText.fontSizeMin = topPromptMinFontSize;
        promptText.textWrappingMode = TextWrappingModes.Normal;
    }

    private void SetPromptText(string text, bool isWarning)
    {
        if (promptText != null)
        {
            promptText.text = text;
            promptText.color = isWarning ? warningColor : normalColor;
        }
    }
}
