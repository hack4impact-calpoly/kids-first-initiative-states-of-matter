using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameSelectorFlowController : MonoBehaviour, IFlowSceneController
{
    private Canvas overlayCanvas;
    private GameObject choiceModal;
    private readonly List<CardLayout> cards = new List<CardLayout>(3);
    private float lastParentWidth = -1f;
    private float lastParentHeight = -1f;
    private bool initialized;

    private sealed class CardLayout
    {
        public RectTransform Rect;
        public Vector2 Size;
    }

    public void InitializeFlow()
    {
        if (initialized)
            return;

        initialized = true;
        BuildHeader();
        ConfigureCard("Canvas/Button", StageProgressIds.MatterKitchen, FlowUiFactory.Orange);
        ConfigureCard("Canvas/Button (1)", StageProgressIds.PipeRescue, FlowUiFactory.Green);
        ConfigureCard("Canvas/Button (2)", StageProgressIds.StateLab, FlowUiFactory.Purple);
        ArrangeCards(true);
    }

    private void LateUpdate()
    {
        ArrangeCards(false);
    }

    private void BuildHeader()
    {
        overlayCanvas = FlowUiFactory.CreateCanvas("Selector Flow Canvas", 350);
        overlayCanvas.transform.SetParent(transform, false);

        Image header = FlowUiFactory.CreatePanel(
            overlayCanvas.transform,
            "Header",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(22f, -105f),
            new Vector2(-22f, -16f),
            new Color(0.04f, 0.12f, 0.2f, 0.82f));
        header.raycastTarget = false;

        TextMeshProUGUI title = FlowUiFactory.CreateText(
            header.transform,
            "Title",
            "CHOOSE AN ACTIVITY",
            38f,
            TextAlignmentOptions.Center,
            FlowUiFactory.White);
        title.rectTransform.offsetMin = new Vector2(220f, 34f);
        title.rectTransform.offsetMax = new Vector2(-220f, -4f);

        string recommended = ActivityFlowCatalog.GetDisplayName(ActivityFlowCatalog.GetRecommendedActivity());
        TextMeshProUGUI subtitle = FlowUiFactory.CreateText(
            header.transform,
            "Recommendation",
            "Suggested next: " + recommended,
            22f,
            TextAlignmentOptions.Center,
            FlowUiFactory.Gold);
        subtitle.fontStyle = FontStyles.Bold;
        subtitle.rectTransform.offsetMin = new Vector2(220f, 4f);
        subtitle.rectTransform.offsetMax = new Vector2(-220f, -52f);

        Button back = FlowUiFactory.CreateButton(header.transform, "Back", "TITLE", FlowUiFactory.Blue, OpenTitle);
        FlowUiFactory.SetRect(back, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, -30f), new Vector2(178f, 30f));
    }

    private void ConfigureCard(string path, string activityId, Color accentColor)
    {
        GameObject cardObject = GameObject.Find(path);
        if (cardObject == null)
        {
            Debug.LogWarning("[ActivityFlow] Could not find selector card " + path + ".");
            return;
        }

        Button button = cardObject.GetComponent<Button>();
        if (button == null)
            return;

        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        if (cardRect != null)
        {
            cards.Add(new CardLayout
            {
                Rect = cardRect,
                Size = cardRect.rect.size
            });
        }

        GameSelectorCardAction action = cardObject.GetComponent<GameSelectorCardAction>();
        if (action == null)
            action = cardObject.AddComponent<GameSelectorCardAction>();
        action.Initialize(this, activityId);

        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(action.Open);

        ColorBlock colors = button.colors;
        colors.normalColor = Color.clear;
        colors.highlightedColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.12f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.26f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.08f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        Image footer = FlowUiFactory.CreatePanel(
            cardObject.transform,
            "Activity Details",
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(18f, 18f),
            new Vector2(-18f, 132f),
            new Color(0.03f, 0.08f, 0.13f, 0.88f));
        footer.raycastTarget = false;

        TextMeshProUGUI theme = FlowUiFactory.CreateText(
            footer.transform,
            "Theme",
            ActivityFlowCatalog.GetTheme(activityId),
            24f,
            TextAlignmentOptions.Center,
            FlowUiFactory.White);
        theme.fontStyle = FontStyles.Bold;
        theme.rectTransform.offsetMin = new Vector2(16f, 38f);
        theme.rectTransform.offsetMax = new Vector2(-16f, -12f);

        TextMeshProUGUI status = FlowUiFactory.CreateText(
            footer.transform,
            "Status",
            ActivityFlowCatalog.GetStatusLabel(activityId),
            18f,
            TextAlignmentOptions.BottomLeft,
            accentColor);
        status.rectTransform.offsetMin = new Vector2(18f, 8f);
        status.rectTransform.offsetMax = new Vector2(-120f, -78f);

        FlowUiFactory.AddProgressDots(footer.transform, activityId, accentColor);

        if (activityId == ActivityFlowCatalog.GetRecommendedActivity())
        {
            Image badge = FlowUiFactory.CreatePanel(
                cardObject.transform,
                "Recommended Badge",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(-105f, 8f),
                new Vector2(105f, 50f),
                FlowUiFactory.Gold);
            badge.raycastTarget = false;
            TextMeshProUGUI badgeText = FlowUiFactory.CreateText(
                badge.transform,
                "Label",
                "START HERE",
                20f,
                TextAlignmentOptions.Center,
                FlowUiFactory.Ink);
            badgeText.fontStyle = FontStyles.Bold;
        }
    }

    private void ArrangeCards(bool force)
    {
        if (cards.Count == 0 || cards[0].Rect == null || cards[0].Rect.parent == null)
            return;

        RectTransform parent = cards[0].Rect.parent as RectTransform;
        if (parent == null)
            return;

        float parentWidth = parent.rect.width;
        float parentHeight = parent.rect.height;
        if (!force
            && Mathf.Abs(parentWidth - lastParentWidth) < 0.5f
            && Mathf.Abs(parentHeight - lastParentHeight) < 0.5f)
        {
            return;
        }

        lastParentWidth = parentWidth;
        lastParentHeight = parentHeight;

        const float gap = 28f;
        float totalWidth = gap * (cards.Count - 1);
        float maxHeight = 0f;
        for (int i = 0; i < cards.Count; i++)
        {
            totalWidth += cards[i].Size.x;
            maxHeight = Mathf.Max(maxHeight, cards[i].Size.y);
        }

        float horizontalMargin = parentWidth < 1800f ? 320f : 100f;
        float widthScale = (parentWidth - horizontalMargin) / totalWidth;
        float heightScale = (parentHeight - 190f) / maxHeight;
        float scale = Mathf.Clamp(Mathf.Min(widthScale, heightScale), 0.72f, 1f);
        float scaledTotalWidth = (totalWidth - gap * (cards.Count - 1)) * scale
            + gap * (cards.Count - 1);
        float cursor = -scaledTotalWidth * 0.5f;

        for (int i = 0; i < cards.Count; i++)
        {
            CardLayout card = cards[i];
            float scaledWidth = card.Size.x * scale;
            card.Rect.localScale = Vector3.one * scale;
            card.Rect.anchoredPosition = new Vector2(
                cursor + scaledWidth * 0.5f,
                Mathf.Min(-52f, 18f - parentHeight * 0.04f));
            cursor += scaledWidth + gap;
        }
    }

    public void OpenActivity(string activityId)
    {
        if (ActivityFlowCatalog.IsActivityComplete(activityId))
        {
            ShowReplayChoice(activityId);
            return;
        }

        SceneManager.LoadScene(ActivityFlowCatalog.GetEntryScene(activityId));
    }

    private void ShowReplayChoice(string activityId)
    {
        if (choiceModal != null)
            Destroy(choiceModal);

        choiceModal = new GameObject("Completed Activity Choice", typeof(RectTransform));
        choiceModal.transform.SetParent(overlayCanvas.transform, false);
        FlowUiFactory.Stretch(choiceModal.GetComponent<RectTransform>());

        Image backdrop = FlowUiFactory.CreatePanel(
            choiceModal.transform,
            "Backdrop",
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            new Color(0f, 0.03f, 0.08f, 0.72f));
        backdrop.raycastTarget = true;

        Image panel = FlowUiFactory.CreatePanel(
            choiceModal.transform,
            "Panel",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-360f, -210f),
            new Vector2(360f, 210f),
            new Color32(247, 251, 255, 255));

        TextMeshProUGUI title = FlowUiFactory.CreateText(
            panel.transform,
            "Title",
            ActivityFlowCatalog.GetDisplayName(activityId) + " Complete",
            42f,
            TextAlignmentOptions.Center,
            FlowUiFactory.Ink);
        title.rectTransform.offsetMin = new Vector2(36f, 250f);
        title.rectTransform.offsetMax = new Vector2(-36f, -36f);

        TextMeshProUGUI body = FlowUiFactory.CreateText(
            panel.transform,
            "Body",
            "Replay this activity, or keep exploring the other games.",
            28f,
            TextAlignmentOptions.Center,
            new Color32(53, 73, 96, 255));
        body.fontStyle = FontStyles.Normal;
        body.rectTransform.offsetMin = new Vector2(70f, 130f);
        body.rectTransform.offsetMax = new Vector2(-70f, -125f);

        Button replay = FlowUiFactory.CreateButton(
            panel.transform,
            "Replay",
            "REPLAY",
            FlowUiFactory.Green,
            () => SceneManager.LoadScene(ActivityFlowCatalog.GetEntryScene(activityId)));
        FlowUiFactory.SetRect(replay, new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(52f, 36f), new Vector2(-14f, 112f));

        Button close = FlowUiFactory.CreateButton(panel.transform, "Close", "KEEP EXPLORING", FlowUiFactory.Blue, CloseChoice);
        FlowUiFactory.SetRect(close, new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(14f, 36f), new Vector2(-52f, 112f));
    }

    private void CloseChoice()
    {
        if (choiceModal != null)
            Destroy(choiceModal);
    }

    private static void OpenTitle()
    {
        SceneManager.LoadScene(ActivityFlowCatalog.TitleScene);
    }
}
