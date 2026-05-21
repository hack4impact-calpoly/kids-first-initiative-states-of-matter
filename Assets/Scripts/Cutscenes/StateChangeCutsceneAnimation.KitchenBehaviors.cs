using UnityEngine;

public partial class StateChangeCutsceneAnimation
{
    private sealed class ChocolateMeltingCutsceneBehavior : StateChangeCutsceneBehavior
    {
        public ChocolateMeltingCutsceneBehavior(StateChangeCutsceneContext context) : base(context)
        {
        }

        public override string Title => "Chocolate Melting";

        public override string GetStageLabel(int stage)
        {
            return stage == 0 ? "Solid chocolate particles vibrate in place." :
                stage == 1 ? "Heating adds energy, so bonds loosen as chocolate melts." :
                "Melted chocolate particles slide past one another.";
        }

        public override Color GetParticleColor(int index)
        {
            return Color.Lerp(new Color(0.34f, 0.14f, 0.045f, 1f), new Color(0.86f, 0.42f, 0.14f, 1f), (index % 5) * 0.16f);
        }

        public override void Tick(CutsceneView view, int stage, float progress, float elapsed, float deltaTime)
        {
            float meltAmount = stage == 0 ? 0f : stage == 1 ? Mathf.SmoothStep(0f, 1f, progress) : 1f;
            float motion = Mathf.Lerp(0.08f, 1f, meltAmount);

            for (int i = 0; i < view.Particles.Count; i++)
            {
                ParticleView particle = view.Particles[i];

                if (stage == 2)
                {
                    particle.Position += particle.Velocity * deltaTime;
                    particle.Position = Context.BounceInside(particle.Position, ref particle.Velocity);
                }
                else
                {
                    particle.Position = Vector2.Lerp(particle.SolidPosition, particle.LiquidPosition, meltAmount);
                }

                Vector2 vibration = new Vector2(
                    Mathf.Sin(elapsed * Mathf.Lerp(15f, 28f, motion) + particle.Phase),
                    Mathf.Cos(elapsed * 13f + particle.Phase)) * Mathf.Lerp(5f, 22f, motion);
                particle.Rect.anchoredPosition = particle.Position + vibration;
            }

            Context.UpdateBonds(view.Bonds, 1f - meltAmount);
            Context.SetFlowLineAlpha(view.FlowLines, Mathf.Lerp(0.08f, 0.35f, meltAmount));
        }
    }

    private sealed class LiquidFlowCutsceneBehavior : StateChangeCutsceneBehavior
    {
        public LiquidFlowCutsceneBehavior(StateChangeCutsceneContext context) : base(context)
        {
        }

        public override string Title => "Liquid Particles";

        public override string GetStageLabel(int stage)
        {
            return stage == 0 ? "Particles are close together." :
                stage == 1 ? "They slide and flow around each other." :
                "Liquids take the shape of their container.";
        }

        public override Color GetParticleColor(int index)
        {
            return Context.GetJuiceParticleColor(index);
        }

        public override void Tick(CutsceneView view, int stage, float progress, float elapsed, float deltaTime)
        {
            for (int i = 0; i < view.Particles.Count; i++)
            {
                ParticleView particle = view.Particles[i];
                particle.Position += particle.Velocity * deltaTime;
                particle.Position = Context.BounceInsideContainer(particle.Position, ref particle.Velocity);
                particle.Rect.anchoredPosition = particle.Position + Vector2.up * Mathf.Sin(elapsed * 3.2f + particle.Phase) * 10f;
            }

            Context.UpdateBonds(view.Bonds, 0f);
            Context.SetFlowLineAlpha(view.FlowLines, 0f);
            Context.SetContainerAlpha(view.Container, stage == 0 ? progress : 1f);
        }
    }

    private sealed class LiquidFreezingCutsceneBehavior : StateChangeCutsceneBehavior
    {
        public LiquidFreezingCutsceneBehavior(StateChangeCutsceneContext context) : base(context)
        {
        }

        public override string Title => "Liquid to Solid";

        public override string GetStageLabel(int stage)
        {
            return stage == 0 ? "Liquid particles slide past each other." :
                stage == 1 ? "Cooling removes energy, so particles slow down." :
                "Frozen particles lock into fixed positions and only vibrate.";
        }

        public override Color GetParticleColor(int index)
        {
            return Context.GetJuiceParticleColor(index);
        }

        public override void Tick(CutsceneView view, int stage, float progress, float elapsed, float deltaTime)
        {
            float lockAmount = stage == 0 ? 0f : stage == 1 ? Mathf.SmoothStep(0f, 1f, progress) : 1f;
            float motion = Mathf.Lerp(1f, 0.08f, lockAmount);

            for (int i = 0; i < view.Particles.Count; i++)
            {
                ParticleView particle = view.Particles[i];
                if (stage == 0)
                {
                    particle.Position += particle.Velocity * deltaTime;
                    particle.Position = Context.BounceInside(particle.Position, ref particle.Velocity);
                }
                else
                {
                    particle.Position = Vector2.Lerp(particle.StageStartPosition, particle.SolidPosition, lockAmount);
                }

                Vector2 vibration = new Vector2(Mathf.Sin(elapsed * 15f + particle.Phase), Mathf.Cos(elapsed * 13f + particle.Phase)) * (5f * motion);
                particle.Rect.anchoredPosition = particle.Position + vibration;
            }

            Context.UpdateBonds(view.Bonds, lockAmount);
            Context.SetFlowLineAlpha(view.FlowLines, 0f);
            Context.SetIceCubeAlpha(view.IceCube, lockAmount);
        }
    }
}
