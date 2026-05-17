using UnityEngine;

public class WireGameDialogueAdapter : DialogueFlowAdapterBase
{
    private const string OutputGuidanceTag = "wire.guidance.output";
    private const string WireGuidanceTag = "wire.guidance.wires";
    private const string ClearGuidanceTag = "wire.guidance.clear";

    public const string DragDeviceIntroKey = "wire.drag_device.intro";
    public const string DragDeviceHintKey = "wire.drag_device.hint";
    public const string ConnectWiresIntroKey = "wire.connect_wires.intro";
    public const string ConnectWiresHintKey = "wire.connect_wires.hint";
    public const string HotPlateSuccessKey = "wire.success.hot_plate";
    public const string IceFlaskSuccessKey = "wire.success.ice_flask";
    public const string CandleSuccessKey = "wire.success.candle";
    public const string PlasmaSuccessKey = "wire.success.plasma";
    public const string GenericSuccessKey = "wire.success.generic";
    public const string FinalWinKey = "wire.win.final";
    public const string RetryIncompleteKey = "wire.retry.incomplete";

    [SerializeField] private Main gameManager;
    [SerializeField] private bool playIntroOnStart = true;
    [SerializeField] private int wireHintConnectedThreshold = 2;

    private Main subscribedGameManager;
    private bool defaultsRegistered;
    private bool introPlayed;

    private void Awake()
    {
        ResolveGameManager();
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

    public void Initialize(Main manager)
    {
        gameManager = manager;
        EnsureFlowController();
        RegisterDefaultFlowsIfNeeded();
        Subscribe();
    }

    public void PlayIntroIfNeeded()
    {
        if (!playIntroOnStart || introPlayed)
            return;

        EnsureFlowController();

        if (flowController != null && flowController.TryPlay(DragDeviceIntroKey))
            introPlayed = true;
    }

    private void Subscribe()
    {
        ResolveGameManager();

        if (gameManager == null || subscribedGameManager == gameManager)
            return;

        Unsubscribe();
        gameManager.DeviceConnectedChanged += OnDeviceConnected;
        gameManager.WireConnectionCountChanged += OnWireConnectionCountChanged;
        gameManager.WireInteractionBlocked += OnWireInteractionBlocked;
        gameManager.CircuitCompleted += OnCircuitCompleted;
        gameManager.WinPresentationShown += OnWinPresentationShown;
        subscribedGameManager = gameManager;
    }

    private void Unsubscribe()
    {
        if (subscribedGameManager == null)
            return;

        subscribedGameManager.DeviceConnectedChanged -= OnDeviceConnected;
        subscribedGameManager.WireConnectionCountChanged -= OnWireConnectionCountChanged;
        subscribedGameManager.WireInteractionBlocked -= OnWireInteractionBlocked;
        subscribedGameManager.CircuitCompleted -= OnCircuitCompleted;
        subscribedGameManager.WinPresentationShown -= OnWinPresentationShown;
        subscribedGameManager = null;
    }

    private void OnDeviceConnected(DraggableDevice device)
    {
        TryPlay(ConnectWiresIntroKey);
    }

    private void OnWireConnectionCountChanged(int connectedCount, int requiredCount)
    {
        if (connectedCount >= wireHintConnectedThreshold && connectedCount < requiredCount)
            TryPlay(ConnectWiresHintKey);
    }

    private void OnWireInteractionBlocked()
    {
        TryPlay(DragDeviceHintKey);
    }

    private void OnCircuitCompleted(DraggableDevice device)
    {
        TryPlay(ResolveDeviceSuccessKey(device));
    }

    private void OnWinPresentationShown()
    {
        TryPlay(FinalWinKey);
    }

    private bool TryPlay(string key)
    {
        return TryPlayFlow(key);
    }

    private string ResolveDeviceSuccessKey(DraggableDevice device)
    {
        if (device == null)
            return GenericSuccessKey;

        if (device.GetComponentInChildren<PlasmaEffect>(true) != null)
            return PlasmaSuccessKey;

        if (device.GetComponentInChildren<CandleMeltEffect>(true) != null)
            return CandleSuccessKey;

        if (device.GetComponentInChildren<IceMeltEffect>(true) != null)
            return IceFlaskSuccessKey;

        if (device.GetComponentInChildren<HotPlateEffect>(true) != null)
            return HotPlateSuccessKey;

        return GenericSuccessKey;
    }

    private void RegisterDefaultFlowsIfNeeded()
    {
        if (!registerDefaultFlows || defaultsRegistered)
            return;

        EnsureFlowController();
        if (flowController == null)
            return;

        DialogueSpeaker patrice = ResolveSpeaker("Patrice");
        DialogueSpeaker gary = ResolveSpeaker("Gary");
        DialogueSpeaker sam = ResolveSpeaker("Sam");

        RegisterWireLine(DragDeviceIntroKey, "wire.drag_device.intro.1", patrice, "Each device shows a different state change. Drag one in to see what energy can do!", OutputGuidanceTag);
        RegisterWireLine(DragDeviceHintKey, "wire.drag_device.hint.1", patrice, "HotPlate heats things. IceFlask freezes. Candle melts wax. Plasma - that's me.", OutputGuidanceTag);
        RegisterWireLine(ConnectWiresIntroKey, "wire.connect_wires.intro.1", patrice, "Wires carry electricity. Connect every wire end-to-end so energy can flow.", WireGuidanceTag);
        RegisterWireLine(ConnectWiresHintKey, "wire.connect_wires.hint.1", patrice, "Drag a wire endpoint to connect it. Each device needs ALL its wires plugged in.", WireGuidanceTag);
        RegisterWireLine(HotPlateSuccessKey, "wire.success.hot_plate.1", gary, "HotPlate boiled the water! Heat to steam means gas. That's evaporation!", ClearGuidanceTag);
        RegisterWireLine(IceFlaskSuccessKey, "wire.success.ice_flask.1", sam, "IceFlask made ice! Cold to solid. That's freezing!", ClearGuidanceTag);
        RegisterWireLine(CandleSuccessKey, "wire.success.candle.1", sam, "Candle wax melted! Solid wax to liquid wax. That's melting!", ClearGuidanceTag);
        RegisterWireLine(PlasmaSuccessKey, "wire.success.plasma.1", patrice, "I'm GLOWING! Plasma is super-heated gas where molecules break apart and shine. The Sun is plasma!", ClearGuidanceTag);
        RegisterWireLine(GenericSuccessKey, "wire.success.generic.1", patrice, "The device is active! Electricity powered a change in matter.", ClearGuidanceTag);
        RegisterWireLine(FinalWinKey, "wire.win.final.1", patrice, "Energy flows! You powered up a state change. That's how electricity transforms matter.", ClearGuidanceTag);
        RegisterWireLine(RetryIncompleteKey, "wire.retry.incomplete.1", patrice, "Some wires aren't connected yet. Each device needs ALL of them.", WireGuidanceTag, playOnce: false);

        defaultsRegistered = true;
    }

    private void RegisterWireLine(
        string key,
        string lineId,
        DialogueSpeaker speaker,
        string text,
        string guidanceTag,
        bool playOnce = true)
    {
        RegisterLine(
            key,
            lineId,
            speaker,
            text,
            new[] { guidanceTag },
            playOnce);
    }

    private void ResolveGameManager()
    {
        if (gameManager == null)
            gameManager = Main.Instance != null ? Main.Instance : FindAnyObjectByType<Main>();
    }

}
