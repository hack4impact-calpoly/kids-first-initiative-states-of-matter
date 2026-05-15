using System.Collections;
using UnityEngine;

public enum MatterCutsceneKind
{
    ChocolateMelting = 0,
    LiquidFlow = 1,
    LiquidFreezing = 2,
    PipeWaterFlow = 3,
    PipeFreezing = 4,
    CircuitCandleMelting = 6,
    CircuitPlasmaIonizing = 7
}

public partial class StateChangeCutsceneAnimation : MonoBehaviour, ICutsceneAnimation, ICutsceneAnimationCleanup
{
    [SerializeField] private MatterCutsceneKind cutsceneKind = MatterCutsceneKind.LiquidFreezing;
    [SerializeField] private int particleCount = 20;
    [SerializeField] private float particleSize = 34f;
    [SerializeField] private Vector2 particleAreaSize = new Vector2(880f, 430f);
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float firstStageDuration = 1.7f;
    [SerializeField] private float secondStageDuration = 2.2f;
    [SerializeField] private float finalStageDuration = 1.2f;
    [SerializeField] private float candleFinalHoldDuration = 1f;

    private const int RandomSeed = 2749;
    private static Sprite circleSprite;
    private CutsceneView activeView;
    private IStateChangeCutsceneBehavior currentBehavior;
    private StateChangeCutsceneContext behaviorContext;
    private MatterCutsceneKind currentBehaviorKind;
    private bool hasResolvedCurrentBehavior;

    public void Configure(MatterCutsceneKind kind)
    {
        cutsceneKind = kind;
        hasResolvedCurrentBehavior = false;
    }

    public IEnumerator Play(CutsceneContext context)
    {
        if (context == null || context.OverlayRoot == null)
            yield break;

        Cleanup(context);

        if (!TryResolveCurrentBehavior(out _))
        {
            Debug.LogError($"Unsupported state change cutscene kind '{cutsceneKind}'. State change cutscene will not play.", this);
            yield break;
        }

        CutsceneView view = BuildView(context.OverlayRoot);
        activeView = view;

        try
        {
            ApplyText(view, 0);
            TickStage(view, 0, 0f, 0f);

            yield return Fade(view.Group, 0f, 1f, fadeDuration, context);
            yield return AnimateStage(view, firstStageDuration, 0, context);

            ApplyText(view, 1);
            yield return AnimateStage(view, secondStageDuration, 1, context);

            ApplyText(view, 2);
            yield return AnimateStage(view, finalStageDuration, 2, context);

            float holdDuration = GetFinalStageHoldDuration();
            if (holdDuration > 0f)
                yield return HoldStage(view, holdDuration, 2, context);

            yield return Fade(view.Group, 1f, 0f, fadeDuration, context);
        }
        finally
        {
            DestroyView(view);
        }
    }

    public void Cleanup(CutsceneContext context)
    {
        if (activeView != null)
            DestroyView(activeView);
    }

    private void DestroyView(CutsceneView view)
    {
        if (view == null)
            return;

        if (view.Root != null)
            Destroy(view.Root.gameObject);

        if (activeView == view)
            activeView = null;
    }

    private IEnumerator AnimateStage(CutsceneView view, float duration, int stage, CutsceneContext context)
    {
        CaptureStageStartPositions(view, stage);
        yield return AnimateFor(duration, context, (progress, elapsed, deltaTime) =>
        {
            view.ElapsedTime += deltaTime;
            TickStage(view, stage, progress, deltaTime);
        });
    }

    private IEnumerator HoldStage(CutsceneView view, float duration, int stage, CutsceneContext context)
    {
        yield return AnimateFor(duration, context, (progress, elapsed, deltaTime) =>
        {
            view.ElapsedTime += deltaTime;
            TickStage(view, stage, 1f, deltaTime);
        });
    }

    private float GetFinalStageHoldDuration()
    {
        return CurrentBehavior.FinalStageHoldDuration;
    }

    private void TickStage(CutsceneView view, int stage, float progress, float deltaTime)
    {
        CurrentBehavior.Tick(view, stage, progress, view.ElapsedTime, deltaTime);
    }

    private void CaptureStageStartPositions(CutsceneView view, int stage)
    {
        bool captureRenderedPosition = CurrentBehavior.ShouldCaptureRenderedPosition(stage);

        for (int i = 0; i < view.Particles.Count; i++)
        {
            ParticleView particle = view.Particles[i];

            if (captureRenderedPosition)
                particle.Position = particle.Rect.anchoredPosition;

            particle.StageStartPosition = particle.Position;
        }
    }

    private void ApplyText(CutsceneView view, int stage)
    {
        IStateChangeCutsceneBehavior behavior = CurrentBehavior;
        view.Title.text = behavior.Title;
        view.StageLabel.text = behavior.GetStageLabel(stage);
    }

    private IEnumerator Fade(CanvasGroup group, float from, float to, float duration, CutsceneContext context)
    {
        yield return AnimateFor(duration, context, (progress, elapsed, deltaTime) =>
        {
            group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, progress));
        });

        group.alpha = to;
    }

    private IEnumerator AnimateFor(float duration, CutsceneContext context, System.Action<float, float, float> tick)
    {
        if (duration <= 0f)
        {
            tick(1f, 0f, 0f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float deltaTime = context.DeltaTime;
            elapsed += deltaTime;
            tick(Mathf.Clamp01(elapsed / duration), elapsed, deltaTime);
            yield return null;
        }

        tick(1f, duration, 0f);
    }
}
