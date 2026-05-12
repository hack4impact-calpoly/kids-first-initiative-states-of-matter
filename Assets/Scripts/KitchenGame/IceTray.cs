using UnityEngine;
using UnityEngine.UI;

public class IceTray : MonoBehaviour
{
    private const int VerticalFillOriginBottom = 0;

    [SerializeField] private Image fillImage;
    [SerializeField] private float fillPerDroplet = 0.05f;
    [SerializeField, Range(0f, 0.45f)] private float hitboxTopInset = 0f;
    [SerializeField, Range(0f, 0.45f)] private float hitboxHorizontalInset = 0f;

    [Header("Fill Visual")]
    [SerializeField] private bool tintFillImage = false;
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color fullColor = new Color(1f, 0.55f, 0f, 1f);

    private readonly Vector3[] trayCorners = new Vector3[4];

    void Start()
    {
        if (fillImage != null)
        {
            ConfigureFillImage();
            fillImage.fillAmount = 0f;
            ApplyFillVisual();
        }
    }

    public bool IsFull()    
    {
        return fillImage != null && fillImage.fillAmount >= 0.99f;
    }

    public void AddJuice()
    {
        if (fillImage == null) return;

        fillImage.fillAmount += fillPerDroplet;
        fillImage.fillAmount = Mathf.Clamp01(fillImage.fillAmount);
        ApplyFillVisual();

        Debug.Log("Tray fill amount: " + fillImage.fillAmount);
    }

    public RectTransform GetTrayRect()
    {
        return fillImage != null ? fillImage.GetComponent<RectTransform>() : null;
    }

    public bool ContainsWorldPoint(Vector3 worldPoint)
    {
        RectTransform trayRect = GetTrayRect();

        if (trayRect == null)
        {
            return false;
        }

        trayRect.GetWorldCorners(trayCorners);

        float minX = trayCorners[0].x;
        float minY = trayCorners[0].y;
        float maxX = trayCorners[2].x;
        float maxY = trayCorners[2].y;
        float width = maxX - minX;
        float height = maxY - minY;

        minX += width * hitboxHorizontalInset;
        maxX -= width * hitboxHorizontalInset;
        maxY -= height * hitboxTopInset;

        return worldPoint.x >= minX &&
            worldPoint.x <= maxX &&
            worldPoint.y >= minY &&
            worldPoint.y <= maxY;
    }

    private void ConfigureFillImage()
    {
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Vertical;
        fillImage.fillOrigin = VerticalFillOriginBottom;
    }

    private void ApplyFillVisual()
    {
        fillImage.color = tintFillImage
            ? Color.Lerp(emptyColor, fullColor, fillImage.fillAmount)
            : Color.white;
    }
}
