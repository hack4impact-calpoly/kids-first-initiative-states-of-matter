using UnityEngine;
using UnityEngine.SceneManagement;

public class Wire : MonoBehaviour
{
    public SpriteRenderer wireEnd;
    public GameObject lightOn;
    Vector3 startPoint;
    Vector3 startPosition;

    void Start()
    {
        startPoint = transform.parent.position;
        startPosition = transform.position;
    }

    private void OnMouseDrag()
    {
        if (Main.Instance.isLocked) return;

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
