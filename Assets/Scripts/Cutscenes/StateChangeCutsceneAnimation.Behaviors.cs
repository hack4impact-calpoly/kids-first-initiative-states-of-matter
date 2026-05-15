using System.Collections.Generic;
using UnityEngine;

public partial class StateChangeCutsceneAnimation
{
    private StateChangeCutsceneContext BehaviorContext
    {
        get
        {
            if (behaviorContext == null)
                behaviorContext = new StateChangeCutsceneContext(this);

            return behaviorContext;
        }
    }

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
                return new ChocolateMeltingCutsceneBehavior(BehaviorContext);
            case MatterCutsceneKind.LiquidFlow:
                return new LiquidFlowCutsceneBehavior(BehaviorContext);
            case MatterCutsceneKind.LiquidFreezing:
                return new LiquidFreezingCutsceneBehavior(BehaviorContext);
            case MatterCutsceneKind.PipeWaterFlow:
                return new PipeFlowCutsceneBehavior(BehaviorContext, false);
            case MatterCutsceneKind.PipeFreezing:
                return new PipeFlowCutsceneBehavior(BehaviorContext, true);
            case MatterCutsceneKind.CircuitCandleMelting:
                return new CircuitCandleMeltingCutsceneBehavior(BehaviorContext);
            case MatterCutsceneKind.CircuitPlasmaIonizing:
                return new CircuitPlasmaIonizingCutsceneBehavior(BehaviorContext);
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
        protected StateChangeCutsceneBehavior(StateChangeCutsceneContext context)
        {
            Context = context;
        }

        protected StateChangeCutsceneContext Context { get; }
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

    private sealed class StateChangeCutsceneContext
    {
        private readonly StateChangeCutsceneAnimation animation;

        public StateChangeCutsceneContext(StateChangeCutsceneAnimation animation)
        {
            this.animation = animation;
        }

        public float ParticleSize => animation.particleSize;
        public Vector2 ParticleAreaSize => animation.particleAreaSize;
        public float CandleFinalHoldDuration => animation.candleFinalHoldDuration;

        public Color GetWaterParticleColor(int index)
        {
            return animation.GetWaterParticleColor(index);
        }

        public Color GetJuiceParticleColor(int index)
        {
            return animation.GetJuiceParticleColor(index);
        }

        public Color GetWaxParticleColor(int index)
        {
            return animation.GetWaxParticleColor(index);
        }

        public Color GetMeltedWaxParticleColor(int index)
        {
            return animation.GetMeltedWaxParticleColor(index);
        }

        public Color GetGasParticleColor(int index)
        {
            return animation.GetGasParticleColor(index);
        }

        public Color GetPlasmaParticleColor(int index, float elapsed)
        {
            return animation.GetPlasmaParticleColor(index, elapsed);
        }

        public Vector2 BounceInside(Vector2 position, ref Vector2 velocity)
        {
            return animation.BounceInside(position, ref velocity);
        }

        public Vector2 BounceInsideContainer(Vector2 position, ref Vector2 velocity)
        {
            return animation.BounceInsideContainer(position, ref velocity);
        }

        public Vector2 BounceInsideWaxLiquid(Vector2 position, ref Vector2 velocity, float margin)
        {
            return animation.BounceInsideWaxLiquid(position, ref velocity, margin);
        }

        public Vector2 BounceInsidePlasmaTube(Vector2 position, ref Vector2 velocity, float margin)
        {
            return animation.BounceInsidePlasmaTube(position, ref velocity, margin);
        }

        public Vector2 ClampInsideContainer(Vector2 position, float margin)
        {
            return animation.ClampInsideContainer(position, margin);
        }

        public Vector2 ClampInsidePlasmaTube(Vector2 position, float margin)
        {
            return animation.ClampInsidePlasmaTube(position, margin);
        }

        public float GetContainerHalfWidth(float y)
        {
            return animation.GetContainerHalfWidth(y);
        }

        public Vector2 PositionOnEllipse(float t, float halfWidth, float halfHeight)
        {
            return animation.PositionOnEllipse(t, halfWidth, halfHeight);
        }

        public void UpdateBonds(IReadOnlyList<BondView> bonds, float alpha)
        {
            animation.UpdateBonds(bonds, alpha);
        }

        public void SetFlowLineAlpha(IReadOnlyList<RectTransform> lines, float alpha)
        {
            animation.SetFlowLineAlpha(lines, alpha);
        }

        public void SetIceCubeAlpha(IceCubeView iceCube, float amount)
        {
            animation.SetIceCubeAlpha(iceCube, amount);
        }

        public void SetContainerAlpha(ContainerView container, float amount)
        {
            animation.SetContainerAlpha(container, amount);
        }
    }
}
