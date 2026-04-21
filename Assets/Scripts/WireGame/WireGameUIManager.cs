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
        if (Instance == null)
            Instance = this;
    }

    void Start()
    {
        if (promptText != null)
        {
            persistentMessage = "Please attach an output before connecting the wires";
            promptText.text = persistentMessage;
            promptText.color = normalColor;
        }
    }

    void Update()
    {
        if (messageTimer > 0)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0 && promptText != null)
            {
                promptText.text = persistentMessage;
                promptText.color = normalColor;
            }
        }
    }

    public void ShowMessage(string message, bool isWarning = false)
    {
        if (promptText != null)
        {
            promptText.text = message;
            promptText.color = isWarning ? warningColor : normalColor;
            messageTimer = messageDisplayTime;
        }
    }

    public void ClearPrompt()
    {
        persistentMessage = "";
        if (promptText != null)
        {
            promptText.text = "";
        }
    }
}