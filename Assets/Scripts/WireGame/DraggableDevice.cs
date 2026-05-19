using UnityEngine;

public class DraggableDevice : MonoBehaviour
{
    public float snapRadius = 0.5f;

    Vector3 startPosition;
    bool isSnapped;
    SnapZone snappedZone;

    void Start()
    {
        startPosition = transform.position;
    }

    private void OnMouseDown()
    {
        // If currently snapped, unsnap first
        if (isSnapped && snappedZone != null)
        {
            snappedZone.Unsnap();
            isSnapped = false;
            snappedZone = null;
        }
    }

    private void OnMouseDrag()
    {
        if (Camera.main == null) return; 
        Vector3 newPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        newPosition.z = 0;
        transform.position = newPosition;
    }

    private void OnMouseUp()
    {
        // Check for nearby snap zones
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, snapRadius);
        foreach (Collider2D collider in colliders)
        {
            SnapZone zone = collider.GetComponent<SnapZone>();
            if (zone != null)
            {
                zone.Snap(this);
                isSnapped = true;
                snappedZone = zone;
                return;
            }
        }

        // No snap zone found, return to start
        ReturnToStart();
    }

    public void ReturnToStart()
    {
        if (snappedZone != null)
        {
            snappedZone.Unsnap();
        }

        isSnapped = false;
        snappedZone = null;
        transform.position = startPosition;
    }
}
