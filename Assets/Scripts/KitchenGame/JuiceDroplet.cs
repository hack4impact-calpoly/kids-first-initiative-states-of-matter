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
        RectTransform trayRect = tray.GetTrayRect();

        if (trayRect == null)
        {
            Debug.Log("trayRect is null");
            return false;
        }

        Vector3[] trayCorners = new Vector3[4];
        trayRect.GetWorldCorners(trayCorners);

        Vector3 pos = rectTransform.position;

        Debug.Log("Droplet pos: " + pos);
        Debug.Log("Tray bottom left: " + trayCorners[0]);
        Debug.Log("Tray top right: " + trayCorners[2]);

        bool overlaps =
            pos.x >= trayCorners[0].x &&
            pos.x <= trayCorners[2].x &&
            pos.y >= trayCorners[0].y &&
            pos.y <= trayCorners[2].y;

        if (overlaps)
        {
            Debug.Log("Overlap detected");
        }

        return overlaps;
    }
}