using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Voice Clip Catalog")]
public class DialogueVoiceClipCatalog : ScriptableObject
{
    private const string DefaultResourcePath = "Dialogue/DialogueVoiceClipCatalog";

    [SerializeField] private List<DialogueVoiceClipEntry> clips = new List<DialogueVoiceClipEntry>();

    private static DialogueVoiceClipCatalog defaultCatalog;
    private Dictionary<string, AudioClip> clipLookup;

    public static AudioClip ResolveDefault(string lineId)
    {
        if (defaultCatalog == null)
            defaultCatalog = Resources.Load<DialogueVoiceClipCatalog>(DefaultResourcePath);

        return defaultCatalog != null ? defaultCatalog.Resolve(lineId) : null;
    }

    public AudioClip Resolve(string lineId)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            return null;

        BuildLookupIfNeeded();
        return clipLookup.TryGetValue(lineId, out AudioClip clip) ? clip : null;
    }

    private void OnValidate()
    {
        clipLookup = null;
    }

    private void BuildLookupIfNeeded()
    {
        if (clipLookup != null)
            return;

        clipLookup = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
        if (clips == null)
            return;

        for (int i = 0; i < clips.Count; i++)
        {
            DialogueVoiceClipEntry entry = clips[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.LineId) || entry.Clip == null)
                continue;

            clipLookup[entry.LineId] = entry.Clip;
        }
    }
}

[Serializable]
public class DialogueVoiceClipEntry
{
    [SerializeField] private string lineId;
    [SerializeField] private AudioClip clip;

    public string LineId => lineId;
    public AudioClip Clip => clip;
}
