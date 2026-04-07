using UnityEngine;
using UnityEngine.UI;

public class IceTray : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private float fillPerDroplet = 0.05f;

    [Header("Fill Colors")]
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color fullColor = new Color(1f, 0.55f, 0f, 1f);

    void Start()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = 0f;
            fillImage.color = emptyColor;
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

        fillImage.color = Color.Lerp(emptyColor, fullColor, fillImage.fillAmount);

        Debug.Log("Tray fill amount: " + fillImage.fillAmount);
    }

    public RectTransform GetTrayRect()
    {
        return fillImage.GetComponent<RectTransform>();
    }
}