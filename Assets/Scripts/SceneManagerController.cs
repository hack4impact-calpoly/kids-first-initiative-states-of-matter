using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerController : MonoBehaviour
{
    [Header("Dialogue Flow")]
    [SerializeField] private bool createStatesMenuDialogueAdapterIfMissing = true;
    [SerializeField] private StatesMenuDialogueAdapter statesMenuDialogueAdapter;

    private void Awake()
    {
        ResolveStatesMenuDialogueAdapter();
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    private void ResolveStatesMenuDialogueAdapter()
    {
        if (SceneManager.GetActiveScene().name != "States of Matter Menu")
            return;

        if (statesMenuDialogueAdapter == null)
            statesMenuDialogueAdapter = FindAnyObjectByType<StatesMenuDialogueAdapter>(FindObjectsInactive.Include);

        if (statesMenuDialogueAdapter == null && createStatesMenuDialogueAdapterIfMissing)
            statesMenuDialogueAdapter = gameObject.AddComponent<StatesMenuDialogueAdapter>();
    }
}
