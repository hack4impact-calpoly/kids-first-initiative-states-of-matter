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

    [HideInInspector] public bool isLocked = true;
    [HideInInspector] public DraggableDevice connectedDevice;

    private int count = 0; // number of wires connected
    private Renderer blockRenderer;
    private bool hasWon;

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

    public void DeviceConnected(DraggableDevice device)
    {
        isLocked = false;
        connectedDevice = device;
        
        // Clear the prompt when device is connected
        if (WireGameUIManager.Instance != null)
        {
            WireGameUIManager.Instance.ClearPrompt();
        }
    }

    public void DeviceDisconnected()
    {
        isLocked = true;
        connectedDevice = null;
    }

    public void LightOn(int points)
    {
        if (hasWon)
            return;

        count += points;  // Simpler than: count = count + points
        
        if (count >= wiresCount)
        {
            WinGame();
        }
    }

    void WinGame()
    {
        hasWon = true;
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

        if (TryPlayCircuitCutscene())
        {
            if (youDidItText != null)
                youDidItText.SetActive(false);

            return;
        }

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

        animation.Configure(MatterCutsceneKind.CircuitEnergy);
        Transform target = ResolveCutsceneTarget();

        if (circuitCutsceneDefinition != null)
            return manager.TryPlay(circuitCutsceneDefinition, target, (ICutsceneAnimation)animation, ShowWinPresentation);

        return manager.TryPlay(target, (ICutsceneAnimation)animation, ShowWinPresentation);
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
            WireGameUIManager.Instance.ShowMessage("Congratulations! You completed the wire game!", isWarning: false);
        }
        
        if (youDidItText != null)
            youDidItText.SetActive(true);
    }
}
