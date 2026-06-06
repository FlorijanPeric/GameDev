using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class FPSMouseLook : MonoBehaviour
{
    [Header("References")]
    public Transform playerBody;
    public Transform cameraRoot;
    public Transform gun;

    [Header("Settings")]
    public float sensitivity = 2f;
    public float smoothTime = 0.05f;

    private Vector2 lookInput;
    private Vector2 currentLook;
    private Vector2 lookVelocity;
    private float xRotation = 0f;
    private PlayerInput playerInput;
    private InputAction lookAction;
    private Transform yawRoot;
    private Transform pitchRoot;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        yawRoot = transform;
        yawRoot.rotation = Quaternion.Euler(0f, yawRoot.eulerAngles.y, 0f);

        if (playerBody == null)
        {
            playerBody = transform;
        }

        if (cameraRoot == null)
        {
            Camera fallbackCamera = playerInput != null ? playerInput.camera : Camera.main;
            if (fallbackCamera != null)
            {
                cameraRoot = fallbackCamera.transform;
            }
        }

        if (playerInput != null)
        {
            lookAction = playerInput.actions["Look"];
        }

        pitchRoot = ResolvePitchRoot();
        DisableCinemachineOnPitchRig();
        HideThirdPersonVisuals();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (pitchRoot == null)
        {
            Debug.LogError("FPSMouseLook requires a cameraRoot Transform reference.", this);
            enabled = false;
            return;
        }

        if (gun != null)
        {
            gun.SetParent(pitchRoot, false);
            gun.localPosition = Vector3.zero;
            gun.localRotation = Quaternion.identity;
        }
    }

    // This will be called by PlayerInput Unity Event
    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    void Update()
    {
        if (lookAction != null)
        {
            lookInput = lookAction.ReadValue<Vector2>();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Optional smoothing
        currentLook = Vector2.SmoothDamp(currentLook, lookInput, ref lookVelocity, smoothTime);

        float mouseX = currentLook.x * sensitivity * 100f * Time.deltaTime;
        float mouseY = currentLook.y * sensitivity * 100f * Time.deltaTime;

        // Vertical rotation
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        pitchRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Horizontal rotation
        yawRoot.Rotate(Vector3.up, mouseX, Space.World);
    }

    private Transform ResolvePitchRoot()
    {
        if (cameraRoot == null)
        {
            return null;
        }

        Camera childCamera = cameraRoot.GetComponentInChildren<Camera>(true);
        if (childCamera != null)
        {
            return childCamera.transform;
        }

        return cameraRoot;
    }

    private void DisableCinemachineOnPitchRig()
    {
        if (cameraRoot == null)
        {
            return;
        }

        CinemachineVirtualCamera virtualCamera = cameraRoot.GetComponent<CinemachineVirtualCamera>();
        if (virtualCamera != null)
        {
            virtualCamera.enabled = false;
        }

        CinemachineBrain brain = cameraRoot.GetComponentInChildren<CinemachineBrain>(true);
        if (brain != null)
        {
            brain.enabled = false;
        }
    }

    private void HideThirdPersonVisuals()
    {
        Transform protectedRoot = pitchRoot != null ? pitchRoot.root == transform.root ? GetDirectChildUnderRoot(pitchRoot) : null : null;

        foreach (Transform child in transform)
        {
            if (child == protectedRoot)
            {
                continue;
            }

            Renderer[] renderers = child.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = false;
            }
        }
    }

    private Transform GetDirectChildUnderRoot(Transform target)
    {
        Transform current = target;

        while (current != null && current.parent != transform)
        {
            current = current.parent;
        }

        return current;
    }
}
