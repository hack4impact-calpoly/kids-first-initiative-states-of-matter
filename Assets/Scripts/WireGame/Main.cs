using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Main : MonoBehaviour
{
    static public Main Instance;

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

    [HideInInspector] public bool isLocked = true;
    [HideInInspector] public DraggableDevice connectedDevice;

    private int count = 0; // number of wires connected
    private Renderer blockRenderer;
    private bool hasWon;
    private Coroutine pendingCircuitCutsceneRoutine;

    private void Awake()
    {
        Instance = this;
        blockRenderer = block.GetComponent<Renderer>();
        hasWon = false;
        
        // Check which scene we're in
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Wire")
        {
            // Scene without outputs - unlock wires immediately
            isLocked = false;
        }
        else
        {
            // Scene with outputs - keep wires locked until device connected
            isLocked = true;
        }
    }

    private void Start()
    {
        ResolvePowerDial();
        ResolveGuidanceController();

        if (powerDial != null)
            powerDial.PowerStateChanged += OnPowerStateChanged;

        ClearGuidance();
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
        isLocked = false;
        connectedDevice = device;
        
        // Clear the prompt when device is connected
        if (WireGameUIManager.Instance != null)
        {
            if (IsPowerReady())
                WireGameUIManager.Instance.ClearPrompt();
            else
                WireGameUIManager.Instance.SetPersistentPrompt("Connect the wires, then turn on the power dial.", isWarning: false);
        }

        ClearGuidance();
        EvaluateWinCondition();
    }

    public void DeviceDisconnected()
    {
        isLocked = true;
        connectedDevice = null;

        if (WireGameUIManager.Instance != null)
            WireGameUIManager.Instance.ResetPrompt();

        ClearGuidance();
    }

    public void LightOn(int points)
    {
        if (hasWon)
            return;

        count += points;  // Simpler than: count = count + points
        EvaluateWinCondition();
    }

    private void OnPowerStateChanged(bool isPoweredOn)
    {
        if (hasWon)
            return;

        if (connectedDevice != null && WireGameUIManager.Instance != null)
        {
            if (isPoweredOn)
            {
                WireGameUIManager.Instance.SetPersistentPrompt("Power dial is on. Complete the wire connections.", isWarning: false);
                if (count < wiresCount && guidanceController != null)
                    guidanceController.ShowWireBoardGuidance();
            }
            else
            {
                WireGameUIManager.Instance.SetPersistentPrompt("Connect the wires, then turn on the power dial.", isWarning: false);
                ClearGuidance();
            }
        }

        EvaluateWinCondition();
    }

    private void EvaluateWinCondition()
    {
        if (hasWon || count < wiresCount)
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

    private void WinGame()
    {
        hasWon = true;
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

        CutsceneManager manager = ResolveCutsceneManager();
        StateChangeCutsceneAnimation animation = ResolveCircuitCutscene();

        if (manager == null || animation == null)
            return false;

        animation.Configure(ResolveCircuitCutsceneKind());
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

    private void ClearGuidance()
    {
        if (guidanceController != null)
            guidanceController.ClearGuidance();
    }

    private MatterCutsceneKind ResolveCircuitCutsceneKind()
    {
        if (connectedDevice == null)
            return MatterCutsceneKind.CircuitEnergy;

        if (connectedDevice.GetComponentInChildren<CandleMeltEffect>() != null)
            return MatterCutsceneKind.CircuitCandleMelting;

        if (connectedDevice.GetComponentInChildren<PlasmaEffect>() != null)
            return MatterCutsceneKind.CircuitPlasmaIonizing;

        return MatterCutsceneKind.CircuitEnergy;
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
    }
}
