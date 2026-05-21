using System.Collections;
using UnityEngine;

public static class DialogueWaitUtility
{
    public static bool IsBusy(DialogueRunner runner = null)
    {
        runner = ResolveRunner(runner);
        return runner != null && (runner.IsPlaying || runner.QueuedCount > 0);
    }

    public static IEnumerator WaitUntilIdle(DialogueRunner runner = null)
    {
        runner = ResolveRunner(runner);

        while (runner != null && (runner.IsPlaying || runner.QueuedCount > 0))
            yield return null;
    }

    private static DialogueRunner ResolveRunner(DialogueRunner runner)
    {
        return runner != null
            ? runner
            : Object.FindAnyObjectByType<DialogueRunner>(FindObjectsInactive.Include);
    }
}
