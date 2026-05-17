using UnityEngine;
using UnityEngine.Events;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private DialogueRunner runner;
    [SerializeField] private DialogueSequence sequence;
    [SerializeField] private DialogueConditionSet conditions = new DialogueConditionSet();
    [SerializeField] private bool queueIfRunnerBusy = true;
    [SerializeField] private bool playOnStart;
    [SerializeField] private bool playOnce = true;
    [SerializeField] private bool playOnPlayerTrigger = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private UnityEvent triggered;

    private bool hasPlayed;
    private bool isPending;

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ShouldPlayFromTrigger(other.gameObject))
            Play();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (ShouldPlayFromTrigger(other.gameObject))
            Play();
    }

    public void Play()
    {
        if (playOnce && (hasPlayed || isPending))
            return;

        if (!conditions.IsMet())
            return;

        DialogueRunner targetRunner = ResolveRunner();
        if (targetRunner == null)
        {
            Debug.LogWarning("DialogueTrigger could not find a DialogueRunner.", this);
            return;
        }

        if (playOnce)
            isPending = true;

        System.Action onStarted = playOnce ? MarkStarted : null;
        System.Action onCanceled = playOnce ? MarkCanceled : null;
        bool accepted = queueIfRunnerBusy
            ? targetRunner.Queue(sequence, onStarted, onCanceled: onCanceled)
            : targetRunner.PlayNow(sequence, onStarted, onCanceled: onCanceled);

        if (!accepted && playOnce)
            isPending = false;

        if (accepted)
        {
            triggered?.Invoke();
        }
    }

    public void ResetTrigger()
    {
        hasPlayed = false;
        isPending = false;
    }

    private void MarkStarted()
    {
        isPending = false;
        hasPlayed = true;
    }

    private void MarkCanceled()
    {
        isPending = false;
    }

    private bool ShouldPlayFromTrigger(GameObject other)
    {
        if (!playOnPlayerTrigger || other == null)
            return false;

        return string.IsNullOrEmpty(playerTag) || other.CompareTag(playerTag);
    }

    private DialogueRunner ResolveRunner()
    {
        if (runner != null)
            return runner;

        runner = FindAnyObjectByType<DialogueRunner>();
        return runner;
    }
}
