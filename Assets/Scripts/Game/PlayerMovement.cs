using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header ("Movement")]
    [SerializeField] float speed;
    [SerializeField] float groundDrag;
    [SerializeField] float jumpForce;
    [SerializeField] float jumpCooldown;
    [SerializeField] float airMultiplier;
    bool readyToJump;


    [Header("Ground Check")]
    [SerializeField] float playerHeight;
    [SerializeField] LayerMask whatIsGround;
    bool grounded;


    [SerializeField] Rigidbody rb;

    [SerializeField] Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb.freezeRotation = true;
        ResetJump();
    }

    private void Update()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        if (grounded)
            rb.linearDamping = groundDrag;
        else
            rb.linearDamping = 0f;

        SpeedControl();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        MovePlayer();
    }

    public void PlayerMovementInput(Vector2 value) 
    {

        horizontalInput = value.x;
        verticalInput = value.y;
    }

    void MovePlayer()
    {
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        
        if (grounded)
            rb.AddForce(moveDirection.normalized * speed, ForceMode.Force);
        
        else if(!grounded)
            rb.AddForce(moveDirection.normalized * speed * airMultiplier, ForceMode.Force);
    }

    void SpeedControl() 
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (flatVel.magnitude > speed) 
        {
            Vector3 limitedVel = flatVel.normalized * speed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }

    public void Jump() 
    {
        if (readyToJump && grounded) 
        {
            readyToJump = false;

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0.0f, rb.linearVelocity.z);

            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void ResetJump() 
    {
        readyToJump = true;
    }
}
