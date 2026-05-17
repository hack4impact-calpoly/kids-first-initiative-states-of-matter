using UnityEngine;

public class WireGameUIManager : MonoBehaviour
{
    public static WireGameUIManager Instance { get; private set; }

    private const string AttachOutputPrompt = "Please attach an output before connecting the wires";

    [SerializeField] private DialoguePromptPresenter dialoguePromptPresenter;

    private float messageDisplayTime = 3f;
    private float messageTimer = 0f;
    private string persistentMessage = "";
    private bool persistentIsWarning = false;

    void Awake()
    {
        Instance = this;
        ResolveDialoguePromptPresenter();
    }

    void Start()
    {
        ResetPrompt();
    }

    void Update()
    {
        if (messageTimer > 0)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0)
            {
                ShowPrompt(persistentMessage, persistentIsWarning);
            }
        }
    }

    public void ShowMessage(string message, bool isWarning = false)
    {
        ShowPrompt(message, isWarning);
        messageTimer = messageDisplayTime;
    }

    public void SetPersistentPrompt(string message, bool isWarning = false)
    {
        persistentMessage = message;
        persistentIsWarning = isWarning;
        messageTimer = 0f;
        ShowPrompt(persistentMessage, persistentIsWarning);
    }

    public void ResetPrompt()
    {
        SetPersistentPrompt(AttachOutputPrompt, false);
    }

    public void ClearPrompt()
    {
        SetPersistentPrompt("", false);
    }

    private void ShowPrompt(string text, bool isWarning)
    {
        if (dialoguePromptPresenter == null)
        {
            Debug.LogError("WireGameUIManager requires a DialoguePromptPresenter and has no legacy prompt fallback.", this);
            return;
        }

        DialogueRunner activeRunner = FindAnyObjectByType<DialogueRunner>(FindObjectsInactive.Include);
        if (activeRunner != null && activeRunner.IsPlaying)
            return;

        dialoguePromptPresenter.ShowPrompt(text, isWarning);
    }

    private void ResolveDialoguePromptPresenter()
    {
        if (dialoguePromptPresenter == null)
            dialoguePromptPresenter = FindAnyObjectByType<DialoguePromptPresenter>(FindObjectsInactive.Include);

        if (dialoguePromptPresenter == null)
            dialoguePromptPresenter = CreateDialoguePromptPresenter();

        EnsureDialogueRunnerAndInput();

        if (dialoguePromptPresenter == null)
            Debug.LogError("WireGameUIManager could not create a DialoguePromptPresenter.", this);

        if (WireGameGuidanceController.Instance != null)
            WireGameGuidanceController.Instance.RefreshDialogueRunner();
    }

    private DialoguePromptPresenter CreateDialoguePromptPresenter()
    {
        Transform parent = ResolvePresenterParent();

        var presenterObject = new GameObject("Dialogue System");
        presenterObject.transform.SetParent(parent, false);
        return presenterObject.AddComponent<DialoguePromptPresenter>();
    }

    private Transform ResolvePresenterParent()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        return canvas != null ? canvas.transform : transform;
    }

    private void EnsureDialogueRunnerAndInput()
    {
        if (dialoguePromptPresenter == null)
        {
            Debug.LogError("WireGameUIManager cannot create dialogue infrastructure without a DialoguePromptPresenter.", this);
            return;
        }

        DialogueRunner runner = FindAnyObjectByType<DialogueRunner>(FindObjectsInactive.Include);
        if (runner == null)
            runner = dialoguePromptPresenter.gameObject.AddComponent<DialogueRunner>();

        if (runner == null)
        {
            Debug.LogError("WireGameUIManager could not create a DialogueRunner for wire game prompts.", this);
            return;
        }

        if (FindAnyObjectByType<DialogueAdvanceInput>(FindObjectsInactive.Include) == null)
            runner.gameObject.AddComponent<DialogueAdvanceInput>();
    }
}
