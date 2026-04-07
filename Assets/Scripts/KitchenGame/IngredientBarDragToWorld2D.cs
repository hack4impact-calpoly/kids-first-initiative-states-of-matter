using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// Drag a UI item to spawn a 2D prefab into the world that follows the pointer.
/// On release, the prefab is dropped and physics is enabled.
public class IngredientBarDragToWorld2D : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Spawn")]
    [SerializeField] private GameObject worldPrefab;   // Chocolate.prefab
    [SerializeField] private Camera worldCamera;   
    [SerializeField] private Transform coolingStation;    // Usually Main Camera

    [Header("Drag Behavior")]
    [SerializeField] private float zDepthFromCamera = 10f; // used only for ScreenToWorldPoint conversion
    [SerializeField] private bool cancelIfReleasedOverUI = false;

    private GameObject spawned;
    private Rigidbody2D spawnedRb;
    private Collider2D spawnedCol;

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (worldPrefab == null || worldCamera == null)
            return;

        spawned = Instantiate(worldPrefab);
        spawnedRb = spawned.GetComponent<Rigidbody2D>();
        spawnedCol = spawned.GetComponent<Collider2D>();

        // While dragging, disable physics so it follows the pointer cleanly.
        if (spawnedRb != null)
        {
            spawnedRb.linearVelocity = Vector2.zero;
            spawnedRb.angularVelocity = 0f;
            spawnedRb.bodyType = RigidbodyType2D.Kinematic;
              spawnedRb.simulated = true;
        }

        if (spawnedCol != null)
            spawnedCol.enabled = false; // prevent early trigger/collision while dragging

        MoveSpawnedToPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (spawned == null) return;
        MoveSpawnedToPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
{
    if (spawned == null) return;

    // Optional: cancel if released over UI
    if (cancelIfReleasedOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
    {
        Destroy(spawned);
        ClearRefs();
        return;
    }

    // Enable physics so it drops naturally
    if (spawnedCol != null)
        spawnedCol.enabled = true;

    if (spawnedRb != null)
        spawnedRb.bodyType = RigidbodyType2D.Dynamic;

    // 🔥 ADD THIS PART
    if (IsOverCoolingStation())
    {
        Debug.Log("Switching to freezing scene");
        SceneManager.LoadScene("Kitchen Game - Freezing Pour");
    }

    ClearRefs();
}

    private void MoveSpawnedToPointer(PointerEventData eventData)
    {
        Vector3 screen = eventData.position;

        // For an orthographic camera in 2D, z just needs to be "in front" of the camera.
        // If your camera is at z = -10 (Unity default), then zDepthFromCamera = 10 works well.
        screen.z = zDepthFromCamera;

        Vector3 world = worldCamera.ScreenToWorldPoint(screen);
        world.z = 0f;

        spawned.transform.position = world;
    }

    private void ClearRefs()
    {
        spawned = null;
        spawnedRb = null;
        spawnedCol = null;
    }

    private bool IsOverCoolingStation()
    {
        if (spawned == null || coolingStation == null) return false;

        float distance = Vector2.Distance(spawned.transform.position, coolingStation.position);

        return distance < 2f; // tweak this value if needed
    }
}