using UnityEngine;
using TMPro;

public class WireGameUIManager : MonoBehaviour
{
    public static WireGameUIManager Instance { get; private set; }
    
    public TextMeshProUGUI promptText;
    private float messageDisplayTime = 3f;
    private float messageTimer = 0f;
    private string persistentMessage = "";
    private Color warningColor = Color.red;
    private Color normalColor = Color.white;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        persistentMessage = "Please attach an output before connecting the wires";
        SetPromptText(persistentMessage, false);
    }

    void Update()
    {
        if (messageTimer > 0)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0)
            {
                SetPromptText(persistentMessage, false);
            }
        }
    }

    public void ShowMessage(string message, bool isWarning = false)
    {
        SetPromptText(message, isWarning);
        messageTimer = messageDisplayTime;
    }

    public void ClearPrompt()
    {
        persistentMessage = "";
        SetPromptText("", false);
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