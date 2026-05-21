using UnityEngine;

public class DialogueAdvanceInput : MonoBehaviour
{
    [SerializeField] private DialogueRunner runner;
    [SerializeField] private KeyCode[] advanceKeys = { KeyCode.Space, KeyCode.Return, KeyCode.E };
    [SerializeField] private bool advanceOnPrimaryMouseButton;

    private void Awake()
    {
        if (runner == null)
            runner = FindAnyObjectByType<DialogueRunner>();
    }

    private void Update()
    {
        if (runner == null || !runner.IsPlaying)
            return;

        if (advanceOnPrimaryMouseButton && Input.GetMouseButtonDown(0))
        {
            runner.Advance();
            return;
        }

        for (int i = 0; i < advanceKeys.Length; i++)
        {
            if (Input.GetKeyDown(advanceKeys[i]))
            {
                runner.Advance();
                return;
            }
        }
    }
}
