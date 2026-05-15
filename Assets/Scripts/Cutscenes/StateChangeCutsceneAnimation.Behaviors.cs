using UnityEngine;

public partial class StateChangeCutsceneAnimation
{
    private IStateChangeCutsceneBehavior CurrentBehavior
    {
        get
        {
            if (!hasResolvedCurrentBehavior || currentBehaviorKind != cutsceneKind)
            {
                currentBehaviorKind = cutsceneKind;
                currentBehavior = CreateBehavior(cutsceneKind);
                hasResolvedCurrentBehavior = true;
            }

            return currentBehavior;
        }
    }

    private IStateChangeCutsceneBehavior CreateBehavior(MatterCutsceneKind kind)
    {
        switch (kind)
        {
            case MatterCutsceneKind.ChocolateMelting:
                return new ChocolateMeltingCutsceneBehavior(this);
            case MatterCutsceneKind.LiquidFlow:
                return new LiquidFlowCutsceneBehavior(this);
            case MatterCutsceneKind.LiquidFreezing:
                return new LiquidFreezingCutsceneBehavior(this);
            case MatterCutsceneKind.PipeWaterFlow:
                return new PipeFlowCutsceneBehavior(this, false);
            case MatterCutsceneKind.PipeFreezing:
                return new PipeFlowCutsceneBehavior(this, true);
            case MatterCutsceneKind.CircuitCandleMelting:
                return new CircuitCandleMeltingCutsceneBehavior(this);
            case MatterCutsceneKind.CircuitPlasmaIonizing:
                return new CircuitPlasmaIonizingCutsceneBehavior(this);
            default:
                return null;
        }
    }

    private interface IStateChangeCutsceneBehavior
    {
        float FinalStageHoldDuration { get; }
        bool UsesPlasmaTubeContainer { get; }
        string Title { get; }
        string GetStageLabel(int stage);
        bool ShouldCaptureRenderedPosition(int stage);
        Color GetParticleColor(int index);
        void Tick(CutsceneView view, int stage, float progress, float elapsed, float deltaTime);
    }

    private abstract class StateChangeCutsceneBehavior : IStateChangeCutsceneBehavior
    {
        protected StateChangeCutsceneBehavior(StateChangeCutsceneAnimation animation)
        {
            Animation = animation;
        }

        protected StateChangeCutsceneAnimation Animation { get; }
        public virtual float FinalStageHoldDuration => 0f;
        public virtual bool UsesPlasmaTubeContainer => false;
        public abstract string Title { get; }
        public abstract string GetStageLabel(int stage);

        public virtual bool ShouldCaptureRenderedPosition(int stage)
        {
            return false;
        }

        public abstract Color GetParticleColor(int index);
        public abstract void Tick(CutsceneView view, int stage, float progress, float elapsed, float deltaTime);
    }
}
