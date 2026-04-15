using UnityEngine;

public class Wire : MonoBehaviour
{
    public SpriteRenderer wireEnd;
    public GameObject lightOn;
    Vector3 startPoint;
    Vector3 startPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPoint = transform.parent.position;
        startPosition = transform.position; // Store initial pos
    }

    private void OnMouseDrag() 
    {

        // If output exists before dragging wire
        if (!HasOutputSide())
        {
            WireGameUIManager.Instance.ShowMessage("Add an output device (candle or plasma)");
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

                // Validate output
                if (!IsValidOutputSlot(collider))
                {
                    continue;
                }

                // update wire to the connection point position
                UpdateWire(collider.transform.position);

                // check if the wires are the same color
                if (transform.parent.name.Equals(collider.transform.parent.name))
                {
                    // count connection
                    Main.Instance.LightOn(1);

                    // finish step
                    collider.GetComponent<Wire>()?.Done();
                    Done();
                }
                return;
            }
        } 

        UpdateWire(newPosition);
    }

    void Done() {
        lightOn.SetActive(true);

        Destroy(this);
    }

    private void OnMouseUp()
    {
        UpdateWire(startPosition);
    }

    void UpdateWire(Vector3 newPosition) {
        // update wire position
        transform.position = newPosition;

        // update direction
        Vector3 direction = newPosition - startPoint; 
        transform.right = direction * transform.lossyScale.x;

        // update scale
        float dist = direction.magnitude;
        wireEnd.size = new Vector2(dist, wireEnd.size.y);
    }

    bool HasOutputSide()
    {
        Candle candle = FindObjectOfType<Candle>();
        Plasma plasma = FindObjectOfType<Plasma>();
        
        return candle != null || plasma != null;
    }

    bool IsValidOutputSlot(Collider2D collider)
    {
        Wire wireComponent = collider.GetComponent<Wire>();
        if (wireComponent == null)
            return false;
        
        return true;
    }
}
