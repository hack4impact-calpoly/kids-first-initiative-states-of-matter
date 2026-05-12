using UnityEngine;

public class JuiceDroplet : MonoBehaviour
{
    [SerializeField] private float fallSpeed = 500f;
    [SerializeField] private float lifetime = 3f;

    private RectTransform rectTransform;
    private IceTray tray;
    private bool loggedMissingTray = false;

    public void SetTray(IceTray targetTray)
    {
        tray = targetTray;
        Debug.Log("SetTray called. tray is null? " + (tray == null));
    }

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        Destroy(gameObject, lifetime);
        Debug.Log("Droplet Awake");
    }

    void Update()
    {
        rectTransform.anchoredPosition += Vector2.down * fallSpeed * Time.deltaTime;

        if (tray == null)
        {
            if (!loggedMissingTray)
            {
                Debug.Log("Droplet has no tray assigned");
                loggedMissingTray = true;
            }
            return;
        }

        if (IsOverlappingTray())
        {
            Debug.Log("Droplet hit tray");
            tray.AddJuice();
            Destroy(gameObject);
        }
    }

    private bool IsOverlappingTray()
    {
        return tray.ContainsWorldPoint(rectTransform.position);
    }
}
