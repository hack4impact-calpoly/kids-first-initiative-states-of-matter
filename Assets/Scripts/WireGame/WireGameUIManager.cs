using UnityEngine;
using TMPro;

public class WireGameUIManager : MonoBehaviour
{
    public static WireGameUIManager Instance { get; private set; }
    
    public TextMeshProUGUI promptText;
    private float messageDisplayTime = 3f;
    private float messageTimer = 0f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    void Start()
    {
        if (promptText != null)
            promptText.text = "";
    }

    void Update()
    {
        if (messageTimer > 0)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0)
            {
                promptText.text = "";
            }
        }
    }

    public void ShowMessage(string message)
    {
        if (promptText != null)
        {
            promptText.text = message;
            messageTimer = messageDisplayTime;
        }
    }
}