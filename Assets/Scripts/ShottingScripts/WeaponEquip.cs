using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponEquip : MonoBehaviour
{
    public Camera firstPersonCamera;
    public Camera thirdPersonCamera;

    public GameObject weapon;
    public Transform weaponHolder;

    private bool equipped = false;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (!equipped)
                Equip();
            else
                Unequip();
        }
    }

    void Equip()
    {
        equipped = true;

        firstPersonCamera.enabled = true;
        thirdPersonCamera.enabled = false;

        weapon.SetActive(true);
        weapon.transform.SetParent(weaponHolder);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }

    void Unequip()
    {
        equipped = false;

        firstPersonCamera.enabled = false;
        thirdPersonCamera.enabled = true;

        weapon.SetActive(false);
    }
}
