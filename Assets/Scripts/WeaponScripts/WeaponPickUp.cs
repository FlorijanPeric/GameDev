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
        if (Camera.main == null)
        {
            return;
        }

        WeaponManager manager = FindObjectOfType<WeaponManager>();
        if (manager == null)
        {
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position,
                            Camera.main.transform.forward,
                            out hit, pickupRange))
        {
            WeaponPickup targetPickup = hit.transform.GetComponentInParent<WeaponPickup>();
            if (targetPickup == this && weapon != null)
            {
                manager.PickupWeapon(weapon);
                Destroy(gameObject);
            }
        }
    }
}
