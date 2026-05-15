using UnityEngine;

public partial class StateChangeCutsceneAnimation
{
    private sealed class CircuitCandleMeltingCutsceneBehavior : StateChangeCutsceneBehavior
    {
        public CircuitCandleMeltingCutsceneBehavior(StateChangeCutsceneAnimation animation) : base(animation)
        {
        }

        public override float FinalStageHoldDuration => Animation.candleFinalHoldDuration;
        public override string Title => "Candle Melting";

        public override string GetStageLabel(int stage)
        {
            return stage == 0 ? "Wax particles start packed together as a solid." :
                stage == 1 ? "Electrical energy becomes heat and loosens the wax." :
                "Melted wax particles slide past each other as a liquid.";
        }

        public override bool ShouldCaptureRenderedPosition(int stage)
        {
            return stage == 2;
        }

        public override Color GetParticleColor(int index)
        {
            return Animation.GetWaxParticleColor(index);
        }

        public override void Tick(CutsceneView view, int stage, float progress, float elapsed, float deltaTime)
        {
            float meltAmount = stage == 0 ? 0f : stage == 1 ? Mathf.SmoothStep(0f, 1f, progress) : 1f;
            float heatAmount = stage == 0 ? Mathf.SmoothStep(0f, 0.35f, progress) : 1f;
            float motion = Mathf.Lerp(0.08f, 1.05f, meltAmount);

            for (int i = 0; i < view.Particles.Count; i++)
            {
                ParticleView particle = view.Particles[i];

                if (stage == 2)
                {
                    Vector2 drift = GetWaxLiquidDrift(elapsed, particle.Phase, i) * deltaTime;
                    particle.Position += particle.Velocity * deltaTime * 0.34f + drift;
                    particle.Position = Animation.BounceInsideWaxLiquid(particle.Position, ref particle.Velocity, Animation.particleSize * 0.5f);
                }
                else
                {
                    Vector2 solidWaxPosition = GetWaxSolidPosition(i, view.Particles.Count);
                    Vector2 softenedLiquidPosition = GetWaxLiquidPosition(particle, i);
                    particle.Position = Vector2.Lerp(solidWaxPosition, softenedLiquidPosition, meltAmount);
                }

                Vector2 vibration = stage == 2
                    ? Vector2.zero
                    : new Vector2(Mathf.Sin(elapsed * Mathf.Lerp(13f, 27f, heatAmount) + particle.Phase), Mathf.Cos(elapsed * 11f + particle.Phase)) * Mathf.Lerp(4f, 18f, motion);
                particle.Rect.anchoredPosition = particle.Position + vibration;
                particle.Image.color = Color.Lerp(Animation.GetWaxParticleColor(i), Animation.GetMeltedWaxParticleColor(i), meltAmount);
                particle.Rect.localScale = Vector3.one * (stage == 2
                    ? 1.06f + Mathf.Sin(elapsed * 3.2f + particle.Phase) * 0.02f
                    : Mathf.Lerp(0.92f, 1.16f + Mathf.Sin(elapsed * 8f + particle.Phase) * 0.05f, heatAmount));
            }

            Animation.UpdateBonds(view.Bonds, 1f - meltAmount);
            Animation.SetFlowLineAlpha(view.FlowLines, 0f);
            Animation.SetContainerAlpha(view.Container, 0.92f);
            Animation.SetIceCubeAlpha(view.IceCube, 0f);
        }

        private Vector2 GetWaxSolidPosition(int index, int count)
        {
            int columns = Mathf.CeilToInt(Mathf.Sqrt(count * 0.9f));
            int rows = Mathf.CeilToInt(count / (float)columns);
            int column = index % columns;
            int row = index / columns;

            float rowProgress = rows <= 1 ? 0.5f : row / (float)(rows - 1);
            float columnProgress = columns <= 1 ? 0.5f : column / (float)(columns - 1);
            float y = Mathf.Lerp(Animation.particleAreaSize.y * 0.08f, -Animation.particleAreaSize.y * 0.32f, rowProgress);
            float halfWidth = Mathf.Min(
                Animation.particleAreaSize.x * 0.082f,
                Mathf.Max(0f, Animation.GetContainerHalfWidth(y) - Animation.particleSize * 0.7f));
            float x = Mathf.Lerp(-halfWidth, halfWidth, columnProgress);

            if (row % 2 == 1)
                x += halfWidth / Mathf.Max(1f, columns) * 0.45f;

            return Animation.ClampInsideContainer(new Vector2(x, y), Animation.particleSize * 0.55f);
        }

        private Vector2 GetWaxLiquidPosition(ParticleView particle, int index)
        {
            float sourceYProgress = Mathf.InverseLerp(
                -Animation.particleAreaSize.y * 0.34f,
                Animation.particleAreaSize.y * 0.34f,
                particle.LiquidPosition.y);
            float y = Mathf.Lerp(-Animation.particleAreaSize.y * 0.31f, Animation.particleAreaSize.y * 0.05f, sourceYProgress);
            float x = particle.LiquidPosition.x * 0.2f + Mathf.Sin(index * 1.7f) * Animation.particleSize * 0.25f;
            return Animation.ClampInsideContainer(new Vector2(x, y), Animation.particleSize * 0.5f);
        }

        private static Vector2 GetWaxLiquidDrift(float elapsed, float phase, int index)
        {
            float stagger = index * 0.37f;
            return new Vector2(
                Mathf.Sin(elapsed * 2.2f + phase + stagger) * 24f + Mathf.Sin(elapsed * 0.9f + phase * 1.7f) * 12f,
                Mathf.Cos(elapsed * 1.8f + phase * 0.8f + stagger) * 14f);
        }
    }

    private sealed class CircuitPlasmaIonizingCutsceneBehavior : StateChangeCutsceneBehavior
    {
        public CircuitPlasmaIonizingCutsceneBehavior(StateChangeCutsceneAnimation animation) : base(animation)
        {
        }

        public override bool UsesPlasmaTubeContainer => true;
        public override string Title => "Gas to Plasma";

        public override string GetStageLabel(int stage)
        {
            return stage == 0 ? "Gas particles move freely inside the tube." :
                stage == 1 ? "Electrical energy separates charges from gas particles." :
                "Charged plasma particles glow and race through the tube.";
        }

        public override Color GetParticleColor(int index)
        {
            return Animation.GetGasParticleColor(index);
        }

        public override void Tick(CutsceneView view, int stage, float progress, float elapsed, float deltaTime)
        {
            float ionization = stage == 0 ? 0f : stage == 1 ? Mathf.SmoothStep(0f, 1f, progress) : 1f;
            float halfWidth = Animation.particleAreaSize.x * 0.39f;
            float halfHeight = Animation.particleAreaSize.y * 0.22f;
            float speed = Mathf.Lerp(0.28f, 1.95f, ionization);

            for (int i = 0; i < view.Particles.Count; i++)
            {
                ParticleView particle = view.Particles[i];

                if (stage == 0)
                {
                    particle.Position += particle.Velocity * deltaTime * 0.24f;
                    particle.Position = Animation.BounceInsidePlasmaTube(particle.Position, ref particle.Velocity, Animation.particleSize * 0.5f);
                }
                else
                {
                    float t = Mathf.Repeat(elapsed * speed + i / (float)view.Particles.Count, 1f);
                    Vector2 plasmaPath = Animation.PositionOnEllipse(t, halfWidth, halfHeight);
                    Vector2 arcOffset = new Vector2(
                        Mathf.Sin(elapsed * 19f + particle.Phase) * Mathf.Lerp(4f, 28f, ionization),
                        Mathf.Cos(elapsed * 23f + particle.Phase) * Mathf.Lerp(2f, 18f, ionization));
                    particle.Position = Animation.ClampInsidePlasmaTube(
                        Vector2.Lerp(particle.StageStartPosition, plasmaPath + arcOffset, ionization),
                        Animation.particleSize * 0.5f);
                }

                particle.Rect.anchoredPosition = particle.Position;
                particle.Image.color = Color.Lerp(Animation.GetGasParticleColor(i), Animation.GetPlasmaParticleColor(i, elapsed), ionization);
                particle.Rect.localScale = Vector3.one * Mathf.Lerp(0.78f, 1.34f + Mathf.Sin(elapsed * 16f + particle.Phase) * 0.12f, ionization);
            }

            Animation.UpdateBonds(view.Bonds, 0f);
            Animation.SetFlowLineAlpha(view.FlowLines, 0f);
            Animation.SetContainerAlpha(view.Container, Mathf.Lerp(0.26f, 0.92f, ionization));
            Animation.SetIceCubeAlpha(view.IceCube, 0f);
        }
    }
}
