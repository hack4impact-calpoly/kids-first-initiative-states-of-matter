using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueLine
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

    public DialogueLine()
    {
    }

    public DialogueLine(
        string lineId,
        string text,
        DialogueSpeaker speaker = null,
        string speakerName = null,
        Sprite portrait = null,
        AudioClip voiceClip = null,
        IEnumerable<string> tags = null,
        bool waitForVoiceClipBeforeAdvance = true,
        bool requiresContinue = true,
        float autoAdvanceDelay = -1f)
    {
        this.lineId = lineId;
        this.text = text;
        this.speaker = speaker;
        this.speakerName = speakerName;
        this.portrait = portrait;
        this.voiceClip = voiceClip;
        this.tags = CopyTags(tags);
        this.waitForVoiceClipBeforeAdvance = waitForVoiceClipBeforeAdvance;
        this.requiresContinue = requiresContinue;
        this.autoAdvanceDelay = autoAdvanceDelay;
    }

    public string LineId => lineId;
    public string[] Tags => tags;
    public DialogueSpeaker Speaker => speaker;
    public string SpeakerName => !string.IsNullOrWhiteSpace(speakerName) ? speakerName : speaker != null ? speaker.DisplayName : string.Empty;
    public string Text => text;
    public Sprite Portrait => portrait != null ? portrait : speaker != null ? speaker.Portrait : null;
    public AudioClip VoiceClip => voiceClip != null ? voiceClip : speaker != null ? speaker.DefaultVoiceClip : null;
    public bool WaitForVoiceClipBeforeAdvance => waitForVoiceClipBeforeAdvance;
    public bool RequiresContinue => requiresContinue;
    public float AutoAdvanceDelay => autoAdvanceDelay >= 0f ? autoAdvanceDelay : 0f;
    public bool ShouldAutoAdvance => autoAdvanceDelay >= 0f || !requiresContinue;

    public bool HasTag(string tag)
    {
        if (string.IsNullOrEmpty(tag) || tags == null)
            return false;

        for (int i = 0; i < tags.Length; i++)
        {
            if (string.Equals(tags[i], tag, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
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
