using UnityEngine;
using UnityEngine.Events;

public class CutsceneDefinitionPlayer : MonoBehaviour
{
    [SerializeField] private CutsceneManager cutsceneManager;
    [SerializeField] private CutsceneDefinition cutsceneDefinition;
    [SerializeField] private Transform focusTarget;
    [SerializeField] private MonoBehaviour animationOverride;
    [SerializeField] private bool playOnStart;
    [SerializeField] private bool playOnce = true;
    [SerializeField] private UnityEvent played;
    [SerializeField] private UnityEvent finished;

    private bool hasPlayed;

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    public void Play()
    {
        if (playOnce && hasPlayed)
            return;

        CutsceneManager manager = ResolveCutsceneManager();
        if (manager == null || cutsceneDefinition == null)
            return;

        Transform target = focusTarget != null ? focusTarget : transform;
        if (manager.TryPlay(cutsceneDefinition, target, animationOverride, OnFinished))
        {
            hasPlayed = true;
            played?.Invoke();
        }
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

    private void OnFinished()
    {
        finished?.Invoke();
    }
}
