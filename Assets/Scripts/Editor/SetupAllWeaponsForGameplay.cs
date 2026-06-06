using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// One-click setup tool to create all weapon prefabs and configure them for gameplay
/// </summary>
public static class SetupAllWeaponsForGameplay
{
    [MenuItem("Tools/Weapons/Setup All Weapons For Gameplay")]
    public static void SetupAllWeapons()
    {
        Debug.Log("=== Setting up all weapons for gameplay ===");
        
        // Step 1: Create all weapon prefabs
        Debug.Log("Step 1/3: Creating weapon prefabs...");
        CreateWeaponPrefabsTool.CreateAllWeaponPrefabs();

        // Step 2: Expand selected asset-pack prefabs into gameplay-ready loadout prefabs
        Debug.Log("Step 2/3: Expanding asset-pack weapons...");
        ExpandWeaponAssetsTool.ExpandAssetPackWeapons();
        
        // Step 3: Verify setup in the active scene
        Debug.Log("Step 3/3: Verifying weapon setup in active scene...");
        VerifyWeaponsInScene();
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("=== Weapon setup complete! ===");
        Debug.Log("All weapons have been configured:");
        Debug.Log("  1 (Rifle) - Primary");
        Debug.Log("  2 (Pistol) - Secondary");
        Debug.Log("  3 (Melee) - Not assigned yet");
        Debug.Log("\nYou can assign weapon prefabs to the WeaponManager in the Player object.");
    }

    private static void VerifyWeaponsInScene()
    {
        WeaponManager weaponManager = Object.FindObjectOfType<WeaponManager>();
        if (weaponManager == null)
        {
            Debug.LogWarning("No WeaponManager found in the scene. Create a Player with WeaponManager component first.");
            return;
        }

        // Load prefabs
        Weapon rifleWeapon = LoadWeaponPrefab("Rifle");
        Weapon pistolWeapon = LoadWeaponPrefab("Pistol");
        Weapon katanaWeapon = LoadExpandedWeaponPrefab("Expanded_Katana");

        if (rifleWeapon != null && weaponManager.primaryWeapon == null)
        {
            Debug.Log("Assigning Rifle to Primary slot...");
            weaponManager.primaryWeapon = rifleWeapon;
        }

        if (pistolWeapon != null && weaponManager.secondaryWeapon == null)
        {
            Debug.Log("Assigning Pistol to Secondary slot...");
            weaponManager.secondaryWeapon = pistolWeapon;
        }

        if (katanaWeapon != null && weaponManager.meleeWeapon == null)
        {
            Debug.Log("Assigning Katana to Melee slot...");
            weaponManager.meleeWeapon = katanaWeapon;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static Weapon LoadWeaponPrefab(string weaponName)
    {
        string prefabPath = $"Assets/Prefabs/Weapons/{weaponName}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            return null;
        }
        return prefab.GetComponent<Weapon>();
    }

    private static Weapon LoadExpandedWeaponPrefab(string weaponName)
    {
        string prefabPath = $"Assets/Prefabs/Weapons/Expanded/{weaponName}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            return null;
        }

        return prefab.GetComponent<Weapon>();
    }

    [MenuItem("Tools/Weapons/Auto-Configure All Weapons In Scene")]
    public static void AutoConfigureWeaponsInScene()
    {
        Debug.Log("Auto-configuring all weapons in the current scene...");
        
        // Find all Weapon components
        Weapon[] allWeapons = Object.FindObjectsOfType<Weapon>();
        Debug.Log($"Found {allWeapons.Length} weapons in scene.");

        foreach (Weapon weapon in allWeapons)
        {
            ConfigureWeapon(weapon);
        }

        // Find WeaponManager and verify setup
        WeaponManager manager = Object.FindObjectOfType<WeaponManager>();
        if (manager != null)
        {
            Debug.Log("Running WeaponManager auto-setup...");
            manager.GetType().GetMethod("AutoSetupWeapon", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(manager, new object[] { manager.primaryWeapon });
            manager.GetType().GetMethod("AutoSetupWeapon", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(manager, new object[] { manager.secondaryWeapon });
            manager.GetType().GetMethod("AutoSetupWeapon", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(manager, new object[] { manager.meleeWeapon });
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Weapon auto-configuration complete!");
    }

    private static void ConfigureWeapon(Weapon weapon)
    {
        if (weapon == null || weapon.weaponModel == null)
            return;

        if (weapon.slot == WeaponSlot.Melee)
        {
            GunShootTracer shooterOnMelee = weapon.weaponModel.GetComponent<GunShootTracer>();
            if (shooterOnMelee != null)
            {
                shooterOnMelee.enabled = false;
            }

            MeleeSlashAttack slash = weapon.weaponModel.GetComponent<MeleeSlashAttack>();
            if (slash == null)
            {
                slash = weapon.weaponModel.AddComponent<MeleeSlashAttack>();
            }

            slash.weaponVisual = weapon.weaponModel.transform;
            slash.idleEuler = weapon.equippedLocalEuler;
            return;
        }

        // Ensure GunShootTracer is present
        GunShootTracer shooter = weapon.weaponModel.GetComponent<GunShootTracer>();
        if (shooter == null)
        {
            shooter = weapon.weaponModel.AddComponent<GunShootTracer>();
            Debug.Log($"Added GunShootTracer to {weapon.name}");
        }

        // Configure firePoint if not already set
        if (shooter.firePoint == null)
        {
            shooter.firePoint = FindFirePoint(weapon.weaponModel.transform);
            if (shooter.firePoint != null)
            {
                Debug.Log($"  - Found FirePoint for {weapon.name}");
            }
        }

        // Make sure damage is set
        if (shooter.damage <= 0)
        {
            shooter.damage = 10f;
        }

        // Make sure range is set
        if (shooter.range <= 0)
        {
            shooter.range = 100f;
        }
    }

    private static Transform FindFirePoint(Transform weaponModel)
    {
        // Recursive search for FirePoint or Muzzle
        for (int i = 0; i < weaponModel.childCount; i++)
        {
            Transform child = weaponModel.GetChild(i);
            if (child.name == "FirePoint" || child.name == "Muzzle")
            {
                return child;
            }

            Transform found = FindFirePoint(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    [MenuItem("Tools/Weapons/List All Configured Weapons")]
    public static void ListAllWeapons()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Weapons" });
        
        Debug.Log($"\n=== Configured Weapons ({prefabGuids.Length} total) ===");
        
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Weapon weapon = prefab.GetComponent<Weapon>();
            
            if (weapon != null)
            {
                GunShootTracer shooter = prefab.GetComponentInChildren<GunShootTracer>();
                string hasShooter = shooter != null ? "✓ GunShootTracer" : "✗ No GunShootTracer";
                Debug.Log($"  {weapon.name} ({weapon.slot}) - {hasShooter}");
            }
        }
        
        Debug.Log("===================================\n");
    }
}
