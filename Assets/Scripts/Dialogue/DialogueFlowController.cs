using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueFlowLineDefinition
{
    [SerializeField] private string lineId;
    [SerializeField] private string[] tags;
    [SerializeField] private DialogueSpeaker speaker;
    [SerializeField] private string speakerName;
    [SerializeField, TextArea(2, 6)] private string text;
    [SerializeField] private Sprite portrait;
    [SerializeField] private AudioClip voiceClip;
    [SerializeField] private bool waitForVoiceClipBeforeAdvance = true;
    [SerializeField] private bool requiresContinue = true;
    [SerializeField] private float autoAdvanceDelay = -1f;

    public DialogueFlowLineDefinition()
    {
    }

    public DialogueFlowLineDefinition(
        string lineId,
        string text,
        DialogueSpeaker speaker = null,
        string speakerName = null,
        IEnumerable<string> tags = null,
        bool requiresContinue = true,
        float autoAdvanceDelay = -1f,
        AudioClip voiceClip = null,
        Sprite portrait = null,
        bool waitForVoiceClipBeforeAdvance = true)
    {
        this.lineId = lineId;
        this.text = text;
        this.speaker = speaker;
        this.speakerName = speakerName;
        this.tags = CopyTags(tags);
        this.requiresContinue = requiresContinue;
        this.autoAdvanceDelay = autoAdvanceDelay;
        this.voiceClip = voiceClip;
        this.portrait = portrait;
        this.waitForVoiceClipBeforeAdvance = waitForVoiceClipBeforeAdvance;
    }

    public bool IsEmpty => string.IsNullOrWhiteSpace(text);

    public DialogueLine ToDialogueLine()
    {
        return new DialogueLine(
            lineId,
            text,
            speaker,
            speakerName,
            portrait,
            voiceClip,
            tags,
            waitForVoiceClipBeforeAdvance,
            requiresContinue,
            autoAdvanceDelay);
    }

    private static string[] CopyTags(IEnumerable<string> sourceTags)
    {
        if (sourceTags == null)
            return Array.Empty<string>();

        List<string> copiedTags = new List<string>();
        foreach (string sourceTag in sourceTags)
        {
            if (!string.IsNullOrWhiteSpace(sourceTag))
                copiedTags.Add(sourceTag);
        }

        return copiedTags.ToArray();
    }
}

[Serializable]
public class DialogueFlowEntry
{
    [SerializeField] private string key;
    [SerializeField] private DialogueSequence sequence;
    [SerializeField] private List<DialogueFlowLineDefinition> inlineLines = new List<DialogueFlowLineDefinition>();
    [SerializeField] private bool playOnce = true;
    [SerializeField] private bool queueIfRunnerBusy = true;
    [SerializeField] private bool pauseTimeWhilePlaying;
    [SerializeField] private bool hideViewWhenFinished = true;

    private DialogueSequence runtimeSequence;

    public DialogueFlowEntry()
    {
    }

    public DialogueFlowEntry(
        string key,
        DialogueSequence sequence,
        bool playOnce = true,
        bool queueIfRunnerBusy = true)
    {
        this.key = key;
        this.sequence = sequence;
        this.playOnce = playOnce;
        this.queueIfRunnerBusy = queueIfRunnerBusy;
    }

    public DialogueFlowEntry(
        string key,
        IEnumerable<DialogueFlowLineDefinition> inlineLines,
        bool playOnce = true,
        bool queueIfRunnerBusy = true,
        bool pauseTimeWhilePlaying = false,
        bool hideViewWhenFinished = true)
    {
        this.key = key;
        this.inlineLines = inlineLines != null
            ? new List<DialogueFlowLineDefinition>(inlineLines)
            : new List<DialogueFlowLineDefinition>();
        this.playOnce = playOnce;
        this.queueIfRunnerBusy = queueIfRunnerBusy;
        this.pauseTimeWhilePlaying = pauseTimeWhilePlaying;
        this.hideViewWhenFinished = hideViewWhenFinished;
    }

    public string Key => key;
    public bool PlayOnce => playOnce;
    public bool QueueIfRunnerBusy => queueIfRunnerBusy;

    public DialogueSequence ResolveSequence()
    {
        if (sequence != null)
            return sequence;

        if (runtimeSequence != null)
            return runtimeSequence;

        if (inlineLines == null || inlineLines.Count == 0)
            return null;

        List<DialogueLine> lines = new List<DialogueLine>();
        for (int i = 0; i < inlineLines.Count; i++)
        {
            DialogueFlowLineDefinition line = inlineLines[i];
            if (line != null && !line.IsEmpty)
                lines.Add(line.ToDialogueLine());
        }

        runtimeSequence = DialogueSequence.CreateRuntime(lines, pauseTimeWhilePlaying, hideViewWhenFinished);
        return runtimeSequence;
    }
}

public class DialogueFlowController : MonoBehaviour
{
    [SerializeField] private DialogueRunner runner;
    [SerializeField] private bool createRunnerIfMissing = true;
    [SerializeField] private bool addAdvanceInputIfMissing = true;
    [SerializeField] private List<DialogueFlowEntry> flows = new List<DialogueFlowEntry>();

    private readonly Dictionary<string, DialogueFlowEntry> flowLookup = new Dictionary<string, DialogueFlowEntry>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DialogueFlowEntry> runtimeFlows = new Dictionary<string, DialogueFlowEntry>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> playedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> pendingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private bool lookupDirty = true;

    private void Awake()
    {
        ResolveRunner();
        RebuildLookup();
    }

    private void OnValidate()
    {
        lookupDirty = true;
    }

    public bool TryPlay(string key, Action onFinished = null)
    {
        if (!TryResolveFlow(key, out DialogueFlowEntry flow))
            return false;

        if (flow.PlayOnce && (playedKeys.Contains(flow.Key) || pendingKeys.Contains(flow.Key)))
            return false;

        DialogueSequence sequence = flow.ResolveSequence();
        if (sequence == null || sequence.Count == 0)
            return false;

        DialogueRunner targetRunner = ResolveRunner();
        if (targetRunner == null)
            return false;

        Action onStarted = flow.PlayOnce ? () => MarkFlowStarted(flow.Key) : null;
        Action onCanceled = flow.PlayOnce ? () => pendingKeys.Remove(flow.Key) : null;
        if (flow.PlayOnce)
            pendingKeys.Add(flow.Key);

        bool accepted = flow.QueueIfRunnerBusy
            ? targetRunner.Queue(sequence, onStarted, onFinished, onCanceled)
            : targetRunner.PlayNow(sequence, onStarted, onFinished, onCanceled);

        if (!accepted && flow.PlayOnce)
            pendingKeys.Remove(flow.Key);

        return accepted;
    }

    public bool TryPlayNow(string key, Action onFinished = null)
    {
        if (!TryResolveFlow(key, out DialogueFlowEntry flow))
            return false;

        if (flow.PlayOnce && (playedKeys.Contains(flow.Key) || pendingKeys.Contains(flow.Key)))
            return false;

        DialogueSequence sequence = flow.ResolveSequence();
        DialogueRunner targetRunner = ResolveRunner();
        if (sequence == null || sequence.Count == 0 || targetRunner == null)
            return false;

        Action onStarted = flow.PlayOnce ? () => MarkFlowStarted(flow.Key) : null;
        Action onCanceled = flow.PlayOnce ? () => pendingKeys.Remove(flow.Key) : null;
        if (flow.PlayOnce)
            pendingKeys.Add(flow.Key);

        bool accepted = targetRunner.PlayNow(sequence, onStarted, onFinished, onCanceled);

        if (!accepted && flow.PlayOnce)
            pendingKeys.Remove(flow.Key);

        return accepted;
    }

    public bool HasFlow(string key)
    {
        return TryResolveFlow(key, out _);
    }

    public void RegisterFlow(DialogueFlowEntry flow, bool replaceExisting = true)
    {
        if (flow == null || string.IsNullOrWhiteSpace(flow.Key))
            return;

        RebuildLookupIfNeeded();

        if (!replaceExisting && flowLookup.ContainsKey(flow.Key))
            return;

        runtimeFlows[flow.Key] = flow;
        flowLookup[flow.Key] = flow;
    }

    public void RegisterSequence(
        string key,
        DialogueSequence sequence,
        bool playOnce = true,
        bool queueIfRunnerBusy = true,
        bool replaceExisting = true)
    {
        if (sequence == null)
            return;

        RegisterFlow(new DialogueFlowEntry(key, sequence, playOnce, queueIfRunnerBusy), replaceExisting);
    }

    public void RegisterLines(
        string key,
        IEnumerable<DialogueFlowLineDefinition> lines,
        bool playOnce = true,
        bool queueIfRunnerBusy = true,
        bool replaceExisting = true,
        bool pauseTimeWhilePlaying = false,
        bool hideViewWhenFinished = true)
    {
        RegisterFlow(
            new DialogueFlowEntry(key, lines, playOnce, queueIfRunnerBusy, pauseTimeWhilePlaying, hideViewWhenFinished),
            replaceExisting);
    }

    public void ResetPlayedKey(string key)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            playedKeys.Remove(key);
            pendingKeys.Remove(key);
        }
    }

    public void ResetAllPlayedKeys()
    {
        playedKeys.Clear();
        pendingKeys.Clear();
    }

    private void MarkFlowStarted(string key)
    {
        pendingKeys.Remove(key);
        playedKeys.Add(key);
    }

    private bool TryResolveFlow(string key, out DialogueFlowEntry flow)
    {
        flow = null;

        if (string.IsNullOrWhiteSpace(key))
            return false;

        RebuildLookupIfNeeded();
        return flowLookup.TryGetValue(key, out flow);
    }

    private void RebuildLookupIfNeeded()
    {
        if (lookupDirty)
            RebuildLookup();
    }

    private void RebuildLookup()
    {
        flowLookup.Clear();

        if (flows != null)
        {
            for (int i = 0; i < flows.Count; i++)
            {
                DialogueFlowEntry flow = flows[i];
                if (flow != null && !string.IsNullOrWhiteSpace(flow.Key))
                    flowLookup[flow.Key] = flow;
            }
        }

        foreach (KeyValuePair<string, DialogueFlowEntry> runtimeFlow in runtimeFlows)
            flowLookup[runtimeFlow.Key] = runtimeFlow.Value;

        lookupDirty = false;
    }

    private DialogueRunner ResolveRunner()
    {
        if (runner == null)
            runner = FindAnyObjectByType<DialogueRunner>(FindObjectsInactive.Include);

        if (runner == null && createRunnerIfMissing)
            runner = gameObject.AddComponent<DialogueRunner>();

        if (runner != null && addAdvanceInputIfMissing && FindAnyObjectByType<DialogueAdvanceInput>(FindObjectsInactive.Include) == null)
            runner.gameObject.AddComponent<DialogueAdvanceInput>();

        return runner;
    }
}
