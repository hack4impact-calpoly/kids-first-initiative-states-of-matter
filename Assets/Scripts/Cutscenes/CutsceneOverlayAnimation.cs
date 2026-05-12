using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class CutsceneOverlayAnimation : MonoBehaviour, ICutsceneAnimation
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Animator animator;
    [SerializeField] private string playTrigger = "Play";
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float holdDuration = 2f;
    [SerializeField] private float fadeOutDuration = 0.25f;

    private void OnValidate()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    public IEnumerator Play(CutsceneContext context)
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (animator != null && !string.IsNullOrEmpty(playTrigger))
            animator.SetTrigger(playTrigger);

        yield return Fade(0f, 1f, fadeInDuration, context);
        yield return Wait(holdDuration, context);
        yield return Fade(1f, 0f, fadeOutDuration, context);
    }

    private IEnumerator Fade(float from, float to, float duration, CutsceneContext context)
    {
        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += context.DeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private IEnumerator Wait(float duration, CutsceneContext context)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += context.DeltaTime;
            yield return null;
        }
    }
}
