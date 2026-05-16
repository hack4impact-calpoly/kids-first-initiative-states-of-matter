using System;
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
}
