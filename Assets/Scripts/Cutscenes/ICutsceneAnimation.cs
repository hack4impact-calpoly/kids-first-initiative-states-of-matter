using System.Collections;

public interface ICutsceneAnimation
{
    IEnumerator Play(CutsceneContext context);
}

public interface ICutsceneAnimationCleanup
{
    void Cleanup(CutsceneContext context);
}
