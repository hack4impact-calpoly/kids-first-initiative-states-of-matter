using UnityEngine;
using UnityEngine.SceneManagement;

public class Wire : MonoBehaviour
{
    public SpriteRenderer wireEnd;
    public GameObject lightOn;
    Vector3 startPoint;
    Vector3 startPosition;
    private bool isConnected = false;
    private string currentScene;

    void Start()
    {
        startPoint = transform.parent.position;
        startPosition = transform.position;
        currentScene = SceneManager.GetActiveScene().name;
    }

    private void OnMouseDrag()
    {
        if (Camera.main == null)
            return;

        // Check if an output device is connected before allowing wire usage
        if (Main.Instance != null && Main.Instance.isLocked)
        {
            Main.Instance.ReportWireInteractionBlocked();

            if (WireGameUIManager.Instance != null)
            {
                WireGameUIManager.Instance.ShowMessage("Connect an output (candle or plasma) to use wires!", isWarning: true);
            }

            if (WireGameGuidanceController.Instance != null)
                WireGameGuidanceController.Instance.ShowConnectOutputGuidance();

            return;
        }

        // mouse position to world point
        Vector3 newPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        newPosition.z = 0; 

        // check for nearby connection points
        Collider2D[] colliders = Physics2D.OverlapCircleAll(newPosition, .2f);
        foreach (Collider2D collider in colliders)
        {
            // make sure not my own collider
            if (collider.gameObject != gameObject)
            {
                // Validate that this is a valid slot for connection
                if (!IsValidSlot(collider))
                {
                    continue;
                }

                // update wire to the connection point position
                UpdateWire(collider.transform.position);

                // check if the wires are the same color
                if (transform.parent.name.Equals(collider.transform.parent.name))
                {
                    // count connection
                    if (Main.Instance != null)
                        Main.Instance.LightOn(1);
                    isConnected = true;

                    // finish step
                    collider.GetComponent<Wire>()?.Done();
                    Done();
                }
                return;
            }
        } 

        UpdateWire(newPosition);
    }

    bool IsValidSlot(Collider2D collider)
    {
        // Validate that the target slot belongs to a valid output component
        Wire targetWire = collider.GetComponent<Wire>();
        return targetWire != null;
    }

    void Done() {
        lightOn.SetActive(true);
        Destroy(this);
    }

    private void OnMouseUp()
    {
        // Return to original position if no valid connection was made
        if (!isConnected)
        {
            UpdateWire(startPosition);
        }
    }

    void UpdateWire(Vector3 newPosition) {
        // update position
        transform.position = newPosition;

        // update direction
        Vector3 direction = newPosition - startPoint; 
        transform.right = direction * transform.lossyScale.x;

        // update scale
        float dist = direction.magnitude;
        wireEnd.size = new Vector2(dist, wireEnd.size.y);
    }
}
