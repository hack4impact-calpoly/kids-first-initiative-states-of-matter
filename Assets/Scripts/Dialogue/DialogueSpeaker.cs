using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Speaker")]
public class DialogueSpeaker : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private Sprite portrait;
    [SerializeField] private AudioClip defaultVoiceClip;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public Sprite Portrait => portrait;
    public AudioClip DefaultVoiceClip => defaultVoiceClip;
}
