using System.Collections.Generic;
using UnityEngine;

public abstract class DialogueFlowAdapterBase : MonoBehaviour
{
    private const string SpeakerCatalogResourcePath = "Dialogue/DialogueSpeakerCatalog";

    [SerializeField] protected DialogueFlowController flowController;
    [SerializeField] protected bool createFlowControllerIfMissing = true;
    [SerializeField] protected bool registerDefaultFlows = true;
    [SerializeField] protected float promptAutoAdvanceDelay = 4f;
    [SerializeField] protected DialogueSpeakerCatalog speakerCatalog;
    [SerializeField] protected DialogueVoiceClipCatalog voiceClipCatalog;

    protected void EnsureFlowController()
    {
        if (flowController != null)
            return;

        flowController = FindAnyObjectByType<DialogueFlowController>(FindObjectsInactive.Include);

        if (flowController == null && createFlowControllerIfMissing)
            flowController = gameObject.AddComponent<DialogueFlowController>();
    }

    protected bool TryPlayFlow(string key)
    {
        EnsureFlowController();
        return flowController != null && flowController.TryPlay(key);
    }

    protected bool TryPlayFlowNow(string key)
    {
        EnsureFlowController();
        return flowController != null && flowController.TryPlayNow(key);
    }

    protected bool ReplayFlowNow(string key)
    {
        EnsureFlowController();
        if (flowController == null || string.IsNullOrWhiteSpace(key))
            return false;

        flowController.ResetPlayedKey(key);
        return flowController.TryPlayNow(key);
    }

    protected DialogueSpeaker ResolveSpeaker(string displayName)
    {
        if (speakerCatalog == null)
            speakerCatalog = Resources.Load<DialogueSpeakerCatalog>(SpeakerCatalogResourcePath);

        return speakerCatalog != null ? speakerCatalog.FindByName(displayName) : null;
    }

    protected void RegisterLine(
        string key,
        string lineId,
        DialogueSpeaker speaker,
        string text,
        IEnumerable<string> tags = null,
        bool playOnce = true,
        bool queueIfRunnerBusy = true,
        string speakerName = null)
    {
        EnsureFlowController();

        if (flowController == null)
            return;

        flowController.RegisterLines(
            key,
            new[]
            {
                new DialogueFlowLineDefinition(
                    lineId,
                    text,
                    speaker,
                    speakerName,
                    tags,
                    requiresContinue: false,
                    autoAdvanceDelay: promptAutoAdvanceDelay,
                    voiceClip: ResolveVoiceClip(lineId))
            },
            playOnce,
            queueIfRunnerBusy,
            replaceExisting: false);
    }

    protected AudioClip ResolveVoiceClip(string lineId)
    {
        if (voiceClipCatalog == null)
            return DialogueVoiceClipCatalog.ResolveDefault(lineId);

        return voiceClipCatalog.Resolve(lineId);
    }
}
