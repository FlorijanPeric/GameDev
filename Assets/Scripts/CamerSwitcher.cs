using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSwitcher : MonoBehaviour
{
    [Header("Cameras")]
    public Camera firstPersonCamera;
    public Camera thirdPersonCamera;

    [Header("Player Body")]
    public GameObject playerBody; // Mesh to hide in FPS

    [Header("Input")]
    public bool allowKeyboardV = true; // quick debug toggle using V

    private bool isFirstPerson = false;

    void Start()
    {
        // Ensure cameras are set up and start in third person by default
        SetThirdPerson();
    }

    void Update()
    {
        // Optional manual switch (for testing)
        if (allowKeyboardV && Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            ToggleCamera();
        }
    }

    public void SetFirstPerson()
    {
        isFirstPerson = true;
        SetCameraActive(firstPersonCamera, true);
        SetCameraActive(thirdPersonCamera, false);

        if (playerBody) playerBody.SetActive(false);
    }

    public void SetThirdPerson()
    {
        isFirstPerson = false;
        SetCameraActive(firstPersonCamera, false);
        SetCameraActive(thirdPersonCamera, true);

        if (playerBody) playerBody.SetActive(true);
    }

    public void ToggleCamera()
    {
        if (isFirstPerson) SetThirdPerson(); else SetFirstPerson();
    }

    // Helper: activate/deactivate camera GameObject + its AudioListener safely
    private void SetCameraActive(Camera cam, bool active)
    {
        if (cam == null) return;

        // Prefer activating the whole GameObject to avoid odd camera state
        cam.gameObject.SetActive(active);
        cam.enabled = active;

        AudioListener al = cam.GetComponent<AudioListener>();
        if (al != null) al.enabled = active;
    }

    // Public methods for UI or Input System wiring
    public void SwitchToFirstPerson() => SetFirstPerson();
    public void SwitchToThirdPerson() => SetThirdPerson();
    public void SwitchToggle() => ToggleCamera();
}
