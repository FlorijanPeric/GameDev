/*using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    public Transform cameraRoot;
    public float sensitivity = 0.1f;

    private Vector2 lookInput;
    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    void Update()
    {
        // Unlock for testing
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Vertical (camera)
        xRotation -= lookInput.y * sensitivity;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Horizontal (player)
        transform.Rotate(Vector3.up * lookInput.x * sensitivity);
    }
}
*/
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonLook : MonoBehaviour
{
    public Transform cameraRoot;
    public float sensitivity = 1.5f; // lower to reduce shake
    public float smoothTime = 0.05f;

    private Vector2 lookInput;
    private Vector2 currentLook;
    private Vector2 lookVelocity;
    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    void Update()
    {
        // Smooth look
        currentLook = Vector2.SmoothDamp(currentLook, lookInput, ref lookVelocity, smoothTime);

        xRotation -= currentLook.y * sensitivity;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * currentLook.x * sensitivity);
    }
}

