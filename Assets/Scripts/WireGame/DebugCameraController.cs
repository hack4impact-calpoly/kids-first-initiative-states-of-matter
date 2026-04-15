using UnityEngine;

/// DEBUG ONLY - Delete this script (and its .meta) before release.
/// Add to Main Camera for WASD camera movement during testing.
public class DebugCameraController : MonoBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        float h = 0f;
        float v = 0f;

        if (Input.GetKey(KeyCode.W)) v += 1f;
        if (Input.GetKey(KeyCode.S)) v -= 1f;
        if (Input.GetKey(KeyCode.A)) h -= 1f;
        if (Input.GetKey(KeyCode.D)) h += 1f;

        if (h != 0f || v != 0f)
        {
            Vector3 move = new Vector3(h, v, 0f) * moveSpeed * Time.deltaTime;
            transform.position += move;
        }
    }
}
