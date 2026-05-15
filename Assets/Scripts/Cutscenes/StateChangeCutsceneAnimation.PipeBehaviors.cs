using UnityEngine;

public partial class StateChangeCutsceneAnimation
{
    private sealed class PipeFlowCutsceneBehavior : StateChangeCutsceneBehavior
    {
        private readonly bool freezes;

        public PipeFlowCutsceneBehavior(StateChangeCutsceneAnimation animation, bool freezes) : base(animation)
        {
            this.freezes = freezes;
        }

        public override string Title => freezes ? "Freezing Flow" : "Liquid Flow";

        public override string GetStageLabel(int stage)
        {
            if (freezes)
            {
                return stage == 0 ? "Water particles move through open pipe paths." :
                    stage == 1 ? "Freezing removes energy and stops unwanted flow." :
                    "Frozen water holds its shape as a solid barrier.";
            }

            return stage == 0 ? "Liquid particles stay close together." :
                stage == 1 ? "They flow through connected pipes and take the pipe shape." :
                "A complete path lets water reach the end.";
        }

        public override Color GetParticleColor(int index)
        {
            return freezes
                ? Color.Lerp(new Color(0.35f, 0.85f, 1f, 1f), new Color(0.8f, 1f, 1f, 1f), (index % 4) * 0.2f)
                : Animation.GetWaterParticleColor(index);
        }

        public override void Tick(CutsceneView view, int stage, float progress, float elapsed, float deltaTime)
        {
            float freezeAmount = freezes ? (stage == 0 ? 0f : stage == 1 ? Mathf.SmoothStep(0f, 1f, progress) : 1f) : 0f;
            float speed = Mathf.Lerp(180f, 12f, freezeAmount);
            float halfWidth = Animation.particleAreaSize.x * 0.45f;

            for (int i = 0; i < view.Particles.Count; i++)
            {
                ParticleView particle = view.Particles[i];
                float lane = -120f + (i % 5) * 60f;
                float x = Mathf.Repeat(particle.LiquidPosition.x + elapsed * speed + i * 43f, halfWidth * 2f) - halfWidth;
                Vector2 flowing = new Vector2(x, lane + Mathf.Sin(elapsed * 6f + particle.Phase) * Mathf.Lerp(13f, 1.5f, freezeAmount));
                Vector2 frozen = particle.SolidPosition;
                particle.Position = Vector2.Lerp(flowing, frozen, freezeAmount);
                particle.Rect.anchoredPosition = particle.Position;
            }

            Animation.UpdateBonds(view.Bonds, freezeAmount);
            Animation.SetFlowLineAlpha(view.FlowLines, Mathf.Lerp(0.38f, 0.08f, freezeAmount));
        }
    }
}
