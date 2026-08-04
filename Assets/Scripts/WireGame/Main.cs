using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    static public Main Instance;

    private const string OutputConnectedCondition = "wire.output_connected";
    private const string PowerOnCondition = "wire.power_on";
    private const string ConnectedWireCountCondition = "wire.connected_wire_count";
    private const string RequiredWireCountCondition = "wire.required_wire_count";
    private const string CircuitCompleteCondition = "wire.circuit_complete";

    public int wiresCount; // wires total
    public GameObject block;
    public GameObject youDidItText;
    [Header("Win Cutscene")]
    [SerializeField] private bool playCircuitCutsceneOnWin = true;
    [SerializeField] private CutsceneDefinition circuitCutsceneDefinition;
    [SerializeField] private CutsceneManager cutsceneManager;
    [SerializeField] private StateChangeCutsceneAnimation circuitCutscene;
    [SerializeField] private Transform circuitCutsceneTargetOverride;
    [SerializeField, Min(0f)] private float circuitCutsceneDelay = 2.5f;

    [Header("Power Dial")]
    [SerializeField] private bool requirePowerDial = true;
    [SerializeField] private bool createPowerDialIfMissing = true;
    [SerializeField] private PowerDialController powerDial;

    [Header("Guidance Highlights")]
    [SerializeField] private bool createGuidanceControllerIfMissing = true;
    [SerializeField] private WireGameGuidanceController guidanceController;

    [Header("Dialogue Conditions")]
    [SerializeField] private bool publishDialogueConditions = true;

    [Header("Dialogue Flow")]
    [SerializeField] private bool createDialogueAdapterIfMissing = true;
    [SerializeField] private WireGameDialogueAdapter dialogueAdapter;

    [HideInInspector] public bool isLocked = true;
    [HideInInspector] public DraggableDevice connectedDevice;

    private int count = 0; // number of wires connected
    private Renderer blockRenderer;
    private bool hasWon;
    private Coroutine pendingCircuitCutsceneRoutine;
    private readonly HashSet<string> startedProgressStages = new HashSet<string>();

    public event Action<DraggableDevice> DeviceConnectedChanged;
    public event Action DeviceDisconnectedChanged;
    public event Action<int, int> WireConnectionCountChanged;
    public event Action<bool> PowerStateChanged;
    public event Action WireInteractionBlocked;
    public event Action PowerDialInteractionBlocked;
    public event Action<DraggableDevice> CircuitCompleted;
    public event Action WinPresentationShown;

    public int ConnectedWireCount => count;
    public bool HasOutputConnected => connectedDevice != null;
    public bool AreAllWiresConnected => wiresCount <= 0 || count >= wiresCount;
    public bool HasWon => hasWon;
    public bool IsPowerOn => IsPowerReady();
    public string CurrentProgressStageId => ResolveSelectedExperimentStage();

    private void Awake()
    {
        Instance = this;
        blockRenderer = block.GetComponent<Renderer>();
        hasWon = false;
        isLocked = true;
    }

    private void Start()
    {
        ResolvePowerDial();
        ResolveGuidanceController();
        ResolveDialogueAdapter();

        if (powerDial != null)
        {
            powerDial.ConfigurePowerOnGate(CanTurnPowerOn, ReportPowerDialInteractionBlocked);
            powerDial.PowerStateChanged += OnPowerStateChanged;
        }

        ClearGuidance();
        PublishDialogueConditions();

        if (dialogueAdapter != null)
            dialogueAdapter.PlayIntroIfNeeded();
    }

    private void OnDestroy()
    {
        if (powerDial != null)
            powerDial.PowerStateChanged -= OnPowerStateChanged;

        if (pendingCircuitCutsceneRoutine != null)
            StopCoroutine(pendingCircuitCutsceneRoutine);
    }

    public void DeviceConnected(DraggableDevice device)
    {
        if (hasWon)
            return;

        isLocked = false;
        connectedDevice = device;
        BeginSelectedExperiment();
        PublishDialogueConditions();
        
        if (WireGameUIManager.Instance != null)
        {
            if (AreAllWiresConnected && IsPowerReady())
            {
                WireGameUIManager.Instance.ClearPrompt();
            }
            else if (AreAllWiresConnected)
            {
                WireGameUIManager.Instance.SetPersistentPrompt("Turn on the power dial to power the circuit.", isWarning: false);
            }
            else
            {
                WireGameUIManager.Instance.SetPersistentPrompt("Connect the wires, then turn on the power dial.", isWarning: false);
            }
        }

        if (AreAllWiresConnected && !IsPowerReady() && guidanceController != null)
            guidanceController.ShowPowerDialGuidance();
        else if (!AreAllWiresConnected && guidanceController != null)
            guidanceController.ShowWireBoardGuidance();
        else
            ClearGuidance();

        DeviceConnectedChanged?.Invoke(device);
        EvaluateWinCondition();
    }

    public void DeviceDisconnected()
    {
        if (hasWon)
            return;

        isLocked = true;
        connectedDevice = null;
        PublishDialogueConditions();

        if (WireGameUIManager.Instance != null)
            WireGameUIManager.Instance.ResetPrompt();

        ClearGuidance();
        DeviceDisconnectedChanged?.Invoke();
    }

    public void UndoSelectedOutput()
    {
        if (hasWon || connectedDevice == null)
            return;

        connectedDevice.ReturnToStart();
    }

    public void LightOn(int points)
    {
        if (hasWon)
            return;

        count += points;  // Simpler than: count = count + points
        PublishDialogueConditions();
        WireConnectionCountChanged?.Invoke(count, wiresCount);
        EvaluateWinCondition();
    }

    public void ReportWireInteractionBlocked()
    {
        if (hasWon)
            return;

        WireInteractionBlocked?.Invoke();
    }

    public void ReportPowerDialInteractionBlocked()
    {
        if (hasWon)
            return;

        if (WireGameUIManager.Instance != null)
        {
            if (!HasOutputConnected)
                WireGameUIManager.Instance.ShowMessage("Place an output device before turning on the power.", isWarning: true);
            else if (!AreAllWiresConnected)
                WireGameUIManager.Instance.ShowMessage("Connect every wire before turning on the power.", isWarning: true);
        }

        if (guidanceController != null)
        {
            if (!HasOutputConnected)
                guidanceController.ShowConnectOutputGuidance();
            else if (!AreAllWiresConnected)
                guidanceController.ShowWireBoardGuidance();
        }

        PowerDialInteractionBlocked?.Invoke();
    }

    private void OnPowerStateChanged(bool isPoweredOn)
    {
        if (hasWon)
            return;

        if (isPoweredOn && !CanTurnPowerOn())
        {
            if (powerDial != null)
                powerDial.ForceOff();

            ReportPowerDialInteractionBlocked();
            return;
        }

        PublishDialogueConditions();

        if (connectedDevice != null && WireGameUIManager.Instance != null)
        {
            if (isPoweredOn)
            {
                WireGameUIManager.Instance.ClearPrompt();
                ClearGuidance();
            }
            else if (AreAllWiresConnected)
            {
                WireGameUIManager.Instance.SetPersistentPrompt("Turn on the power dial to power the circuit.", isWarning: false);
                if (guidanceController != null)
                    guidanceController.ShowPowerDialGuidance();
            }
            else
            {
                WireGameUIManager.Instance.SetPersistentPrompt("Connect the wires, then turn on the power dial.", isWarning: false);
                if (guidanceController != null)
                    guidanceController.ShowWireBoardGuidance();
            }
        }

        PowerStateChanged?.Invoke(isPoweredOn);
        EvaluateWinCondition();
    }

    private void EvaluateWinCondition()
    {
        if (hasWon || !AreAllWiresConnected)
        {
            if (hasWon)
                ClearGuidance();
            return;
        }

        if (!IsPowerReady())
        {
            if (WireGameUIManager.Instance != null)
                WireGameUIManager.Instance.SetPersistentPrompt("Turn on the power dial to power the circuit.", isWarning: true);

            if (guidanceController != null)
                guidanceController.ShowPowerDialGuidance();

            return;
        }

        WinGame();
    }

    private bool IsPowerReady()
    {
        return !requirePowerDial || powerDial == null || powerDial.IsPoweredOn;
    }

    private bool CanTurnPowerOn()
    {
        return !requirePowerDial || (HasOutputConnected && AreAllWiresConnected);
    }

    private void WinGame()
    {
        hasWon = true;
        PublishDialogueConditions();
        LockOutputInteractions();

        if (guidanceController != null)
            guidanceController.ClearGuidance();

        block.SetActive(true);
        blockRenderer.material.color = Color.green;

        if (connectedDevice != null)
        {
            DeviceEffect[] effects = connectedDevice.GetComponentsInChildren<DeviceEffect>();
            foreach (DeviceEffect effect in effects)
            {
                effect.Activate();
            }
        }

        string progressStageId = ResolveSelectedExperimentStage();
        if (!string.IsNullOrEmpty(progressStageId))
            StageProgressService.CompleteStage(StageProgressIds.StateLab, progressStageId);
        else
            Debug.LogWarning("State Lab completed with an output that has no stable progress stage ID.");

        CircuitCompleted?.Invoke(connectedDevice);

        if (playCircuitCutsceneOnWin)
        {
            if (youDidItText != null)
                youDidItText.SetActive(false);

            pendingCircuitCutsceneRoutine = StartCoroutine(PlayCircuitCutsceneAfterDelay());
            return;
        }

        ShowWinPresentation();
    }

    private IEnumerator PlayCircuitCutsceneAfterDelay()
    {
        if (circuitCutsceneDelay > 0f)
            yield return new WaitForSeconds(circuitCutsceneDelay);

        pendingCircuitCutsceneRoutine = null;

        if (!TryPlayCircuitCutscene())
            ShowWinPresentation();
    }

    private bool TryPlayCircuitCutscene()
    {
        if (!playCircuitCutsceneOnWin)
            return false;

        if (!TryResolveCircuitCutsceneKind(out MatterCutsceneKind cutsceneKind))
            return false;

        CutsceneManager manager = ResolveCutsceneManager();
        StateChangeCutsceneAnimation animation = ResolveCircuitCutscene();

        if (manager == null || animation == null)
            return false;

        animation.Configure(cutsceneKind);
        Transform target = ResolveCutsceneTarget();

        if (circuitCutsceneDefinition != null)
            return manager.TryPlay(circuitCutsceneDefinition, target, (ICutsceneAnimation)animation, ShowWinPresentation);

        return manager.TryPlay(target, (ICutsceneAnimation)animation, ShowWinPresentation);
    }

    private void ResolvePowerDial()
    {
        if (!requirePowerDial || powerDial != null)
            return;

        powerDial = FindAnyObjectByType<PowerDialController>();

        if (powerDial == null && createPowerDialIfMissing)
        {
            var powerDialObject = new GameObject("Power Dial Controller");
            powerDial = powerDialObject.AddComponent<PowerDialController>();
        }
    }

    private void ResolveGuidanceController()
    {
        if (guidanceController != null)
            return;

        guidanceController = FindAnyObjectByType<WireGameGuidanceController>();

        if (guidanceController == null && createGuidanceControllerIfMissing)
            guidanceController = gameObject.AddComponent<WireGameGuidanceController>();
    }

    private void ResolveDialogueAdapter()
    {
        if (dialogueAdapter == null)
            dialogueAdapter = FindAnyObjectByType<WireGameDialogueAdapter>(FindObjectsInactive.Include);

        if (dialogueAdapter == null && createDialogueAdapterIfMissing)
            dialogueAdapter = gameObject.AddComponent<WireGameDialogueAdapter>();

        if (dialogueAdapter != null)
            dialogueAdapter.Initialize(this);

        if (guidanceController != null)
            guidanceController.RefreshDialogueRunner();
    }

    private void ClearGuidance()
    {
        if (guidanceController != null)
            guidanceController.ClearGuidance();
    }

    private bool TryResolveCircuitCutsceneKind(out MatterCutsceneKind cutsceneKind)
    {
        cutsceneKind = default;

        if (connectedDevice == null)
            return false;

        if (connectedDevice.GetComponentInChildren<CandleMeltEffect>() != null)
        {
            cutsceneKind = MatterCutsceneKind.CircuitCandleMelting;
            return true;
        }

        if (connectedDevice.GetComponentInChildren<PlasmaEffect>() != null)
        {
            cutsceneKind = MatterCutsceneKind.CircuitPlasmaIonizing;
            return true;
        }

        return false;
    }

    private void BeginSelectedExperiment()
    {
        string stageId = ResolveSelectedExperimentStage();
        if (string.IsNullOrEmpty(stageId))
            return;

        if (!startedProgressStages.Add(stageId))
            return;

        StageProgressService.BeginStage(StageProgressIds.StateLab, stageId);
    }

    private string ResolveSelectedExperimentStage()
    {
        if (connectedDevice == null)
            return null;

        if (connectedDevice.GetComponentInChildren<CandleMeltEffect>() != null)
            return StageProgressIds.MeltWax;

        if (connectedDevice.GetComponentInChildren<PlasmaEffect>() != null)
            return StageProgressIds.IonizeGas;

        return null;
    }

    private Transform ResolveCutsceneTarget()
    {
        if (circuitCutsceneTargetOverride != null)
            return circuitCutsceneTargetOverride;

        if (connectedDevice != null)
            return connectedDevice.transform;

        return block != null ? block.transform : transform;
    }

    private CutsceneManager ResolveCutsceneManager()
    {
        if (cutsceneManager != null)
            return cutsceneManager;

        cutsceneManager = FindAnyObjectByType<CutsceneManager>();

        if (cutsceneManager == null)
            cutsceneManager = gameObject.AddComponent<CutsceneManager>();

        return cutsceneManager;
    }

    private StateChangeCutsceneAnimation ResolveCircuitCutscene()
    {
        if (circuitCutscene != null)
            return circuitCutscene;

        circuitCutscene = GetComponent<StateChangeCutsceneAnimation>();

        if (circuitCutscene == null)
            circuitCutscene = gameObject.AddComponent<StateChangeCutsceneAnimation>();

        return circuitCutscene;
    }

    private void ShowWinPresentation()
    {
        // Show congratulations message
        if (WireGameUIManager.Instance != null)
        {
            WireGameUIManager.Instance.SetPersistentPrompt("Congratulations! You completed the wire game!", isWarning: false);
        }
        
        if (youDidItText != null)
            youDidItText.SetActive(true);

        WinPresentationShown?.Invoke();
    }

    private void LockOutputInteractions()
    {
        DraggableDevice[] devices = FindObjectsByType<DraggableDevice>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < devices.Length; i++)
            devices[i].LockInteraction();
    }

    private void PublishDialogueConditions()
    {
        if (!publishDialogueConditions)
            return;

        DialogueConditionState.SetBool(OutputConnectedCondition, connectedDevice != null);
        DialogueConditionState.SetBool(PowerOnCondition, IsPowerReady());
        DialogueConditionState.SetNumber(ConnectedWireCountCondition, count);
        DialogueConditionState.SetNumber(RequiredWireCountCondition, wiresCount);
        DialogueConditionState.SetBool(CircuitCompleteCondition, hasWon);
    }
}
