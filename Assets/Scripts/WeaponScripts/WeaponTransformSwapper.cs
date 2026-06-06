using UnityEngine;

public class WeaponTransformSwapper : MonoBehaviour
{
    public Transform weaponHolder;   // CameraPivot/WeaponHolder

    public void SwapWeapon(Transform oldWeapon, Transform newWeapon)
    {
        // Parent new weapon first
        newWeapon.SetParent(weaponHolder);

        if (oldWeapon != null)
        {
            // Copy local transform from old weapon
            newWeapon.localPosition = oldWeapon.localPosition;
            newWeapon.localRotation = oldWeapon.localRotation;
            newWeapon.localScale = oldWeapon.localScale;
        }
        else
        {
            // Default fallback
            newWeapon.localPosition = Vector3.zero;
            newWeapon.localRotation = Quaternion.identity;
            newWeapon.localScale = Vector3.one;
        }

        newWeapon.gameObject.SetActive(true);

        if (oldWeapon != null)
            oldWeapon.gameObject.SetActive(false);
    }
}
