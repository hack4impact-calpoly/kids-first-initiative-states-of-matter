using UnityEngine;

public partial class StateChangeCutsceneAnimation
{
    private sealed class PipeFlowCutsceneBehavior : StateChangeCutsceneBehavior
    {
        private readonly bool freezes;

        public PipeFlowCutsceneBehavior(StateChangeCutsceneContext context, bool freezes) : base(context)
        {
            this.freezes = freezes;
        }

        public override string Title => freezes ? "Freezing Flow" : "Liquid Flow";

        public override string GetStageLabel(int stage)
        {
            if (freezes)
            {
                return stage == 0 ? "Water reaches the T-junction and leaks out the bottom." :
                    stage == 1 ? "Freezing blocks the bottom branch." :
                    "With the bottom sealed, water flows straight through.";
            }

            return stage == 0 ? "Liquid particles stay close together." :
                stage == 1 ? "They flow through connected pipes and take the pipe shape." :
                "A complete path lets water reach the end.";
        }

        public override Color GetParticleColor(int index)
        {
            return freezes
                ? Color.Lerp(new Color(0.35f, 0.85f, 1f, 1f), new Color(0.8f, 1f, 1f, 1f), (index % 4) * 0.2f)
                : Context.GetWaterParticleColor(index);
        }

        public override void Tick(CutsceneView view, int stage, float progress, float elapsed, float deltaTime)
        {
            if (!freezes)
            {
                TickStraightPipe(view, elapsed);
                return;
            }

            float freezeAmount = stage == 0 ? 0f : stage == 1 ? Mathf.SmoothStep(0f, 1f, progress) : 1f;
            float speed = Mathf.Lerp(190f, 230f, freezeAmount);
            float pipeY = Context.ParticleAreaSize.y * 0.1f;
            float halfWidth = Context.ParticleAreaSize.x * 0.42f;
            float dropLength = Context.ParticleAreaSize.y * 0.52f;
            float leakPathLength = halfWidth + dropLength;
            float straightPathLength = halfWidth * 2f;

            for (int i = 0; i < view.Particles.Count; i++)
            {
                ParticleView particle = view.Particles[i];
                float lane = -Context.ParticleSize * 1.2f + (i % 5) * Context.ParticleSize * 0.6f;
                float offset = i * 47f;
                float leakDistance = Mathf.Repeat(elapsed * speed + offset, leakPathLength);
                float straightDistance = Mathf.Repeat(elapsed * speed + offset, straightPathLength);
                Vector2 leaking = SampleLeakingPath(leakDistance, lane, halfWidth, dropLength, pipeY, elapsed, particle.Phase);
                Vector2 straight = SampleStraightPath(straightDistance, lane, halfWidth, pipeY, elapsed, particle.Phase);
                particle.Position = Vector2.Lerp(leaking, straight, freezeAmount);
                particle.Rect.anchoredPosition = particle.Position;
            }

            Context.UpdateBonds(view.Bonds, 0f);
            Context.SetFlowLineAlpha(view.FlowLines, Mathf.Lerp(0.26f, 0.18f, freezeAmount));
            Context.SetPipeBackground(view.PipeBackground, 1f, freezeAmount, elapsed);
        }

        private void TickStraightPipe(CutsceneView view, float elapsed)
        {
            float speed = 180f;
            float pipeY = Context.ParticleAreaSize.y * 0.1f;
            float halfWidth = Context.ParticleAreaSize.x * 0.42f;
            float pathLength = halfWidth * 2f;

            for (int i = 0; i < view.Particles.Count; i++)
            {
                ParticleView particle = view.Particles[i];
                float lane = -Context.ParticleSize * 1.2f + (i % 5) * Context.ParticleSize * 0.6f;
                float distance = Mathf.Repeat(elapsed * speed + i * 47f, pathLength);
                particle.Position = SampleStraightPath(distance, lane, halfWidth, pipeY, elapsed, particle.Phase);
                particle.Rect.anchoredPosition = particle.Position;
            }

            Context.UpdateBonds(view.Bonds, 0f);
            Context.SetFlowLineAlpha(view.FlowLines, 0.32f);
            Context.SetPipeBackground(view.PipeBackground, 1f, 0f, elapsed);
        }

        private static Vector2 SampleLeakingPath(float distance, float lane, float halfWidth, float dropLength, float pipeY, float elapsed, float phase)
        {
            float wobble = Mathf.Sin(elapsed * 6f + phase) * 7f;
            if (distance < halfWidth)
                return new Vector2(-halfWidth + distance, pipeY + lane + wobble);

            float drop = Mathf.Min(dropLength, distance - halfWidth);
            return new Vector2(lane * 0.68f + wobble * 0.15f, pipeY - drop);
        }

        private static Vector2 SampleStraightPath(float distance, float lane, float halfWidth, float pipeY, float elapsed, float phase)
        {
            float wobble = Mathf.Sin(elapsed * 6f + phase) * 6f;
            return new Vector2(distance - halfWidth, pipeY + lane + wobble);
        }
    }
}
