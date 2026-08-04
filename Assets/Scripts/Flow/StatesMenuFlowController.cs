using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class StatesMenuFlowController : MonoBehaviour, IFlowSceneController
{
    private bool initialized;

    public void InitializeFlow()
    {
        if (initialized)
            return;

        initialized = true;
        ConfigurePlayButton();
        ConfigureCreditsButton();
    }

    private static void ConfigurePlayButton()
    {
        GameObject buttonObject = GameObject.Find("StartButton (2)");
        if (buttonObject == null)
        {
            Debug.LogWarning("[ActivityFlow] Could not find the title Play button.");
            return;
        }

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        Image image = buttonObject.GetComponent<Image>();
        Button button = buttonObject.GetComponent<Button>();
        if (rect == null || image == null || button == null)
            return;

        rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(520f, 104f);

        image.sprite = FlowUiFactory.GetButtonSprite();
        image.type = Image.Type.Sliced;
        image.color = FlowUiFactory.Blue;

        Outline outline = buttonObject.GetComponent<Outline>();
        if (outline == null)
            outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color32(8, 48, 103, 230);
        outline.effectDistance = new Vector2(5f, -5f);

        TextMeshProUGUI existing = buttonObject.GetComponentInChildren<TextMeshProUGUI>(true);
        if (existing != null)
            Destroy(existing.gameObject);

        string label = StageProgressService.HasAnyProgress() ? "CONTINUE" : "PLAY";
        TextMeshProUGUI text = FlowUiFactory.CreateText(
            buttonObject.transform,
            "Play Label",
            label,
            56f,
            TextAlignmentOptions.Center,
            FlowUiFactory.White);
        text.outlineColor = new Color32(8, 48, 103, 255);
        text.outlineWidth = 0.24f;
        text.faceColor = FlowUiFactory.White;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.98f, 1f, 1f);
        colors.pressedColor = new Color(0.72f, 0.88f, 1f, 1f);
        button.colors = colors;
        button.targetGraphic = image;
    }

    private static void ConfigureCreditsButton()
    {
        GameObject buttonObject = GameObject.Find("CreditsButton");
        if (buttonObject == null)
            return;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        Image image = buttonObject.GetComponent<Image>();
        Button button = buttonObject.GetComponent<Button>();
        if (rect == null || image == null || button == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(24f, 24f);
        rect.sizeDelta = new Vector2(250f, 76f);
        rect.localScale = Vector3.one;

        image.sprite = FlowUiFactory.GetButtonSprite();
        image.type = Image.Type.Sliced;
        image.color = FlowUiFactory.Orange;

        Outline outline = buttonObject.GetComponent<Outline>();
        if (outline == null)
            outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color32(8, 48, 103, 220);
        outline.effectDistance = new Vector2(3f, -3f);

        TMP_Text[] existingTmp = buttonObject.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < existingTmp.Length; i++)
            existingTmp[i].gameObject.SetActive(false);

        Text[] existingLegacy = buttonObject.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < existingLegacy.Length; i++)
            existingLegacy[i].gameObject.SetActive(false);

        TextMeshProUGUI label = FlowUiFactory.CreateText(
            buttonObject.transform,
            "Credits Label",
            "CREDITS",
            28f,
            TextAlignmentOptions.Center,
            FlowUiFactory.White);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        button.targetGraphic = image;
    }
}
