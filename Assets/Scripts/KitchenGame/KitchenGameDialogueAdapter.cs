using UnityEngine;

public class KitchenGameDialogueAdapter : DialogueFlowAdapterBase, IActivityHintProvider
{
    public const string SolidIntroKey = "kitchen.solid.intro";
    public const string SolidIngredientAddedKey = "kitchen.solid.ingredient_added";
    public const string SolidHeatActiveKey = "kitchen.solid.heat_active";
    public const string SolidWinKey = "kitchen.solid.win";
    public const string SolidFailKey = "kitchen.solid.fail";
    public const string PourIntroKey = "kitchen.freezing_pour.intro";
    public const string PourActiveKey = "kitchen.freezing_pour.active";
    public const string PourWinKey = "kitchen.freezing_pour.win";
    public const string StationIntroKey = "kitchen.freezing_station.intro";
    public const string StationActiveKey = "kitchen.freezing_station.active";
    public const string StationWinKey = "kitchen.freezing_station.win";

    [SerializeField] private KitchenGameManager solidManager;
    [SerializeField] private JuiceFreezingManager freezingPourManager;
    [SerializeField] private JuicePouringGameManager freezingStationManager;
    [SerializeField] private bool playIntroOnStart = true;

    private KitchenGameManager subscribedSolidManager;
    private JuiceFreezingManager subscribedFreezingPourManager;
    private JuicePouringGameManager subscribedFreezingStationManager;
    private bool defaultsRegistered;
    private bool introPlayed;
    private string currentHintKey;

    private void Awake()
    {
        ResolveManagers();
        EnsureFlowController();
        RegisterDefaultFlowsIfNeeded();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        PlayIntroIfNeeded();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Initialize(KitchenGameManager manager)
    {
        solidManager = manager;
        EnsureFlowController();
        RegisterDefaultFlowsIfNeeded();
        Subscribe();
    }

    public void Initialize(JuiceFreezingManager manager)
    {
        freezingPourManager = manager;
        EnsureFlowController();
        RegisterDefaultFlowsIfNeeded();
        Subscribe();
    }

    public void Initialize(JuicePouringGameManager manager)
    {
        freezingStationManager = manager;
        EnsureFlowController();
        RegisterDefaultFlowsIfNeeded();
        Subscribe();
    }

    public void PlayIntroIfNeeded()
    {
        if (!playIntroOnStart || introPlayed)
            return;

        ResolveManagers();

        string introKey = null;
        if (solidManager != null)
            introKey = SolidIntroKey;
        else if (freezingPourManager != null)
            introKey = PourIntroKey;
        else if (freezingStationManager != null)
            introKey = StationIntroKey;

        currentHintKey = introKey;
        if (!string.IsNullOrWhiteSpace(introKey) && TryPlayFlow(introKey))
            introPlayed = true;
    }

    public void ReplayCurrentInstruction()
    {
        if (string.IsNullOrWhiteSpace(currentHintKey))
        {
            ResolveManagers();
            currentHintKey = solidManager != null
                ? SolidIntroKey
                : freezingPourManager != null
                    ? PourIntroKey
                    : StationIntroKey;
        }

        if (!string.IsNullOrWhiteSpace(currentHintKey))
            ReplayFlowNow(currentHintKey);
    }

    private void Subscribe()
    {
        ResolveManagers();
        Unsubscribe();

        if (solidManager != null)
        {
            solidManager.RequiredIngredientAdded += OnSolidRequiredIngredientAdded;
            solidManager.MaxHeatReached += OnSolidMaxHeatReached;
            solidManager.WinPresentationShown += OnSolidWinPresentationShown;
            solidManager.Failed += OnSolidFailed;
            subscribedSolidManager = solidManager;
        }

        if (freezingPourManager != null)
        {
            freezingPourManager.TrayFillStarted += OnPourTrayFillStarted;
            freezingPourManager.PourStepCompleted += OnPourStepCompleted;
            subscribedFreezingPourManager = freezingPourManager;
        }

        if (freezingStationManager != null)
        {
            freezingStationManager.ColdEnoughReached += OnStationColdEnoughReached;
            freezingStationManager.FreezingCompleted += OnStationFreezingCompleted;
            subscribedFreezingStationManager = freezingStationManager;
        }
    }

    private void Unsubscribe()
    {
        if (subscribedSolidManager != null)
        {
            subscribedSolidManager.RequiredIngredientAdded -= OnSolidRequiredIngredientAdded;
            subscribedSolidManager.MaxHeatReached -= OnSolidMaxHeatReached;
            subscribedSolidManager.WinPresentationShown -= OnSolidWinPresentationShown;
            subscribedSolidManager.Failed -= OnSolidFailed;
            subscribedSolidManager = null;
        }

        if (subscribedFreezingPourManager != null)
        {
            subscribedFreezingPourManager.TrayFillStarted -= OnPourTrayFillStarted;
            subscribedFreezingPourManager.PourStepCompleted -= OnPourStepCompleted;
            subscribedFreezingPourManager = null;
        }

        if (subscribedFreezingStationManager != null)
        {
            subscribedFreezingStationManager.ColdEnoughReached -= OnStationColdEnoughReached;
            subscribedFreezingStationManager.FreezingCompleted -= OnStationFreezingCompleted;
            subscribedFreezingStationManager = null;
        }
    }

    private void OnSolidRequiredIngredientAdded(IngredientSO ingredient)
    {
        currentHintKey = SolidIngredientAddedKey;
        TryPlayFlow(SolidIngredientAddedKey);
    }

    private void OnSolidMaxHeatReached(float heat)
    {
        currentHintKey = SolidHeatActiveKey;
        TryPlayFlow(SolidHeatActiveKey);
    }

    private void OnSolidWinPresentationShown()
    {
        currentHintKey = SolidWinKey;
        TryPlayFlow(SolidWinKey);
    }

    private void OnSolidFailed()
    {
        currentHintKey = SolidFailKey;
        TryPlayFlow(SolidFailKey);
    }

    private void OnPourTrayFillStarted()
    {
        currentHintKey = PourActiveKey;
        TryPlayFlow(PourActiveKey);
    }

    private void OnPourStepCompleted()
    {
        currentHintKey = PourWinKey;
        TryPlayFlow(PourWinKey);
    }

    private void OnStationColdEnoughReached()
    {
        currentHintKey = StationActiveKey;
        TryPlayFlow(StationActiveKey);
    }

    private void OnStationFreezingCompleted()
    {
        currentHintKey = StationWinKey;
        TryPlayFlow(StationWinKey);
    }

    private void RegisterDefaultFlowsIfNeeded()
    {
        if (!registerDefaultFlows || defaultsRegistered)
            return;

        EnsureFlowController();
        if (flowController == null)
            return;

        DialogueSpeaker sam = ResolveSpeaker("Sam");
        DialogueSpeaker lucy = ResolveSpeaker("Lucy");

        RegisterKitchenLine(SolidIntroKey, "kitchen.solid.intro.1", sam, "Chocolate is a solid: it keeps its shape. Drag it into the pot.", "solid");
        RegisterKitchenLine(SolidIngredientAddedKey, "kitchen.solid.ingredient_added.1", sam, "Now add heat. Heat can melt a solid into liquid.", "solid", "melting");
        RegisterKitchenLine(SolidHeatActiveKey, "kitchen.solid.heat_active.1", sam, "Heat makes the molecules move faster.", "molecules", "melting");
        RegisterKitchenLine(SolidWinKey, "kitchen.solid.win.1", sam, "Melting changed solid chocolate into liquid chocolate.", "solid", "liquid", "melting");
        RegisterKitchenLine(SolidFailKey, "kitchen.solid.fail.1", sam, "Start with the solid chocolate, then add heat.", "solid", "melting", playOnce: false);
        RegisterKitchenLine(PourIntroKey, "kitchen.freezing_pour.intro.1", lucy, "Juice is a liquid: it flows. Pour it into the tray.", "liquid");
        RegisterKitchenLine(PourActiveKey, "kitchen.freezing_pour.active.1", lucy, "A liquid takes the shape of its container.", "liquid");
        RegisterKitchenLine(PourWinKey, "kitchen.freezing_pour.win.1", lucy, "The tray is full of liquid. Next, cool it down.", "liquid", "freezing");
        RegisterKitchenLine(StationIntroKey, "kitchen.freezing_station.intro.1", sam, "Cooling a liquid enough can freeze it solid.", "liquid", "solid", "freezing");
        RegisterKitchenLine(StationActiveKey, "kitchen.freezing_station.active.1", sam, "Cold slows molecules until they lock into ice.", "molecules", "freezing", "solid");
        RegisterKitchenLine(StationWinKey, "kitchen.freezing_station.win.1", sam, "Freezing changed liquid water into solid ice.", "liquid", "solid", "freezing", "melting");

        defaultsRegistered = true;
    }

    private void RegisterKitchenLine(
        string key,
        string lineId,
        DialogueSpeaker speaker,
        string text,
        params string[] tags)
    {
        RegisterLine(key, lineId, speaker, text, tags);
    }

    private void RegisterKitchenLine(
        string key,
        string lineId,
        DialogueSpeaker speaker,
        string text,
        string firstTag,
        string secondTag,
        bool playOnce)
    {
        RegisterLine(key, lineId, speaker, text, new[] { firstTag, secondTag }, playOnce);
    }

    private void ResolveManagers()
    {
        if (solidManager == null)
            solidManager = FindAnyObjectByType<KitchenGameManager>();

        if (freezingPourManager == null)
            freezingPourManager = FindAnyObjectByType<JuiceFreezingManager>();

        if (freezingStationManager == null)
            freezingStationManager = FindAnyObjectByType<JuicePouringGameManager>();
    }
}
