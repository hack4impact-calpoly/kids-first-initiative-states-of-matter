using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ActivityFlowController : MonoBehaviour, IFlowSceneController
{
    private string sceneName;
    private string activityId;
    private string stageId;
    private Canvas shellCanvas;
    private TextMeshProUGUI objectiveText;
    private AttentionHighlight currentHighlight;
    private ChildVisualGuide visualGuide;
    private Button restartButton;
    private Button resultPrimaryButton;
    private bool resultScheduled;

    private KitchenGameManager solidManager;
    private JuiceFreezingManager pourManager;
    private JuicePouringGameManager stationManager;
    private StartButtonHandler pipeStartHandler;
    private Main labManager;
    private bool initialized;

    public void InitializeFlow()
    {
        if (initialized)
            return;

        initialized = true;
        sceneName = SceneManager.GetActiveScene().name;
        activityId = ActivityFlowCatalog.GetActivityForScene(sceneName);
        stageId = ActivityFlowCatalog.GetStageForScene(sceneName);

        HideLegacyControls();
        BuildShell();
        ConfigureSceneFlow();
    }

    private void OnDestroy()
    {
        UnsubscribeFromSceneFlow();
    }

    private void BuildShell()
    {
        shellCanvas = FlowUiFactory.CreateCanvas("Activity Shell Canvas", 600);
        shellCanvas.transform.SetParent(transform, false);
        visualGuide = gameObject.AddComponent<ChildVisualGuide>();

        Image header = FlowUiFactory.CreatePanel(
            shellCanvas.transform,
            "Activity Header",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, -88f),
            Vector2.zero,
            FlowUiFactory.Navy);

        Button activities = FlowUiFactory.CreateButton(
            header.transform,
            "Activities",
            "ACTIVITIES",
            FlowUiFactory.Blue,
            OpenActivities);
        FlowUiFactory.SetRect(activities, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(16f, 14f), new Vector2(190f, -14f));

        objectiveText = FlowUiFactory.CreateText(
            header.transform,
            "Objective",
            string.Empty,
            27f,
            TextAlignmentOptions.Center,
            FlowUiFactory.White);
        objectiveText.fontStyle = FontStyles.Bold;
        objectiveText.rectTransform.offsetMin = new Vector2(215f, 12f);
        objectiveText.rectTransform.offsetMax = new Vector2(-430f, -12f);

        if (!string.IsNullOrEmpty(activityId))
            FlowUiFactory.AddProgressDots(header.transform, activityId, FlowUiFactory.Gold);

        Button hint = FlowUiFactory.CreateButton(header.transform, "Hint", "HINT", FlowUiFactory.Green, ReplayHint);
        FlowUiFactory.SetRect(hint, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-410f, 14f), new Vector2(-284f, -14f));

        restartButton = FlowUiFactory.CreateButton(header.transform, "Restart", "RESTART", FlowUiFactory.Orange, RestartStage);
        FlowUiFactory.SetRect(restartButton, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-270f, 14f), new Vector2(-126f, -14f));

        if (sceneName == ActivityFlowCatalog.LabScene)
        {
            Button undo = FlowUiFactory.CreateButton(header.transform, "Undo", "UNDO", FlowUiFactory.Purple, UndoLabOutput);
            FlowUiFactory.SetRect(undo, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-116f, 14f), new Vector2(-16f, -14f));
        }
    }

    private void ConfigureSceneFlow()
    {
        switch (sceneName)
        {
            case ActivityFlowCatalog.KitchenSolidScene:
                ConfigureSolidKitchen();
                break;
            case ActivityFlowCatalog.KitchenPourScene:
                ConfigurePourKitchen();
                break;
            case ActivityFlowCatalog.KitchenFreezeScene:
                ConfigureFreezeKitchen();
                break;
            case ActivityFlowCatalog.PipeScene:
                ConfigurePipes();
                break;
            case ActivityFlowCatalog.LabScene:
                ConfigureLab();
                break;
        }
    }

    private void ConfigureSolidKitchen()
    {
        solidManager = FindAnyObjectByType<KitchenGameManager>();
        if (solidManager != null)
        {
            solidManager.RequiredIngredientAdded += OnSolidIngredientAdded;
            solidManager.MaxHeatReached += OnSolidHeatReached;
            solidManager.Failed += OnSolidFailed;
            solidManager.WinPresentationShown += OnSolidCompleted;
        }

        UpdateObjective("Drag the solid chocolate into the pot.");
        IngredientBarDragToWorld2D ingredient = FindAnyObjectByType<IngredientBarDragToWorld2D>();
        MockPotController pot = FindAnyObjectByType<MockPotController>();
        ShowDragHighlight(
            ingredient != null ? ingredient.gameObject : null,
            pot != null ? pot.gameObject : null,
            "DRAG",
            "DROP HERE");
    }

    private void ConfigurePourKitchen()
    {
        pourManager = FindAnyObjectByType<JuiceFreezingManager>();
        if (pourManager != null)
        {
            pourManager.TrayFillStarted += OnTrayFillStarted;
            pourManager.PourStepCompleted += OnPourStepCompleted;
            pourManager.WinPresentationShown += OnPourCompleted;
        }

        UpdateObjective("Drag and tilt the juice bottle over the tray.");
        JuicePouring bottle = FindJuiceBottle();
        IceTray tray = FindAnyObjectByType<IceTray>();
        ShowDragHighlight(
            bottle != null ? bottle.gameObject : null,
            tray != null ? tray.gameObject : null,
            "DRAG & TILT",
            "POUR HERE");
    }

    private void ConfigureFreezeKitchen()
    {
        stationManager = FindAnyObjectByType<JuicePouringGameManager>();
        if (stationManager != null)
        {
            stationManager.IngredientAddedToFreezer += OnTrayPlacedInFreezer;
            stationManager.ColdEnoughReached += OnColdEnough;
            stationManager.FreezingCompleted += OnFreezeCompleted;
        }

        UpdateObjective("Place the filled tray in the freezer.");
        IngredientBarDragToWorld2D ingredient = FindAnyObjectByType<IngredientBarDragToWorld2D>();
        MockPotController freezer = FindAnyObjectByType<MockPotController>();
        ShowDragHighlight(
            ingredient != null ? ingredient.gameObject : null,
            freezer != null ? freezer.gameObject : null,
            "DRAG",
            "DROP HERE");
    }

    private void ConfigurePipes()
    {
        pipeStartHandler = FindAnyObjectByType<StartButtonHandler>();
        if (pipeStartHandler != null)
        {
            pipeStartHandler.StartPressed += OnPipeTestStarted;
            pipeStartHandler.ValidationFailed += OnPipeTestFailed;
            pipeStartHandler.ValidationSucceeded += OnPipeCompleted;
            RenamePipeTestButton(pipeStartHandler.startButton);
        }

        FreezeOnClick.PipeFreezeToggled += OnPipeFreezeToggled;
        UpdateObjective("Freeze each leaking pipe with solid ice.");
        ShowHighlight(FindNextUnfrozenSink(), "TAP");
    }

    private void ConfigureLab()
    {
        labManager = Main.Instance != null ? Main.Instance : FindAnyObjectByType<Main>();
        if (labManager != null)
        {
            labManager.DeviceConnectedChanged += OnLabDeviceConnected;
            labManager.DeviceDisconnectedChanged += OnLabDeviceDisconnected;
            labManager.WireConnectionCountChanged += OnLabWireCountChanged;
            labManager.PowerStateChanged += OnLabPowerChanged;
            labManager.WinPresentationShown += OnLabCompleted;
        }

        UpdateObjective("Drag a device into the dashed experiment station.");
        ShowLabDeviceGuide();
    }

    private void UnsubscribeFromSceneFlow()
    {
        if (solidManager != null)
        {
            solidManager.RequiredIngredientAdded -= OnSolidIngredientAdded;
            solidManager.MaxHeatReached -= OnSolidHeatReached;
            solidManager.Failed -= OnSolidFailed;
            solidManager.WinPresentationShown -= OnSolidCompleted;
        }

        if (pourManager != null)
        {
            pourManager.TrayFillStarted -= OnTrayFillStarted;
            pourManager.PourStepCompleted -= OnPourStepCompleted;
            pourManager.WinPresentationShown -= OnPourCompleted;
        }

        if (stationManager != null)
        {
            stationManager.IngredientAddedToFreezer -= OnTrayPlacedInFreezer;
            stationManager.ColdEnoughReached -= OnColdEnough;
            stationManager.FreezingCompleted -= OnFreezeCompleted;
        }

        if (pipeStartHandler != null)
        {
            pipeStartHandler.StartPressed -= OnPipeTestStarted;
            pipeStartHandler.ValidationFailed -= OnPipeTestFailed;
            pipeStartHandler.ValidationSucceeded -= OnPipeCompleted;
        }

        FreezeOnClick.PipeFreezeToggled -= OnPipeFreezeToggled;

        if (labManager != null)
        {
            labManager.DeviceConnectedChanged -= OnLabDeviceConnected;
            labManager.DeviceDisconnectedChanged -= OnLabDeviceDisconnected;
            labManager.WireConnectionCountChanged -= OnLabWireCountChanged;
            labManager.PowerStateChanged -= OnLabPowerChanged;
            labManager.WinPresentationShown -= OnLabCompleted;
        }
    }

    private void OnSolidIngredientAdded(IngredientSO ingredient)
    {
        HideHighlight();
        UpdateObjective("Slide the heat control right to melt the chocolate.");
        HeatController heat = FindAnyObjectByType<HeatController>();
        ShowHighlight(heat != null ? heat.gameObject : null, "SLIDE RIGHT");
    }

    private void OnSolidHeatReached(float heat)
    {
        HideHighlight();
        UpdateObjective("Watch heat change the solid into a liquid.");
    }

    private void OnSolidFailed()
    {
        UpdateObjective("Restart, then add the chocolate before turning up the heat.");
        ShowHighlight(restartButton != null ? restartButton.gameObject : null, "PRESS");
    }

    private void OnSolidCompleted()
    {
        ScheduleResult(StageProgressIds.MatterKitchen, StageProgressIds.MeltChocolate);
    }

    private void OnTrayFillStarted()
    {
        UpdateObjective("Keep pouring until the liquid fills the tray.");
    }

    private void OnPourStepCompleted()
    {
        HideHighlight();
        UpdateObjective("Watch the liquid settle into the tray.");
    }

    private void OnPourCompleted()
    {
        ScheduleResult(StageProgressIds.MatterKitchen, StageProgressIds.PourJuice);
    }

    private void OnTrayPlacedInFreezer(IngredientSO ingredient)
    {
        HideHighlight();
        UpdateObjective("Lower the temperature until the juice freezes.");
        JuiceCoolingController cooling = FindAnyObjectByType<JuiceCoolingController>();
        ShowHighlight(cooling != null ? cooling.gameObject : null, "SLIDE DOWN");
    }

    private void OnColdEnough()
    {
        HideHighlight();
        UpdateObjective("Watch the liquid lock into a solid.");
    }

    private void OnFreezeCompleted()
    {
        ScheduleResult(StageProgressIds.MatterKitchen, StageProgressIds.FreezeJuice);
    }

    private void OnPipeFreezeToggled(PipeObject pipe)
    {
        HideHighlight();

        if (pipe != null && pipe.isFrozen && !pipe.isSink)
        {
            UpdateObjective("That blocks the route. Click it again to unfreeze it.");
            ShowHighlight(pipe.gameObject, "TAP AGAIN");
            return;
        }

        GameObject nextLeak = FindNextUnfrozenSink();
        if (nextLeak != null)
        {
            string objective = "Good plug. Freeze the next leaking pipe.";
            if (pipe != null && !pipe.isFrozen)
            {
                objective = pipe.isSink
                    ? "That leak is open again. Freeze the highlighted pipe."
                    : "The route is open again. Freeze the highlighted leak.";
            }

            UpdateObjective(objective);
            ShowHighlight(nextLeak, "TAP");
            return;
        }

        UpdateObjective("All leaks are plugged. Press Test Route.");
        if (pipeStartHandler != null)
            ShowHighlight(pipeStartHandler.startButton, "PRESS");
    }

    private void OnPipeTestStarted()
    {
        HideHighlight();
        UpdateObjective("Watch where the liquid flows or leaks.");
    }

    private void OnPipeTestFailed()
    {
        GameObject nextLeak = FindNextUnfrozenSink();
        if (nextLeak != null)
        {
            UpdateObjective("Water still leaks. Freeze the highlighted pipe.");
            ShowHighlight(nextLeak, "TAP");
            return;
        }

        GameObject blockedRoute = FindFrozenRoutePipe();
        UpdateObjective(blockedRoute != null
            ? "The route is blocked. Unfreeze the highlighted pipe."
            : "A connection is still open. Check the route, then test again.");
        ShowHighlight(blockedRoute, blockedRoute != null ? "TAP AGAIN" : "CHECK");
    }

    private void OnPipeCompleted(MatterCutsceneKind kind)
    {
        ScheduleResult(StageProgressIds.PipeRescue, StageProgressIds.FreezePipeLeak);
    }

    private void OnLabDeviceConnected(DraggableDevice device)
    {
        UpdateObjective("Match all four wire pairs (0 / " + labManager.wiresCount + ").");
        ShowNextWireGuide();
    }

    private void OnLabDeviceDisconnected()
    {
        UpdateObjective("Drag a device into the dashed experiment station.");
        ShowLabDeviceGuide();
    }

    private void OnLabWireCountChanged(int connected, int required)
    {
        UpdateObjective(connected >= required
            ? "All wires connected. Raise the POWER control."
            : "Match all four wire pairs (" + connected + " / " + required + ").");

        if (connected >= required)
        {
            ShowPowerGuide();
            return;
        }

        StartCoroutine(ShowNextWireGuideNextFrame());
    }

    private void OnLabPowerChanged(bool powered)
    {
        if (powered)
        {
            HideHighlight();
            UpdateObjective("Watch electrical energy change the matter.");
        }
        else if (labManager != null && labManager.AreAllWiresConnected)
        {
            ShowPowerGuide();
        }
    }

    private void OnLabCompleted()
    {
        string completedStage = labManager != null ? labManager.CurrentProgressStageId : null;
        if (string.IsNullOrEmpty(completedStage))
            completedStage = StageProgressIds.MeltWax;

        ScheduleResult(StageProgressIds.StateLab, completedStage);
    }

    private void ScheduleResult(string completedActivityId, string completedStageId)
    {
        if (resultScheduled)
            return;

        resultScheduled = true;
        HideHighlight();
        StartCoroutine(ShowResultWhenReady(completedActivityId, completedStageId));
    }

    private IEnumerator ShowResultWhenReady(string completedActivityId, string completedStageId)
    {
        yield return null;
        yield return DialogueWaitUtility.WaitUntilIdle();
        ShowResult(completedActivityId, completedStageId);
    }

    private void ShowResult(string completedActivityId, string completedStageId)
    {
        Canvas resultCanvas = FlowUiFactory.CreateCanvas("Activity Result Canvas", 32000);
        resultCanvas.transform.SetParent(transform, false);

        Image backdrop = FlowUiFactory.CreatePanel(
            resultCanvas.transform,
            "Backdrop",
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            new Color(0f, 0.03f, 0.08f, 0.76f));
        backdrop.raycastTarget = true;

        Image panel = FlowUiFactory.CreatePanel(
            resultCanvas.transform,
            "Result Panel",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-450f, -260f),
            new Vector2(450f, 260f),
            new Color32(247, 251, 255, 255));

        TextMeshProUGUI title = FlowUiFactory.CreateText(
            panel.transform,
            "Title",
            ActivityFlowCatalog.GetStageTitle(completedActivityId, completedStageId),
            52f,
            TextAlignmentOptions.Center,
            FlowUiFactory.Ink);
        title.rectTransform.offsetMin = new Vector2(55f, 330f);
        title.rectTransform.offsetMax = new Vector2(-55f, -42f);

        TextMeshProUGUI recap = FlowUiFactory.CreateText(
            panel.transform,
            "Recap",
            ActivityFlowCatalog.GetStageRecap(completedActivityId, completedStageId),
            31f,
            TextAlignmentOptions.Center,
            new Color32(52, 74, 99, 255));
        recap.fontStyle = FontStyles.Normal;
        recap.rectTransform.offsetMin = new Vector2(80f, 170f);
        recap.rectTransform.offsetMax = new Vector2(-80f, -165f);

        if (StageProgressService.CanReportGameCompletion())
        {
            resultPrimaryButton = FlowUiFactory.CreateButton(
                panel.transform,
                "Primary Action",
                "TAKE THE QUIZ",
                FlowUiFactory.Green,
                FinishGame);
        }
        else
        {
            ResolvePrimaryAction(completedActivityId, completedStageId, out string primaryLabel, out string primaryScene);
            resultPrimaryButton = FlowUiFactory.CreateButton(
                panel.transform,
                "Primary Action",
                primaryLabel,
                FlowUiFactory.Green,
                () => SceneManager.LoadScene(primaryScene));
        }

        FlowUiFactory.SetRect(resultPrimaryButton, new Vector2(0f, 0f), new Vector2(0.62f, 0f), new Vector2(55f, 44f), new Vector2(-12f, 130f));

        Button activities = FlowUiFactory.CreateButton(
            panel.transform,
            "Activities",
            "ACTIVITIES",
            FlowUiFactory.Blue,
            OpenActivities);
        FlowUiFactory.SetRect(activities, new Vector2(0.62f, 0f), new Vector2(1f, 0f), new Vector2(12f, 44f), new Vector2(-55f, 130f));
    }

    private void FinishGame()
    {
        if (!StageProgressService.ReportGameCompletion())
            return;

        if (resultPrimaryButton == null)
            return;

        resultPrimaryButton.interactable = false;
        TextMeshProUGUI label = resultPrimaryButton.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = "OPENING QUIZ...";
    }

    private static void ResolvePrimaryAction(
        string completedActivityId,
        string completedStageId,
        out string label,
        out string scene)
    {
        if (StageProgressService.IsGameComplete())
        {
            label = "CONTINUE EXPLORING";
            scene = ActivityFlowCatalog.SelectorScene;
            return;
        }

        string nextStageScene = ActivityFlowCatalog.GetNextStageScene(completedActivityId, completedStageId);
        if (!string.IsNullOrEmpty(nextStageScene))
        {
            label = "CONTINUE";
            scene = nextStageScene;
            return;
        }

        if (completedActivityId == StageProgressIds.StateLab
            && !ActivityFlowCatalog.IsActivityComplete(StageProgressIds.StateLab))
        {
            label = "TRY ANOTHER EXPERIMENT";
            scene = ActivityFlowCatalog.LabScene;
            return;
        }

        string nextActivity = ActivityFlowCatalog.GetNextActivity(completedActivityId);
        if (!string.IsNullOrEmpty(nextActivity))
        {
            label = "NEXT ACTIVITY";
            scene = ActivityFlowCatalog.GetEntryScene(nextActivity);
            return;
        }

        label = "CONTINUE EXPLORING";
        scene = ActivityFlowCatalog.SelectorScene;
    }

    private void ReplayHint()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IActivityHintProvider hintProvider)
            {
                hintProvider.ReplayCurrentInstruction();
                break;
            }
        }

        if (visualGuide != null)
            visualGuide.Emphasize();
    }

    private void ShowHighlight(GameObject target, string actionLabel = "TAP")
    {
        HideHighlight();
        if (target == null)
            return;

        currentHighlight = target.GetComponent<AttentionHighlight>();
        if (currentHighlight == null)
            currentHighlight = target.AddComponent<AttentionHighlight>();
        currentHighlight.Show();

        if (visualGuide != null)
            visualGuide.ShowAction(target, actionLabel);
    }

    private void ShowDragHighlight(
        GameObject source,
        GameObject destination,
        string actionLabel,
        string destinationLabel)
    {
        HideHighlight();

        if (source != null)
        {
            currentHighlight = source.GetComponent<AttentionHighlight>();
            if (currentHighlight == null)
                currentHighlight = source.AddComponent<AttentionHighlight>();
            currentHighlight.Show();
        }

        if (visualGuide != null)
            visualGuide.ShowDrag(source, destination, actionLabel, destinationLabel);
    }

    private void ShowChoiceHighlight(
        GameObject[] sources,
        GameObject destination,
        string actionLabel,
        string destinationLabel)
    {
        HideHighlight();

        if (sources != null && sources.Length > 0 && sources[0] != null)
        {
            currentHighlight = sources[0].GetComponent<AttentionHighlight>();
            if (currentHighlight == null)
                currentHighlight = sources[0].AddComponent<AttentionHighlight>();
            currentHighlight.Show();
        }

        if (visualGuide != null)
        {
            visualGuide.ShowChoices(
                sources,
                destination,
                actionLabel,
                destinationLabel,
                false);
        }
    }

    private void HideHighlight()
    {
        if (currentHighlight != null)
            currentHighlight.Hide();
        currentHighlight = null;

        if (visualGuide != null)
            visualGuide.Hide();
    }

    private void UpdateObjective(string objective)
    {
        if (objectiveText != null)
            objectiveText.text = objective;
    }

    private static GameObject FindNextUnfrozenSink()
    {
        PipeObject[] pipes = FindObjectsByType<PipeObject>(FindObjectsSortMode.None);
        PipeObject next = null;
        int bestDistance = int.MaxValue;
        for (int i = 0; i < pipes.Length; i++)
        {
            PipeObject pipe = pipes[i];
            if (!pipe.isSink || pipe.isFrozen)
                continue;

            int distanceFromSource = Mathf.Abs(pipe.xPos - 1) + Mathf.Abs(pipe.yPos - 4);
            if (distanceFromSource < bestDistance)
            {
                bestDistance = distanceFromSource;
                next = pipe;
            }
        }

        return next != null ? next.gameObject : null;
    }

    private static GameObject FindFrozenRoutePipe()
    {
        PipeObject[] pipes = FindObjectsByType<PipeObject>(FindObjectsSortMode.None);
        for (int i = 0; i < pipes.Length; i++)
        {
            if (pipes[i].isFrozen && !pipes[i].isSink)
                return pipes[i].gameObject;
        }

        return null;
    }

    private static JuicePouring FindJuiceBottle()
    {
        JuicePouring[] pourers = FindObjectsByType<JuicePouring>(FindObjectsSortMode.None);
        JuicePouring best = null;
        float bestArea = float.MaxValue;
        for (int i = 0; i < pourers.Length; i++)
        {
            RectTransform rect = pourers[i].transform as RectTransform;
            if (rect == null)
                continue;

            float area = Mathf.Abs(rect.rect.width * rect.rect.height);
            if (area < bestArea)
            {
                best = pourers[i];
                bestArea = area;
            }
        }

        return best;
    }

    private void ShowLabDeviceGuide()
    {
        DraggableDevice[] devices = FindObjectsByType<DraggableDevice>(FindObjectsSortMode.None);
        SnapZone zone = FindAnyObjectByType<SnapZone>();
        GameObject[] targets = new GameObject[Mathf.Min(devices.Length, 2)];
        for (int i = 0; i < targets.Length; i++)
            targets[i] = devices[i].gameObject;

        ShowChoiceHighlight(
            targets,
            zone != null ? zone.gameObject : null,
            "DRAG ONE",
            "DROP HERE");
    }

    private IEnumerator ShowNextWireGuideNextFrame()
    {
        yield return null;
        ShowNextWireGuide();
    }

    private void ShowNextWireGuide()
    {
        Wire[] wires = FindObjectsByType<Wire>(FindObjectsSortMode.None);
        for (int i = 0; i < wires.Length; i++)
        {
            if (wires[i] == null || wires[i].transform.parent == null)
                continue;

            for (int j = i + 1; j < wires.Length; j++)
            {
                if (wires[j] == null || wires[j].transform.parent == null)
                    continue;

                if (wires[i].transform.parent.name != wires[j].transform.parent.name)
                    continue;

                ShowDragHighlight(
                    wires[i].gameObject,
                    wires[j].gameObject,
                    "DRAG",
                    "MATCH COLOR");
                return;
            }
        }

        ShowPowerGuide();
    }

    private void ShowPowerGuide()
    {
        PowerDialController power = FindAnyObjectByType<PowerDialController>();
        AttentionHighlight highlight = power != null ? power.GuidanceHighlight : null;
        ShowHighlight(highlight != null ? highlight.gameObject : null, "SLIDE UP");
    }

    private static void RenamePipeTestButton(GameObject startButton)
    {
        if (startButton == null)
            return;

        RectTransform buttonRect = startButton.GetComponent<RectTransform>();
        if (buttonRect != null)
            buttonRect.sizeDelta = new Vector2(200f, 50f);

        Image image = startButton.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = FlowUiFactory.GetButtonSprite();
            image.type = Image.Type.Sliced;
            image.color = FlowUiFactory.Blue;
        }

        Button button = startButton.GetComponent<Button>();
        if (button != null && image != null)
            button.targetGraphic = image;

        Outline outline = startButton.GetComponent<Outline>();
        if (outline == null)
            outline = startButton.AddComponent<Outline>();
        outline.effectColor = new Color32(8, 48, 103, 220);
        outline.effectDistance = new Vector2(3f, -3f);

        TextMeshProUGUI text = startButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            text.text = "TEST ROUTE";
            text.font = TMP_Settings.defaultFontAsset;
            text.fontMaterial = new Material(text.fontSharedMaterial)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            text.fontSize = 24f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 15f;
            text.fontSizeMax = 24f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.alignment = TextAlignmentOptions.Center;
            text.color = FlowUiFactory.White;
            text.faceColor = FlowUiFactory.White;
            text.fontMaterial.SetColor(ShaderUtilities.ID_FaceColor, FlowUiFactory.White);
            text.outlineColor = new Color32(8, 48, 103, 255);
            text.outlineWidth = 0.16f;
            FlowUiFactory.Stretch(text.rectTransform);
        }
    }

    private static void HideLegacyControls()
    {
        TextMeshProUGUI[] tmpTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < tmpTexts.Length; i++)
            HideLegacyButton(tmpTexts[i].text, tmpTexts[i].GetComponentInParent<Button>());

        Text[] legacyTexts = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < legacyTexts.Length; i++)
            HideLegacyButton(legacyTexts[i].text, legacyTexts[i].GetComponentInParent<Button>());
    }

    private static void HideLegacyButton(string label, Button button)
    {
        if (button == null || string.IsNullOrWhiteSpace(label))
            return;

        string normalized = label.Trim().ToUpperInvariant();
        if (normalized == "MENU"
            || normalized == "BACK"
            || normalized == "RETRY"
            || normalized == "UNDO"
            || normalized == "RESTART")
        {
            button.gameObject.SetActive(false);
        }
    }

    private static void OpenActivities()
    {
        SceneManager.LoadScene(ActivityFlowCatalog.SelectorScene);
    }

    private static void RestartStage()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private static void UndoLabOutput()
    {
        if (Main.Instance != null)
            Main.Instance.UndoSelectedOutput();
    }
}
