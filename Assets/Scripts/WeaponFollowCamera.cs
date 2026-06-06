using UnityEngine;

public class WeaponFollowCamera : MonoBehaviour
{
    public Transform cameraTransform;
    public Vector3 positionOffset = new Vector3(0.18f, -0.18f, 0.35f);
    [Range(0f, 1f)] public float rotationFollow = 1f;

    void Awake()
    {
        if (positionOffset == Vector3.zero)
        {
            positionOffset = new Vector3(0.18f, -0.18f, 0.35f);
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null)
        {
            return;
        }

        transform.position = cameraTransform.position + cameraTransform.TransformVector(positionOffset);

        Quaternion targetRotation = cameraTransform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationFollow);
    }
}
