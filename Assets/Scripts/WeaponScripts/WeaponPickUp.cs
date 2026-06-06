using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponPickup : MonoBehaviour
{
    public Weapon weapon;
    public float pickupRange = 2f;

    void Awake()
    {
        if (weapon == null)
        {
            weapon = GetComponent<Weapon>();
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            TryPickup();
        }
    }

    void TryPickup()
    {
        if (weapon == null || Camera.main == null) return;

        WeaponManager manager = FindObjectOfType<WeaponManager>();
        if (manager == null) return;

        Vector3 camPos = Camera.main.transform.position;

        // Distance check — no collider required
        if (Vector3.Distance(transform.position, camPos) > pickupRange) return;

        // Facing check — player must be roughly looking toward the weapon
        Vector3 toWeapon = (transform.position - camPos).normalized;
        if (Vector3.Dot(Camera.main.transform.forward, toWeapon) < 0.4f) return;

        manager.PickupWeapon(weapon);
        Destroy(gameObject);
    }
}
