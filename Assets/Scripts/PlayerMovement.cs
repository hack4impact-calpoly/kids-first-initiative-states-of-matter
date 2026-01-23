using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerMovement : MonoBehaviour
{
    public float speed = 5;
    public int facingDir = 1;
    public Vector2 moveInput;
    public float hitLockTime = 0.5f;
    bool isHitting;
    float hitTimer;
    public Rigidbody2D rb;
    public Animator anim;


    // Start is called before the first frame update
    // void Start()
    // {
        
    // }

    // Update is called once per frame
    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        moveInput = new Vector2(horizontal, vertical).normalized;

        if(horizontal > 0 && transform.localScale.x < 0 ||
        horizontal < 0 && transform.localScale.x > 0)
        {
            Flip();
        }

        if (Input.GetButtonDown("Jump") && !isHitting)
        {
            isHitting = true;
            hitTimer = hitLockTime;
            anim.SetTrigger("Hit");
        }

        anim.SetFloat("horizontal", Mathf.Abs(horizontal));
        anim.SetFloat("vertical", Mathf.Abs(vertical));
        anim.SetBool("isHitting", isHitting);

 

        rb.linearVelocity = new Vector2(horizontal, vertical) * speed;

    }

    void FixedUpdate()
    {
        if (isHitting)
        {
            rb.linearVelocity = Vector2.zero;
            hitTimer -= Time.fixedDeltaTime;

            if (hitTimer <= 0f)
                isHitting = false;
        }
        else
        {
            rb.linearVelocity = moveInput * speed;
        }
    }

    void Flip()
    {
        facingDir *= -1;
        transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
    }

}
