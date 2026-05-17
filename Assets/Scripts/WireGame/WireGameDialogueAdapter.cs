using System.Collections.Generic;
using UnityEngine;

public class WireGameDialogueAdapter : MonoBehaviour
{
    private const string SpeakerCatalogResourcePath = "Dialogue/DialogueSpeakerCatalog";
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
    [SerializeField] private DialogueFlowController flowController;
    [SerializeField] private bool createFlowControllerIfMissing = true;
    [SerializeField] private bool registerDefaultFlows = true;
    [SerializeField] private bool playIntroOnStart = true;
    [SerializeField] private int wireHintConnectedThreshold = 2;
    [SerializeField] private float promptAutoAdvanceDelay = 4f;
    [SerializeField] private DialogueSpeakerCatalog speakerCatalog;

    private Main subscribedGameManager;
    private bool defaultsRegistered;
    private bool introPlayed;

    private void Awake()
    {
        ResolveGameManager();
        ResolveFlowController();
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
        ResolveFlowController();
        RegisterDefaultFlowsIfNeeded();
        Subscribe();
    }

    public void PlayIntroIfNeeded()
    {
        if (!playIntroOnStart || introPlayed)
            return;

        ResolveFlowController();

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
        ResolveFlowController();
        return flowController != null && flowController.TryPlay(key);
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

        ResolveFlowController();
        if (flowController == null)
            return;

        DialogueSpeaker patrice = ResolveSpeaker("Patrice");
        DialogueSpeaker gary = ResolveSpeaker("Gary");
        DialogueSpeaker sam = ResolveSpeaker("Sam");

        RegisterLine(DragDeviceIntroKey, "wire.drag_device.intro.1", patrice, "Each device shows a different state change. Drag one in to see what energy can do!", OutputGuidanceTag);
        RegisterLine(DragDeviceHintKey, "wire.drag_device.hint.1", patrice, "HotPlate heats things. IceFlask freezes. Candle melts wax. Plasma - that's me.", OutputGuidanceTag);
        RegisterLine(ConnectWiresIntroKey, "wire.connect_wires.intro.1", patrice, "Wires carry electricity. Connect every wire end-to-end so energy can flow.", WireGuidanceTag);
        RegisterLine(ConnectWiresHintKey, "wire.connect_wires.hint.1", patrice, "Drag a wire endpoint to connect it. Each device needs ALL its wires plugged in.", WireGuidanceTag);
        RegisterLine(HotPlateSuccessKey, "wire.success.hot_plate.1", gary, "HotPlate boiled the water! Heat to steam means gas. That's evaporation!", ClearGuidanceTag);
        RegisterLine(IceFlaskSuccessKey, "wire.success.ice_flask.1", sam, "IceFlask made ice! Cold to solid. That's freezing!", ClearGuidanceTag);
        RegisterLine(CandleSuccessKey, "wire.success.candle.1", sam, "Candle wax melted! Solid wax to liquid wax. That's melting!", ClearGuidanceTag);
        RegisterLine(PlasmaSuccessKey, "wire.success.plasma.1", patrice, "I'm GLOWING! Plasma is super-heated gas where molecules break apart and shine. The Sun is plasma!", ClearGuidanceTag);
        RegisterLine(GenericSuccessKey, "wire.success.generic.1", patrice, "The device is active! Electricity powered a change in matter.", ClearGuidanceTag);
        RegisterLine(FinalWinKey, "wire.win.final.1", patrice, "Energy flows! You powered up a state change. That's how electricity transforms matter.", ClearGuidanceTag);
        RegisterLine(RetryIncompleteKey, "wire.retry.incomplete.1", patrice, "Some wires aren't connected yet. Each device needs ALL of them.", WireGuidanceTag, playOnce: false);

        defaultsRegistered = true;
    }

    private void RegisterLine(
        string key,
        string lineId,
        DialogueSpeaker speaker,
        string text,
        string guidanceTag,
        bool playOnce = true)
    {
        flowController.RegisterLines(
            key,
            new[]
            {
                new DialogueFlowLineDefinition(
                    lineId,
                    text,
                    speaker,
                    tags: new[] { guidanceTag },
                    requiresContinue: false,
                    autoAdvanceDelay: promptAutoAdvanceDelay)
            },
            playOnce,
            replaceExisting: false);
    }

    private void ResolveGameManager()
    {
        if (gameManager == null)
            gameManager = Main.Instance != null ? Main.Instance : FindAnyObjectByType<Main>();
    }

    private void ResolveFlowController()
    {
        if (flowController != null)
            return;

        flowController = FindAnyObjectByType<DialogueFlowController>(FindObjectsInactive.Include);

        if (flowController == null && createFlowControllerIfMissing)
            flowController = gameObject.AddComponent<DialogueFlowController>();
    }

    private DialogueSpeaker ResolveSpeaker(string displayName)
    {
        if (speakerCatalog == null)
            speakerCatalog = Resources.Load<DialogueSpeakerCatalog>(SpeakerCatalogResourcePath);

        return speakerCatalog != null ? speakerCatalog.FindByName(displayName) : null;
    }
}
