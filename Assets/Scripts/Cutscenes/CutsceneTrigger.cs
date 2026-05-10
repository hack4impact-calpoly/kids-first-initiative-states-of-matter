using UnityEngine;

public class CutsceneTrigger : MonoBehaviour
{
    [SerializeField] private CutsceneManager cutsceneManager;
    [SerializeField] private CutsceneDefinition cutsceneDefinition;
    [SerializeField] private Transform focusTarget;
    [SerializeField] private MonoBehaviour cutsceneAnimation;
    [SerializeField] private bool playOnce = true;
    [SerializeField] private bool triggerOnEnter2D = true;
    [SerializeField] private string requiredTag;

    private bool hasPlayed;

    public void Play()
    {
        if (playOnce && hasPlayed)
            return;

        CutsceneManager manager = ResolveCutsceneManager();
        if (manager == null)
            return;

        if (cutsceneDefinition == null && cutsceneAnimation == null)
        {
            Debug.LogWarning($"{name} cannot play a cutscene because it has no definition or animation.");
            return;
        }

        Transform target = focusTarget != null ? focusTarget : transform;
        bool played = cutsceneDefinition != null
            ? manager.TryPlay(cutsceneDefinition, target, cutsceneAnimation)
            : manager.TryPlay(target, cutsceneAnimation);

        if (played)
            hasPlayed = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggerOnEnter2D || !IsAllowedTrigger(other))
            return;

        Play();
    }

    private bool IsAllowedTrigger(Collider2D other)
    {
        return string.IsNullOrEmpty(requiredTag) || other.CompareTag(requiredTag);
    }

    private CutsceneManager ResolveCutsceneManager()
    {
        if (cutsceneManager != null)
            return cutsceneManager;

        cutsceneManager = FindAnyObjectByType<CutsceneManager>();

        if (cutsceneManager == null)
            cutsceneManager = gameObject.AddComponent<CutsceneManager>();

        return cutsceneManager;
    }
}
