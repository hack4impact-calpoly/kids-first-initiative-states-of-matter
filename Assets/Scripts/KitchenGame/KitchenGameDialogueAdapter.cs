using UnityEngine;

public class KitchenGameDialogueAdapter : DialogueFlowAdapterBase
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

        if (!string.IsNullOrWhiteSpace(introKey) && TryPlayFlow(introKey))
            introPlayed = true;
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
        TryPlayFlow(SolidIngredientAddedKey);
    }

    private void OnSolidMaxHeatReached(float heat)
    {
        TryPlayFlow(SolidHeatActiveKey);
    }

    private void OnSolidWinPresentationShown()
    {
        TryPlayFlow(SolidWinKey);
    }

    private void OnSolidFailed()
    {
        TryPlayFlow(SolidFailKey);
    }

    private void OnPourTrayFillStarted()
    {
        TryPlayFlow(PourActiveKey);
    }

    private void OnPourStepCompleted()
    {
        TryPlayFlow(PourWinKey);
    }

    private void OnStationColdEnoughReached()
    {
        TryPlayFlow(StationActiveKey);
    }

    private void OnStationFreezingCompleted()
    {
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

        RegisterKitchenLine(SolidIntroKey, "kitchen.solid.intro.1", sam, "Chocolate is a SOLID - see how it holds its shape? Drag a piece into the pot.", "solid");
        RegisterKitchenLine(SolidIngredientAddedKey, "kitchen.solid.ingredient_added.1", sam, "Nice! Now the stove unlocks. Crank up the heat and watch what happens to my chocolate friend.", "solid", "melting");
        RegisterKitchenLine(SolidHeatActiveKey, "kitchen.solid.heat_active.1", sam, "Heat makes the molecules wiggle faster and faster...", "molecules", "melting");
        RegisterKitchenLine(SolidWinKey, "kitchen.solid.win.1", sam, "Melted! Solids turn to LIQUID when they get hot enough - that's called melting.", "solid", "liquid", "melting");
        RegisterKitchenLine(SolidFailKey, "kitchen.solid.fail.1", sam, "Add the chocolate before turning the heat all the way up.", "solid", "melting", playOnce: false);
        RegisterKitchenLine(PourIntroKey, "kitchen.freezing_pour.intro.1", lucy, "Hi, I'm Lucy! Juice is mostly water - and water is a LIQUID. Pour me into the freezer!", "liquid");
        RegisterKitchenLine(PourActiveKey, "kitchen.freezing_pour.active.1", lucy, "Liquids take the shape of their container. Watch me fill up the freezer.", "liquid");
        RegisterKitchenLine(PourWinKey, "kitchen.freezing_pour.win.1", lucy, "Brrr - I'm getting cold. Time to make ice cubes!", "liquid", "freezing");
        RegisterKitchenLine(StationIntroKey, "kitchen.freezing_station.intro.1", sam, "Cold liquid turns into solid ice. Fill up the tray and watch!", "liquid", "solid", "freezing");
        RegisterKitchenLine(StationActiveKey, "kitchen.freezing_station.active.1", sam, "When water gets really cold, the molecules slow down and lock together - that's how ice forms.", "molecules", "freezing", "solid");
        RegisterKitchenLine(StationWinKey, "kitchen.freezing_station.win.1", sam, "We did it! Liquid to solid means freezing. The opposite of melting!", "liquid", "solid", "freezing", "melting");

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
