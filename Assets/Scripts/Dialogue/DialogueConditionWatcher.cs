using UnityEngine;
using UnityEngine.Events;

public class DialogueConditionWatcher : MonoBehaviour
{
    [SerializeField] private DialogueRunner runner;
    [SerializeField] private DialogueSequence sequence;
    [SerializeField] private DialogueConditionSet conditions = new DialogueConditionSet();
    [SerializeField] private bool evaluateOnStart = true;
    [SerializeField] private bool evaluateWhenConditionsChange = true;
    [SerializeField] private bool playOnce = true;
    [SerializeField] private bool queueIfRunnerBusy = true;
    [SerializeField] private UnityEvent queued;

    private bool hasQueued;
    private bool isPending;

    private void OnEnable()
    {
        if (!evaluateWhenConditionsChange)
            return;

        DialogueConditionState.Changed += OnConditionChanged;
        DialogueConditionState.Cleared += OnConditionCleared;
    }

    private void Start()
    {
        if (evaluateOnStart)
            EvaluateAndQueue();
    }

    private void OnDisable()
    {
        DialogueConditionState.Changed -= OnConditionChanged;
        DialogueConditionState.Cleared -= OnConditionCleared;
    }

    public void EvaluateAndQueue()
    {
        if (playOnce && (hasQueued || isPending))
            return;

        if (!conditions.IsMet())
            return;

        DialogueRunner targetRunner = ResolveRunner();
        if (targetRunner == null)
        {
            Debug.LogWarning("DialogueConditionWatcher could not find a DialogueRunner.", this);
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

        if (!accepted)
            return;

        queued?.Invoke();
    }

    public void ResetWatcher()
    {
        hasQueued = false;
        isPending = false;
    }

    private void MarkStarted()
    {
        isPending = false;
        hasQueued = true;
    }

    private void MarkCanceled()
    {
        isPending = false;
    }

    private void OnConditionChanged(string key, DialogueConditionValue value)
    {
        EvaluateAndQueue();
    }

    private void OnConditionCleared(string key)
    {
        EvaluateAndQueue();
    }

    private DialogueRunner ResolveRunner()
    {
        if (runner != null)
            return runner;

        runner = FindAnyObjectByType<DialogueRunner>();
        return runner;
    }
}
