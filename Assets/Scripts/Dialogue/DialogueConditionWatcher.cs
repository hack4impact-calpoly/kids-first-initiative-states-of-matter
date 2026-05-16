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
        if (playOnce && hasQueued)
            return;

        if (!conditions.IsMet())
            return;

        DialogueRunner targetRunner = ResolveRunner();
        if (targetRunner == null)
        {
            Debug.LogWarning("DialogueConditionWatcher could not find a DialogueRunner.", this);
            return;
        }

        bool accepted = queueIfRunnerBusy ? targetRunner.Queue(sequence) : targetRunner.PlayNow(sequence);
        if (!accepted)
            return;

        hasQueued = true;
        queued?.Invoke();
    }

    public void ResetWatcher()
    {
        hasQueued = false;
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
