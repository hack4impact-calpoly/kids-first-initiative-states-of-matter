using UnityEngine;

public class RotateOnClick : MonoBehaviour
{
    public float degrees = 90f;

    void OnMouseDown()
    {
        Debug.Log("clicked");
        transform.Rotate(0f, 0f, degrees);
    }
}