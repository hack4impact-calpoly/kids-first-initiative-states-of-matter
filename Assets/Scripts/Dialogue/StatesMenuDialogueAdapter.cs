using UnityEngine;
using UnityEngine.SceneManagement;

public class StatesMenuDialogueAdapter : DialogueFlowAdapterBase
{
    public const string IntroKey = "menu.states.intro";

    [SerializeField] private bool playIntroOnStart = false;

    private bool defaultsRegistered;
    private bool introPlayed;

    private void Awake()
    {
        EnsureFlowController();
        RegisterDefaultFlowsIfNeeded();
    }

    private void Start()
    {
        PlayIntroIfNeeded();
    }

    public void PlayIntroIfNeeded()
    {
        if (!playIntroOnStart || introPlayed || !IsStatesMenuScene())
            return;

        if (TryPlayFlow(IntroKey))
            introPlayed = true;
    }

    private void RegisterDefaultFlowsIfNeeded()
    {
        if (!registerDefaultFlows || defaultsRegistered)
            return;

        EnsureFlowController();
        if (flowController == null)
            return;

        RegisterLine(
            IntroKey,
            "menu.states.intro.1",
            null,
            "Pick a game to see solids, liquids, gases, and plasma change.",
            new[] { "solid", "liquid", "gas", "state-change" });

        defaultsRegistered = true;
    }

    private static bool IsStatesMenuScene()
    {
        return SceneManager.GetActiveScene().name == "States of Matter Menu";
    }
}
