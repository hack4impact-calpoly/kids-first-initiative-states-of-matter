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
        if (playOnce && hasPlayed)
            return;

        if (!conditions.IsMet())
            return;

        DialogueRunner targetRunner = ResolveRunner();
        if (targetRunner == null)
        {
            Debug.LogWarning("DialogueTrigger could not find a DialogueRunner.", this);
            return;
        }

        bool accepted = queueIfRunnerBusy ? targetRunner.Queue(sequence) : targetRunner.PlayNow(sequence);
        if (accepted)
        {
            hasPlayed = true;
            triggered?.Invoke();
        }
    }

    public void ResetTrigger()
    {
        hasPlayed = false;
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
