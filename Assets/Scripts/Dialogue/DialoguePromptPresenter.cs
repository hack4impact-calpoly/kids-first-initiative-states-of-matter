using UnityEngine;

public class DialoguePromptPresenter : MonoBehaviour
{
    private const string DefaultCatalogResourcePath = "Dialogue/DialogueSpeakerCatalog";

    [SerializeField] private DialogueView view;
    [SerializeField] private bool createViewIfMissing = true;
    [SerializeField] private DialogueBubblePlacement placement = DialogueBubblePlacement.Bottom;
    [SerializeField] private bool forceBottomPlacement = true;
    [SerializeField] private DialogueSpeakerCatalog fallbackSpeakerCatalog;
    [SerializeField] private DialogueSpeaker speaker;
    [SerializeField] private DialogueSpeaker warningSpeaker;
    [SerializeField] private string speakerName = "Patrice";
    [SerializeField] private string warningSpeakerName = "Patrice";
    [SerializeField] private Sprite portrait;
    [SerializeField] private Sprite warningPortrait;

    private void Awake()
    {
        ResolveView();
    }

    public void ShowPrompt(string message, bool isWarning = false)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Hide();
            return;
        }

        DialogueView targetView = ResolveView();
        if (targetView == null)
        {
            Debug.LogError("DialoguePromptPresenter could not resolve a DialogueView for prompts.", this);
            return;
        }

        targetView.SetPlacement(ResolvedPlacement);
        targetView.ShowPrompt(
            ResolveSpeakerName(isWarning),
            message,
            ResolvePortrait(isWarning),
            isWarning,
            false);
    }

    public void Hide()
    {
        DialogueView targetView = ResolveView();
        if (targetView != null)
            targetView.Hide();
    }

    private DialogueView ResolveView()
    {
        if (view != null)
            return view;

        view = GetComponentInChildren<DialogueView>(true);

        if (view == null)
            view = FindAnyObjectByType<DialogueView>(FindObjectsInactive.Include);

        if (view == null && createViewIfMissing)
        {
            var viewObject = new GameObject("Dialogue View");
            viewObject.transform.SetParent(transform, false);
            view = viewObject.AddComponent<DialogueView>();
        }

        if (view != null)
            view.SetPlacement(ResolvedPlacement);

        return view;
    }

    private DialogueBubblePlacement ResolvedPlacement => forceBottomPlacement ? DialogueBubblePlacement.Bottom : placement;

    private string ResolveSpeakerName(bool isWarning)
    {
        DialogueSpeaker resolvedSpeaker = ResolveSpeaker(isWarning);
        if (resolvedSpeaker != null)
            return resolvedSpeaker.DisplayName;

        return isWarning ? warningSpeakerName : speakerName;
    }

    private Sprite ResolvePortrait(bool isWarning)
    {
        DialogueSpeaker resolvedSpeaker = ResolveSpeaker(isWarning);
        if (resolvedSpeaker != null && resolvedSpeaker.Portrait != null)
            return resolvedSpeaker.Portrait;

        return isWarning && warningPortrait != null ? warningPortrait : portrait;
    }

    private DialogueSpeaker ResolveSpeaker(bool isWarning)
    {
        if (isWarning && warningSpeaker != null)
            return warningSpeaker;

        if (speaker != null)
            return speaker;

        DialogueSpeakerCatalog catalog = ResolveFallbackSpeakerCatalog();
        return catalog != null ? catalog.FindByName(isWarning ? warningSpeakerName : speakerName) : null;
    }

    private DialogueSpeakerCatalog ResolveFallbackSpeakerCatalog()
    {
        if (fallbackSpeakerCatalog == null)
            fallbackSpeakerCatalog = Resources.Load<DialogueSpeakerCatalog>(DefaultCatalogResourcePath);

        return fallbackSpeakerCatalog;
    }
}
