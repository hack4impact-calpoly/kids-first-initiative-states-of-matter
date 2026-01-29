using UnityEngine;

/*
    This script provides jumping and movement in Unity 2D - Gatsby
*/

public class Player : MonoBehaviour
{
    // Left/Right Movement
    private Rigidbody2D _rigidbody;
    private float moveX;
    private float moveY;
    private Vector2 movement;
    public float MoveSpeed = 5f;

    // Jumping
    public float JumpForce = 10f;
    public LayerMask GroundLayer;
    public BoxCollider2D GroundCollider;
    public bool OnGround;

    void Start()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        OnGround = true;
    }

    void Update()
    {
        moveX = Input.GetAxisRaw("Horizontal");   
        moveY = Input.GetAxisRaw("Vertical");   
        if(Input.GetKeyDown(KeyCode.Space) && OnGround)
        {
            // Make our player jump
            _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, JumpForce);
            OnGround = false;
        }
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        // Check if we collided with the ground
        OnGround = true;
    }

    void FixedUpdate()
    {
        // Determine our movement vector based on our speed and move inputs
        movement = new Vector2(moveX * MoveSpeed, _rigidbody.linearVelocity.y +moveY * MoveSpeed * 0.3f);
        
        // Make our player move left/right
        _rigidbody.linearVelocity = movement;
    }

}
