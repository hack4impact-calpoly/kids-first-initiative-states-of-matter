using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JuicePouring : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Jug Rotation")]
    [SerializeField] private float targetAngle = 90f;
    [SerializeField] private float rotationSpeed = 220f;

    [Header("Dragging")]
    [SerializeField] private Canvas canvas;

    [Header("Pouring")]
    [SerializeField] private GameObject dropletPrefab;
    [SerializeField] private RectTransform spawnPoint;
    [SerializeField] private float dropletSpawnInterval = 0.08f;
    [SerializeField] private IceTray tray;

    private RectTransform rectTransform;
    private bool shouldRotate = false;
    private bool isDragging = false;
    private bool canPour = false;
    private float dropletTimer = 0f;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        shouldRotate = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }

    void Update()
    {
        HandleRotation();
        HandlePouring();
    }

    private void HandleRotation()
    {
        if (!shouldRotate) return;

        float currentZ = rectTransform.localEulerAngles.z;
        float newZ = Mathf.MoveTowardsAngle(currentZ, targetAngle, rotationSpeed * Time.deltaTime);

        rectTransform.localRotation = Quaternion.Euler(0f, 0f, newZ);

        if (Mathf.Abs(Mathf.DeltaAngle(newZ, targetAngle)) < 0.1f)
        {
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, targetAngle);
            shouldRotate = false;
            canPour = true;
        }
    }

    private void HandlePouring()
    {
        if (!isDragging || !canPour || dropletPrefab == null || spawnPoint == null) return;

        dropletTimer += Time.deltaTime;

        if (dropletTimer >= dropletSpawnInterval)
        {
            dropletTimer = 0f;
            GameObject dropletObj = Instantiate(dropletPrefab, spawnPoint.position, Quaternion.identity, canvas.transform);

            JuiceDroplet droplet = dropletObj.GetComponent<JuiceDroplet>();

            if (droplet != null && tray != null)
            {
                droplet.SetTray(tray);
            }
        }
    }
}