using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Dialogue Sequence")]
public class DialogueSequence : ScriptableObject
{
    [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();
    [SerializeField] private bool pauseTimeWhilePlaying = true;
    [SerializeField] private bool hideViewWhenFinished = true;
    [SerializeField] private bool waitForAudioBeforeNextQueuedSequence = true;
    [SerializeField] private bool stopLineAudioWhenSkipped = true;

    public IReadOnlyList<DialogueLine> Lines => lines;
    public int Count => lines != null ? lines.Count : 0;
    public bool PauseTimeWhilePlaying => pauseTimeWhilePlaying;
    public bool HideViewWhenFinished => hideViewWhenFinished;
    public bool WaitForAudioBeforeNextQueuedSequence => waitForAudioBeforeNextQueuedSequence;
    public bool StopLineAudioWhenSkipped => stopLineAudioWhenSkipped;

    public static DialogueSequence CreateRuntime(
        IEnumerable<DialogueLine> runtimeLines,
        bool pauseTimeWhilePlaying = false,
        bool hideViewWhenFinished = true,
        bool waitForAudioBeforeNextQueuedSequence = true,
        bool stopLineAudioWhenSkipped = true)
    {
        DialogueSequence sequence = CreateInstance<DialogueSequence>();
        sequence.lines = runtimeLines != null ? new List<DialogueLine>(runtimeLines) : new List<DialogueLine>();
        sequence.pauseTimeWhilePlaying = pauseTimeWhilePlaying;
        sequence.hideViewWhenFinished = hideViewWhenFinished;
        sequence.waitForAudioBeforeNextQueuedSequence = waitForAudioBeforeNextQueuedSequence;
        sequence.stopLineAudioWhenSkipped = stopLineAudioWhenSkipped;
        return sequence;
    }

    public DialogueLine GetLine(int index)
    {
        return lines[index];
    }
}
