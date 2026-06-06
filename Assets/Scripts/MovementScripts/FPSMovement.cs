using UnityEngine;
using UnityEngine.InputSystem;

public class FPSMovement : MonoBehaviour
{
    public CharacterController controller;

    public float speed = 7f;
    public float speedMultiplier = 1f;
    public float gravity = -14f;
    public float jumpHeight = 0.9f;
    public float maxFallSpeed = -20f;

    [Header("Air Movement")]
    public float airAcceleration = 8f;

    [Header("Crouch")]
    public float standingHeight = 2f;
    public float crouchHeight = 1.2f;
    public float crouchSpeedMultiplier = 0.55f;
    public float crouchLerpSpeed = 14f;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    private Vector3 velocity;
    private bool isGrounded;
    private bool hasJumped;
    private Transform movementBasis;

    // Input System variables
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;

    private void Awake()
    {
        CharacterController ownController = GetComponent<CharacterController>();
        if (ownController != null)
        {
            controller = ownController;
        }

        playerInput = GetComponent<PlayerInput>();

        if (controller == null)
        {
            Debug.LogError("FPSMovement requires a CharacterController on the same GameObject.", this);
            enabled = false;
            return;
        }

        if (playerInput == null)
        {
            Debug.LogError("FPSMovement requires a PlayerInput component on the same GameObject.", this);
            enabled = false;
            return;
        }

        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        movementBasis = transform;

        if (moveAction == null || jumpAction == null)
        {
            Debug.LogError("FPSMovement could not find Move/Jump actions on the PlayerInput asset.", this);
            enabled = false;
            return;
        }

        if (!controller.enabled)
        {
            controller.enabled = true;
        }

        CharacterController[] controllersInChildren = GetComponentsInChildren<CharacterController>(true);
        for (int i = 0; i < controllersInChildren.Length; i++)
        {
            if (controllersInChildren[i] != controller)
            {
                controllersInChildren[i].enabled = false;
            }
        }
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        jumpAction?.Enable();
    }

    private void OnDisable()
    {
        jumpAction?.Disable();
        moveAction?.Disable();
    }

    void Update()
    {
        if (groundCheck == null)
        {
            Debug.LogError("FPSMovement requires a groundCheck Transform reference.", this);
            enabled = false;
            return;
        }

        // Check if grounded
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Movement
        bool crouching = Keyboard.current != null && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);
        UpdateCrouch(crouching);

        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 forward = Vector3.ProjectOnPlane(movementBasis.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(movementBasis.right, Vector3.up).normalized;
        Vector3 move = right * input.x + forward * input.y;

        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        float baseSpeed = speed * Mathf.Max(0.1f, speedMultiplier);
        float movementSpeed = crouching ? baseSpeed * crouchSpeedMultiplier : baseSpeed;
        controller.Move(move * movementSpeed * Time.deltaTime);

        // Jump: strict single jump, reset only when grounded and falling/stable.
        if (isGrounded && velocity.y <= 0f)
        {
            hasJumped = false;
        }

        if (jumpAction.triggered && isGrounded && !hasJumped)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            hasJumped = true;
        }

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        
        // Cap max fall speed
        if (velocity.y < maxFallSpeed)
        {
            velocity.y = maxFallSpeed;
        }

        controller.Move(velocity * Time.deltaTime);
    }

    private void UpdateCrouch(bool crouching)
    {
        float targetHeight = crouching ? crouchHeight : standingHeight;
        targetHeight = Mathf.Max(0.5f, targetHeight);

        float previousHeight = controller.height;
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchLerpSpeed);

        float heightDelta = controller.height - previousHeight;
        Vector3 center = controller.center;
        center.y += heightDelta * 0.5f;
        controller.center = center;
    }
}
