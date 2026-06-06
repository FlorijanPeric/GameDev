using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Diagnoses and fixes weapon positioning issues in the scene
/// </summary>
public static class FixWeaponPositioning
{
    [MenuItem("Tools/Weapons/Diagnose Weapon Positioning")]
    public static void DiagnoseWeaponPositioning()
    {
        WeaponManager manager = Object.FindObjectOfType<WeaponManager>();
        if (manager == null)
        {
            Debug.LogError("No WeaponManager found in scene!");
            return;
        }

        Debug.Log("=== WEAPON POSITIONING DIAGNOSIS ===\n");

        DiagnoseWeapon("Primary", manager.primaryWeapon, manager.weaponHolder);
        DiagnoseWeapon("Secondary (Pistol)", manager.secondaryWeapon, manager.weaponHolder);
        DiagnoseWeapon("Melee", manager.meleeWeapon, manager.weaponHolder);

        Debug.Log("\n=== END DIAGNOSIS ===");
    }

    private static void DiagnoseWeapon(string slotName, Weapon weapon, Transform weaponHolder)
    {
        if (weapon == null)
        {
            Debug.LogWarning($"  {slotName}: NOT ASSIGNED");
            return;
        }

        Debug.Log($"\n{slotName}: {weapon.name}");
        Debug.Log($"  GameObject: {weapon.gameObject.name}");
        Debug.Log($"  Weapon Model: {(weapon.weaponModel != null ? weapon.weaponModel.name : "NOT SET")}");
        
        if (weapon.weaponModel != null)
        {
            Debug.Log($"  Model Active: {weapon.weaponModel.activeInHierarchy}");
            Debug.Log($"  Model Position: {weapon.weaponModel.transform.localPosition}");
            Debug.Log($"  Equipped Position: {weapon.equippedLocalPosition}");
            Debug.Log($"  Equipped Euler: {weapon.equippedLocalEuler}");
        }

        GunShootTracer shooter = weapon.weaponModel != null ? weapon.weaponModel.GetComponent<GunShootTracer>() : null;
        if (shooter != null)
        {
            Debug.Log($"  GunShootTracer: Assigned");
            Debug.Log($"    Damage: {shooter.damage}");
            Debug.Log($"    FirePoint: {(shooter.firePoint != null ? shooter.firePoint.name : "NOT SET")}");
        }
        else
        {
            Debug.LogWarning("  GunShootTracer: NOT FOUND");
        }
    }

    [MenuItem("Tools/Weapons/Fix Pistol Positioning")]
    public static void FixPistolPositioning()
    {
        WeaponManager manager = Object.FindObjectOfType<WeaponManager>();
        if (manager == null)
        {
            Debug.LogError("No WeaponManager found!");
            return;
        }

        if (manager.secondaryWeapon == null)
        {
            Debug.LogError("No secondary weapon (Pistol) assigned to WeaponManager!");
            return;
        }

        Weapon pistol = manager.secondaryWeapon;

        Debug.Log($"Fixing pistol positioning for: {pistol.name}");

        // Set corrected values for pistol
        pistol.equippedLocalPosition = new Vector3(0.15f, 0.05f, 0.30f);  // Smaller, lower right
        pistol.equippedLocalEuler = Vector3.zero;
        pistol.equippedLocalScale = Vector3.one;

        if (pistol.weaponModel != null)
        {
            // Apply the positioning immediately
            pistol.weaponModel.transform.localPosition = pistol.equippedLocalPosition;
            pistol.weaponModel.transform.localRotation = Quaternion.Euler(pistol.equippedLocalEuler);
            pistol.weaponModel.transform.localScale = pistol.equippedLocalScale;

            // Ensure it's active
            if (!pistol.weaponModel.activeInHierarchy)
            {
                pistol.weaponModel.SetActive(true);
            }

            Debug.Log($"✓ Pistol positioning corrected to: {pistol.equippedLocalPosition}");
        }

        EditorUtility.SetDirty(pistol);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Weapons/Fix All Weapon Positioning")]
    public static void FixAllWeaponPositioning()
    {
        WeaponManager manager = Object.FindObjectOfType<WeaponManager>();
        if (manager == null)
        {
            Debug.LogError("No WeaponManager found!");
            return;
        }

        Debug.Log("=== FIXING ALL WEAPON POSITIONING ===");

        // Primary weapon (Rifle) - centered in front of player
        FixWeaponPose("Primary (Rifle)", manager.primaryWeapon, new Vector3(0.16f, -0.08f, 0.30f), Vector3.zero);

        // Secondary weapon (Pistol) - smaller, lower right
        FixWeaponPose("Secondary (Pistol)", manager.secondaryWeapon, new Vector3(0.14f, -0.10f, 0.26f), Vector3.zero);

        // Melee weapon (Katana/Sword) - rotate out of vertical orientation
        FixWeaponPose("Melee", manager.meleeWeapon, new Vector3(0.18f, -0.12f, 0.22f), new Vector3(0f, 0f, 90f));

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("=== ALL WEAPONS FIXED ===");
    }

    private static void FixWeaponPose(string slotName, Weapon weapon, Vector3 position, Vector3 euler)
    {
        if (weapon == null)
        {
            Debug.LogWarning($"{slotName}: NOT ASSIGNED");
            return;
        }

        weapon.equippedLocalPosition = position;
        weapon.equippedLocalEuler = euler;
        weapon.equippedLocalScale = Vector3.one;

        if (weapon.weaponModel != null)
        {
            weapon.weaponModel.transform.localPosition = position;
            weapon.weaponModel.transform.localRotation = Quaternion.Euler(euler);
            weapon.weaponModel.transform.localScale = Vector3.one;

            if (!weapon.weaponModel.activeInHierarchy)
            {
                weapon.weaponModel.SetActive(true);
            }

            Debug.Log($"✓ {slotName} fixed to position: {position}, rotation: {euler}");
            EditorUtility.SetDirty(weapon);
        }
    }

    [MenuItem("Tools/Weapons/Fix Melee Rotation (Katana)")]
    public static void FixMeleeRotation()
    {
        WeaponManager manager = Object.FindObjectOfType<WeaponManager>();
        if (manager == null)
        {
            Debug.LogError("No WeaponManager found!");
            return;
        }

        if (manager.meleeWeapon == null)
        {
            Debug.LogError("No melee weapon assigned to WeaponManager!");
            return;
        }

        FixWeaponPose("Melee", manager.meleeWeapon, new Vector3(0.18f, -0.12f, 0.22f), new Vector3(0f, 0f, 90f));
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Melee rotation fixed. Katana should no longer be vertical.");
    }

    [MenuItem("Tools/Weapons/Make All Weapons Visible")]
    public static void MakeAllWeaponsVisible()
    {
        Weapon[] allWeapons = Object.FindObjectsOfType<Weapon>();
        
        Debug.Log($"Found {allWeapons.Length} weapons in scene");

        foreach (Weapon weapon in allWeapons)
        {
            if (weapon.weaponModel != null)
            {
                if (!weapon.weaponModel.activeInHierarchy)
                {
                    weapon.weaponModel.SetActive(true);
                    Debug.Log($"  Activated: {weapon.name}");
                    EditorUtility.SetDirty(weapon);
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("All weapons made visible!");
    }

    [MenuItem("Tools/Weapons/Quick Fix: Pistol Not Visible")]
    public static void QuickFixPistol()
    {
        Debug.Log("=== QUICK FIX: PISTOL NOT VISIBLE ===");

        WeaponManager manager = Object.FindObjectOfType<WeaponManager>();
        if (manager == null)
        {
            Debug.LogError("❌ No WeaponManager found");
            return;
        }

        Weapon pistol = manager.secondaryWeapon;
        if (pistol == null)
        {
            Debug.LogError("❌ No secondary weapon (Pistol) assigned");
            return;
        }

        Debug.Log($"Found Pistol: {pistol.name}");

        // Step 1: Ensure weapon model is found
        if (pistol.weaponModel == null)
        {
            Debug.LogWarning("  Weapon model is null, trying to find it...");
            // Try to find model child
            if (pistol.transform.childCount > 0)
            {
                pistol.weaponModel = pistol.transform.GetChild(0).gameObject;
                Debug.Log($"  ✓ Found weapon model: {pistol.weaponModel.name}");
            }
            else
            {
                Debug.LogError("  ❌ Could not find weapon model");
                return;
            }
        }

        // Step 2: Make it visible
        pistol.weaponModel.SetActive(true);
        Debug.Log("  ✓ Weapon model activated");

        // Step 3: Set correct position
        pistol.equippedLocalPosition = new Vector3(0.14f, -0.10f, 0.26f);
        pistol.weaponModel.transform.localPosition = pistol.equippedLocalPosition;
        pistol.weaponModel.transform.localRotation = Quaternion.identity;
        Debug.Log($"  ✓ Position set to: {pistol.equippedLocalPosition}");

        // Step 4: Ensure it has shooting capability
        GunShootTracer shooter = pistol.weaponModel.GetComponent<GunShootTracer>();
        if (shooter == null)
        {
            shooter = pistol.weaponModel.AddComponent<GunShootTracer>();
            Debug.Log("  ✓ Added GunShootTracer component");
        }
        else
        {
            Debug.Log("  ✓ GunShootTracer already present");
        }

        if (shooter.damage <= 0)
        {
            shooter.damage = 10f;
            shooter.range = 100f;
            Debug.Log("  ✓ Configured shooter properties");
        }

        // Step 5: Mark as dirty
        EditorUtility.SetDirty(pistol);
        EditorUtility.SetDirty(shooter);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("\n✓ PISTOL SHOULD NOW BE VISIBLE AND WORKING!");
        Debug.Log("  Press 2 to switch to pistol in-game");
        Debug.Log("  Close and reopen the scene if needed");
    }
}
