using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class RetryButton : MonoBehaviour
{
    [SerializeField] private bool playDialogueBeforeReload = true;
    [SerializeField] private string retryDialogueKey = WireGameDialogueAdapter.RetryIncompleteKey;
    [SerializeField] private float reloadDelay = 1.4f;
    [SerializeField] private DialogueFlowController flowController;

    public void OnRetryButtonPressed()
    {
        if (playDialogueBeforeReload && TryPlayRetryDialogue())
        {
            StartCoroutine(ReloadAfterDelay());
            return;
        }

        ReloadScene();
    }

    private bool TryPlayRetryDialogue()
    {
        if (flowController == null)
            flowController = FindAnyObjectByType<DialogueFlowController>(FindObjectsInactive.Include);

        return flowController != null && flowController.TryPlayNow(retryDialogueKey);
    }

    private IEnumerator ReloadAfterDelay()
    {
        if (reloadDelay > 0f)
            yield return new WaitForSecondsRealtime(reloadDelay);

        ReloadScene();
    }

    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
