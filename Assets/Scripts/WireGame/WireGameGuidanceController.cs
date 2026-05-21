using System.Collections.Generic;
using UnityEngine;

public class WireGameGuidanceController : MonoBehaviour
{
    public static WireGameGuidanceController Instance { get; private set; }

    [SerializeField] private bool autoDiscoverTargets = true;
    [SerializeField] private List<AttentionHighlight> outputHighlights = new List<AttentionHighlight>();
    [SerializeField] private List<AttentionHighlight> wireBoardHighlights = new List<AttentionHighlight>();
    [SerializeField] private List<AttentionHighlight> powerDialHighlights = new List<AttentionHighlight>();

    [Header("Dialogue Line Tags")]
    [SerializeField] private bool followDialogueLineTags = true;
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private string[] outputGuidanceTags = { "wire.guidance.output", "guidance.output" };
    [SerializeField] private string[] wireBoardGuidanceTags = { "wire.guidance.wires", "guidance.wires" };
    [SerializeField] private string[] powerDialGuidanceTags = { "wire.guidance.power", "guidance.power" };
    [SerializeField] private string[] clearGuidanceTags = { "wire.guidance.clear", "guidance.clear" };

    private bool hasDiscoveredTargets;
    private DialogueRunner subscribedDialogueRunner;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        SubscribeToDialogueRunner();
    }

    private void OnDisable()
    {
        UnsubscribeFromDialogueRunner();
    }

    public void ShowConnectOutputGuidance()
    {
        EnsureTargets();
        ShowOnly(outputHighlights);
    }

    public void ShowWireBoardGuidance()
    {
        EnsureTargets();
        ShowOnly(wireBoardHighlights);
    }

    public void ShowPowerDialGuidance()
    {
        EnsureTargets();
        ShowOnly(powerDialHighlights);
    }

    public void ClearGuidance()
    {
        EnsureTargets();
        HideAll(outputHighlights);
        HideAll(wireBoardHighlights);
        HideAll(powerDialHighlights);
    }

    public void RefreshTargets()
    {
        hasDiscoveredTargets = false;
        EnsureTargets();
    }

    public void RefreshDialogueRunner()
    {
        SubscribeToDialogueRunner();
    }

    private void SubscribeToDialogueRunner()
    {
        UnsubscribeFromDialogueRunner();

        if (!followDialogueLineTags)
            return;

        if (dialogueRunner == null)
            dialogueRunner = FindAnyObjectByType<DialogueRunner>(FindObjectsInactive.Include);

        if (dialogueRunner == null)
            return;

        dialogueRunner.LineChanged += OnDialogueLineChanged;
        subscribedDialogueRunner = dialogueRunner;
    }

    private void UnsubscribeFromDialogueRunner()
    {
        if (subscribedDialogueRunner == null)
            return;

        subscribedDialogueRunner.LineChanged -= OnDialogueLineChanged;
        subscribedDialogueRunner = null;
    }

    private void OnDialogueLineChanged(DialogueLine line)
    {
        if (!followDialogueLineTags || line == null)
            return;

        if (HasAnyTag(line, clearGuidanceTags))
        {
            ClearGuidance();
            return;
        }

        if (HasAnyTag(line, outputGuidanceTags))
        {
            ShowConnectOutputGuidance();
            return;
        }

        if (HasAnyTag(line, wireBoardGuidanceTags))
        {
            ShowWireBoardGuidance();
            return;
        }

        if (HasAnyTag(line, powerDialGuidanceTags))
            ShowPowerDialGuidance();
    }

    private static bool HasAnyTag(DialogueLine line, IReadOnlyList<string> tags)
    {
        if (line == null || tags == null)
            return false;

        for (int i = 0; i < tags.Count; i++)
        {
            if (line.HasTag(tags[i]))
                return true;
        }

        return false;
    }

    private void EnsureTargets()
    {
        if (hasDiscoveredTargets || !autoDiscoverTargets)
            return;

        AddOutputHighlights();
        AddWireBoardHighlights();
        AddPowerDialHighlights();
        hasDiscoveredTargets = true;
    }

    private void AddOutputHighlights()
    {
        DraggableDevice[] devices = FindObjectsByType<DraggableDevice>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < devices.Length; i++)
            AddHighlight(outputHighlights, devices[i].gameObject);
    }

    private void AddWireBoardHighlights()
    {
        AddHighlight(wireBoardHighlights, GameObject.Find("Left"));
        AddHighlight(wireBoardHighlights, GameObject.Find("Right"));
    }

    private void AddPowerDialHighlights()
    {
        PowerDialController powerDial = FindAnyObjectByType<PowerDialController>();
        if (powerDial != null && powerDial.GuidanceHighlight != null)
            AddHighlight(powerDialHighlights, powerDial.GuidanceHighlight);
    }

    private void AddHighlight(List<AttentionHighlight> highlights, GameObject target)
    {
        if (target == null)
            return;

        AttentionHighlight highlight = target.GetComponent<AttentionHighlight>();
        if (highlight == null)
            highlight = target.AddComponent<AttentionHighlight>();

        AddHighlight(highlights, highlight);
    }

    private void AddHighlight(List<AttentionHighlight> highlights, AttentionHighlight highlight)
    {
        if (highlight == null || highlights.Contains(highlight))
            return;

        highlights.Add(highlight);
    }

    private void ShowOnly(List<AttentionHighlight> activeHighlights)
    {
        HideAll(outputHighlights);
        HideAll(wireBoardHighlights);
        HideAll(powerDialHighlights);
        ShowAll(activeHighlights);
    }

    private void ShowAll(List<AttentionHighlight> highlights)
    {
        for (int i = 0; i < highlights.Count; i++)
        {
            if (highlights[i] != null)
                highlights[i].Show();
        }
    }

    private void HideAll(List<AttentionHighlight> highlights)
    {
        for (int i = 0; i < highlights.Count; i++)
        {
            if (highlights[i] != null)
                highlights[i].Hide();
        }
    }
}
