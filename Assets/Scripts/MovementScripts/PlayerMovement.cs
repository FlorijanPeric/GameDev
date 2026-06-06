using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 8.5f;
    public float gravity = -9.81f;

    [Header("Jump")]
    public float jumpForce = 5.5f;
    public int maxJumps = 1; // Single jump only

    private Vector2 moveInput;
    private CharacterController controller;
    private Animator animator;

    private float verticalVelocity;
    
    private int jumpCount = 0; // Tracks number of jumps

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

private bool jumpRequest = false;
private bool wasGrounded;

void Update()
{
    bool isRunning = Keyboard.current.leftShiftKey.isPressed;

    // -------- HANDLE JUMP INPUT --------
    if (Keyboard.current.spaceKey.wasPressedThisFrame)
    {
        jumpRequest = true;
    }

    // -------- GRAVITY & JUMP LOGIC --------
    if (controller.isGrounded)
    {
        verticalVelocity = -2f; // stick to ground
        jumpCount = 0;          // reset jumps
    }

    if (jumpRequest && jumpCount < maxJumps)
    {
        verticalVelocity = jumpForce;
        jumpCount++;
        jumpRequest = false;
    }

    verticalVelocity += gravity * Time.deltaTime;

    // -------- MOVE --------
    Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y);
    inputDir = Vector3.ClampMagnitude(inputDir, 1f);
    Vector3 move = transform.TransformDirection(inputDir);
    move.y = verticalVelocity;
    float targetSpeed = isRunning ? runSpeed : walkSpeed;
    controller.Move(move * targetSpeed * Time.deltaTime);

    // -------- ANIMATOR --------
    // Update Speed for walking/running animation
    animator.SetFloat("Speed", inputDir.magnitude * (isRunning ? 1.5f : 1f), 0.1f, Time.deltaTime);

    // Detect grounded state change
    if (controller.isGrounded != wasGrounded)
    {
        if (!controller.isGrounded)
        {
            // Left the ground → Jump started
            animator.SetTrigger("Jump"); 
        }
        else
        {
            // Landed
            OnLand();
        }

        animator.SetBool("Grounded", controller.isGrounded);
    }

    wasGrounded = controller.isGrounded;

    // Optional: smooth InAir animation while airborne
    animator.SetBool("InAir", !controller.isGrounded);
}


public void OnLand()
{
    animator.SetBool("Grounded",false);
       // This will be called when the animation triggers OnLand
    Debug.Log("Player landed!");
    // You can play landing sound, particle effect, etc.
}


}
