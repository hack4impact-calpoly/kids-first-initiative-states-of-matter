using UnityEngine;

public class WireGameDialogueAdapter : DialogueFlowAdapterBase
{
    private const string OutputGuidanceTag = "wire.guidance.output";
    private const string WireGuidanceTag = "wire.guidance.wires";
    private const string PowerDialGuidanceTag = "wire.guidance.power";
    private const string ClearGuidanceTag = "wire.guidance.clear";

    public const string DragDeviceIntroKey = "wire.drag_device.intro";
    public const string DragDeviceHintKey = "wire.drag_device.hint";
    public const string ConnectWiresIntroKey = "wire.connect_wires.intro";
    public const string ConnectWiresHintKey = "wire.connect_wires.hint";
    public const string TurnOnPowerKey = "wire.power.turn_on";
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
        gameManager.PowerStateChanged += OnPowerStateChanged;
        gameManager.WireInteractionBlocked += OnWireInteractionBlocked;
        gameManager.PowerDialInteractionBlocked += OnPowerDialInteractionBlocked;
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
        subscribedGameManager.PowerStateChanged -= OnPowerStateChanged;
        subscribedGameManager.WireInteractionBlocked -= OnWireInteractionBlocked;
        subscribedGameManager.PowerDialInteractionBlocked -= OnPowerDialInteractionBlocked;
        subscribedGameManager.CircuitCompleted -= OnCircuitCompleted;
        subscribedGameManager.WinPresentationShown -= OnWinPresentationShown;
        subscribedGameManager = null;
    }

    private void OnDeviceConnected(DraggableDevice device)
    {
        if (!TryPlayPowerPromptIfReady())
            TryPlay(ConnectWiresIntroKey);
    }

    private void OnWireConnectionCountChanged(int connectedCount, int requiredCount)
    {
        if (connectedCount >= requiredCount && requiredCount > 0)
        {
            TryPlayPowerPromptIfReady();
            return;
        }

        if (connectedCount >= wireHintConnectedThreshold && connectedCount < requiredCount)
            TryPlay(ConnectWiresHintKey);
    }

    private void OnPowerStateChanged(bool isPoweredOn)
    {
        if (!isPoweredOn)
            TryPlayPowerPromptIfReady();
    }

    private void OnWireInteractionBlocked()
    {
        TryPlay(DragDeviceHintKey);
    }

    private void OnPowerDialInteractionBlocked()
    {
        ResolveGameManager();

        if (gameManager == null || !gameManager.HasOutputConnected)
        {
            TryPlay(DragDeviceHintKey);
            return;
        }

        if (!gameManager.AreAllWiresConnected)
        {
            TryPlay(RetryIncompleteKey);
            return;
        }

        TryPlayPowerPromptIfReady();
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
        return TryPlayFlowNow(key);
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

        RegisterWireLine(DragDeviceIntroKey, "wire.drag_device.intro.1", patrice, "Choose a device to show how energy changes matter.", OutputGuidanceTag);
        RegisterWireLine(DragDeviceHintKey, "wire.drag_device.hint.1", patrice, "Heat melts or evaporates. Cold freezes. Energy makes plasma.", OutputGuidanceTag);
        RegisterWireLine(ConnectWiresIntroKey, "wire.connect_wires.intro.1", patrice, "Connect wire ends so electrical energy can flow.", WireGuidanceTag);
        RegisterWireLine(ConnectWiresHintKey, "wire.connect_wires.hint.1", patrice, "Every wire needs a closed path before power works.", WireGuidanceTag);
        RegisterWireLine(TurnOnPowerKey, "wire.power.turn_on.1", patrice, "All wired. Turn on power to transfer energy.", PowerDialGuidanceTag);
        RegisterWireLine(HotPlateSuccessKey, "wire.success.hot_plate.1", gary, "Heat evaporated liquid water into gas.", ClearGuidanceTag);
        RegisterWireLine(IceFlaskSuccessKey, "wire.success.ice_flask.1", sam, "Cooling froze liquid water into solid ice.", ClearGuidanceTag);
        RegisterWireLine(CandleSuccessKey, "wire.success.candle.1", sam, "Heat melted solid wax into liquid wax.", ClearGuidanceTag);
        RegisterWireLine(PlasmaSuccessKey, "wire.success.plasma.1", patrice, "Energy ionized gas into glowing plasma.", ClearGuidanceTag);
        RegisterWireLine(GenericSuccessKey, "wire.success.generic.1", patrice, "Energy changed the matter into a new state.", ClearGuidanceTag);
        RegisterWireLine(FinalWinKey, "wire.win.final.1", patrice, "Circuit complete. Energy drove a state change.", ClearGuidanceTag);
        RegisterWireLine(RetryIncompleteKey, "wire.retry.incomplete.1", patrice, "The circuit is open. Connect every wire.", WireGuidanceTag, playOnce: false);

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

    private bool TryPlayPowerPromptIfReady()
    {
        ResolveGameManager();

        if (gameManager == null || gameManager.HasWon || gameManager.IsPowerOn)
            return false;

        if (!gameManager.HasOutputConnected || !gameManager.AreAllWiresConnected)
            return false;

        return TryPlay(TurnOnPowerKey);
    }

}
