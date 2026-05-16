using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Speaker Catalog")]
public class DialogueSpeakerCatalog : ScriptableObject
{
    [SerializeField] private List<DialogueSpeaker> speakers = new List<DialogueSpeaker>();

    public IReadOnlyList<DialogueSpeaker> Speakers => speakers;

    public DialogueSpeaker FindByName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return null;

        for (int i = 0; i < speakers.Count; i++)
        {
            DialogueSpeaker speaker = speakers[i];
            if (speaker != null && string.Equals(speaker.DisplayName, displayName, System.StringComparison.OrdinalIgnoreCase))
                return speaker;
        }

        return null;
    }
}
