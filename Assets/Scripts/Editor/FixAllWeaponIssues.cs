using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Comprehensive fix for all weapon issues: positioning, tracers, fire points
/// </summary>
public static class FixAllWeaponIssues
{
    [MenuItem("Tools/Weapons/MASTER FIX: Position + Tracers + FirePoints")]
    public static void MasterFix()
    {
        Debug.Log("=== MASTER WEAPON FIX ===\n");

        // Step 1: Ensure tracer prefab exists
        Debug.Log("Step 1/5: Ensuring tracer prefab exists...");
        EnsureTracerPrefabExists();

        // Step 2: Assign tracer to WeaponManager
        Debug.Log("Step 2/5: Assigning tracer to WeaponManager...");
        AssignTracerToWeaponManager();

        // Step 3: Find and assign fire points
        Debug.Log("Step 3/5: Finding fire points in all weapons...");
        FindAndAssignFirePoints();

        // Step 4: Fix weapon positioning
        Debug.Log("Step 4/5: Fixing weapon positioning...");
        FixAllWeaponPositions();

        // Step 5: Assign tracer to all weapons
        Debug.Log("Step 5/5: Assigning tracer to weapons...");
        AssignTracerToAllWeapons();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("\n✓ MASTER FIX COMPLETE!");
        Debug.Log("Press Play to test:");
        Debug.Log("  1 - Primary weapon");
        Debug.Log("  2 - Pistol");
        Debug.Log("  3 - Katana");
        Debug.Log("  Click - Shoot with tracers");
    }

    private static void EnsureTracerPrefabExists()
    {
        string tracerPath = "Assets/Prefabs/Bullets/BulletTracer.prefab";
        GameObject tracerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tracerPath);
        
        if (tracerPrefab == null)
        {
            Debug.Log("  Creating tracer prefab...");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Bullets"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Bullets");
            }

            GameObject tracer = new GameObject("BulletTracer");

            // Add BulletTracer component
            BulletTracer bulletTracer = tracer.AddComponent<BulletTracer>();
            bulletTracer.speed = 300f;
            bulletTracer.maxLifeTime = 1f;

            // Create visual
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(tracer.transform);
            visual.transform.localPosition = Vector3.zero;

            // Add cylinder mesh
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "TracerMesh";
            cylinder.transform.SetParent(visual.transform);
            cylinder.transform.localScale = new Vector3(0.05f, 0.3f, 0.05f);

            Collider col = cylinder.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            Material tracerMaterial = new Material(Shader.Find("Standard"));
            tracerMaterial.color = new Color(1f, 0.8f, 0.1f, 1f);
            tracerMaterial.SetFloat("_Metallic", 0.8f);
            tracerMaterial.SetFloat("_Glossiness", 0.9f);

            Renderer rend = cylinder.GetComponent<Renderer>();
            if (rend != null) rend.material = tracerMaterial;

            TrailRenderer trail = visual.AddComponent<TrailRenderer>();
            trail.time = 0.1f;
            trail.startWidth = 0.08f;
            trail.endWidth = 0.02f;

            PrefabUtility.SaveAsPrefabAsset(tracer, tracerPath);
            Object.DestroyImmediate(tracer);

            Debug.Log($"  ✓ Tracer prefab created at {tracerPath}");
        }
        else
        {
            Debug.Log("  ✓ Tracer prefab already exists");
        }

        AssetDatabase.Refresh();
    }

    private static void AssignTracerToWeaponManager()
    {
        WeaponManager manager = Object.FindObjectOfType<WeaponManager>();
        if (manager == null)
        {
            Debug.LogWarning("  No WeaponManager found!");
            return;
        }

        GameObject tracerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullets/BulletTracer.prefab");
        if (tracerPrefab != null && manager.defaultTracerPrefab == null)
        {
            manager.defaultTracerPrefab = tracerPrefab;
            EditorUtility.SetDirty(manager);
            Debug.Log("  ✓ Assigned tracer to WeaponManager");
        }
    }

    private static void FindAndAssignFirePoints()
    {
        Weapon[] allWeapons = Object.FindObjectsOfType<Weapon>();
        
        foreach (Weapon weapon in allWeapons)
        {
            if (weapon.weaponModel == null) continue;

            GunShootTracer shooter = weapon.weaponModel.GetComponent<GunShootTracer>();
            if (shooter == null) continue;

            if (shooter.firePoint == null)
            {
                Transform firePoint = FindFirePointInModel(weapon.weaponModel.transform);
                if (firePoint != null)
                {
                    shooter.firePoint = firePoint;
                    EditorUtility.SetDirty(shooter);
                    Debug.Log($"  ✓ Found firePoint for {weapon.name}");
                }
                else
                {
                    Debug.LogWarning($"  ⚠ Could not find firePoint for {weapon.name}, using transform");
                    shooter.firePoint = weapon.weaponModel.transform;
                }
            }
        }
    }

    private static Transform FindFirePointInModel(Transform model)
    {
        // Recursive search for FirePoint or Muzzle
        for (int i = 0; i < model.childCount; i++)
        {
            Transform child = model.GetChild(i);
            if (child.name.Contains("FirePoint") || child.name.Contains("Muzzle") || child.name.Contains("fire") || child.name.Contains("muzzle"))
            {
                return child;
            }

            Transform found = FindFirePointInModel(child);
            if (found != null) return found;
        }
        return null;
    }

    private static void FixAllWeaponPositions()
    {
        WeaponManager manager = Object.FindObjectOfType<WeaponManager>();
        if (manager == null)
        {
            Debug.LogWarning("  No WeaponManager found!");
            return;
        }

        // Primary weapon positioning
        if (manager.primaryWeapon != null)
        {
            FixWeaponPosition(manager.primaryWeapon, new Vector3(0.18f, -0.18f, 0.35f), "Primary");
        }

        // Secondary weapon (pistol) positioning - smaller, lower, more to the right
        if (manager.secondaryWeapon != null)
        {
            FixWeaponPosition(manager.secondaryWeapon, new Vector3(0.15f, -0.22f, 0.25f), "Secondary (Pistol)");
        }

        // Melee weapon (katana) positioning - higher up on back, more to the left
        if (manager.meleeWeapon != null)
        {
            FixWeaponPosition(manager.meleeWeapon, new Vector3(-0.10f, 0.05f, 0.10f), "Melee (Katana)");
        }
    }

    private static void FixWeaponPosition(Weapon weapon, Vector3 position, string slotName)
    {
        weapon.equippedLocalPosition = position;
        weapon.equippedLocalEuler = Vector3.zero;
        weapon.equippedLocalScale = Vector3.one;

        if (weapon.weaponModel != null)
        {
            weapon.weaponModel.transform.localPosition = position;
            weapon.weaponModel.transform.localRotation = Quaternion.identity;
            weapon.weaponModel.transform.localScale = Vector3.one;

            if (!weapon.weaponModel.activeInHierarchy)
            {
                weapon.weaponModel.SetActive(true);
            }

            EditorUtility.SetDirty(weapon);
            Debug.Log($"  ✓ {slotName} positioned at {position}");
        }
    }

    private static void AssignTracerToAllWeapons()
    {
        GameObject tracerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullets/BulletTracer.prefab");
        if (tracerPrefab == null)
        {
            Debug.LogError("  ❌ Tracer prefab not found!");
            return;
        }

        Weapon[] allWeapons = Object.FindObjectsOfType<Weapon>();
        int assignedCount = 0;

        foreach (Weapon weapon in allWeapons)
        {
            if (weapon.weaponModel == null) continue;

            GunShootTracer shooter = weapon.weaponModel.GetComponent<GunShootTracer>();
            if (shooter == null) continue;

            if (shooter.tracerPrefab == null)
            {
                shooter.tracerPrefab = tracerPrefab;
                EditorUtility.SetDirty(shooter);
                assignedCount++;
            }
        }

        Debug.Log($"  ✓ Tracer assigned to {assignedCount} weapons");
    }

    [MenuItem("Tools/Weapons/Debug: Show All Weapon Info")]
    public static void DebugShowAllWeaponInfo()
    {
        WeaponManager manager = Object.FindObjectOfType<WeaponManager>();
        if (manager == null)
        {
            Debug.LogError("No WeaponManager found!");
            return;
        }

        Debug.Log("\n=== WEAPON DEBUG INFO ===\n");

        Debug.Log($"DEFAULT TRACER: {(manager.defaultTracerPrefab != null ? manager.defaultTracerPrefab.name : "NOT SET")}");
        Debug.Log($"WEAPON HOLDER: {(manager.weaponHolder != null ? manager.weaponHolder.name : "NOT SET")}\n");

        ShowWeaponInfo("PRIMARY", manager.primaryWeapon);
        ShowWeaponInfo("SECONDARY (PISTOL)", manager.secondaryWeapon);
        ShowWeaponInfo("MELEE (KATANA)", manager.meleeWeapon);

        Debug.Log("=== END DEBUG ===\n");
    }

    private static void ShowWeaponInfo(string slotName, Weapon weapon)
    {
        if (weapon == null)
        {
            Debug.LogWarning($"{slotName}: NOT ASSIGNED");
            return;
        }

        Debug.Log($"{slotName}:");
        Debug.Log($"  Name: {weapon.name}");
        Debug.Log($"  Position: {weapon.equippedLocalPosition}");
        Debug.Log($"  Model: {(weapon.weaponModel != null ? weapon.weaponModel.name : "NOT SET")}");
        Debug.Log($"  Model Active: {(weapon.weaponModel != null ? weapon.weaponModel.activeInHierarchy : false)}");

        if (weapon.weaponModel != null)
        {
            GunShootTracer shooter = weapon.weaponModel.GetComponent<GunShootTracer>();
            if (shooter != null)
            {
                Debug.Log($"  GunShootTracer: YES");
                Debug.Log($"    Tracer: {(shooter.tracerPrefab != null ? shooter.tracerPrefab.name : "NOT SET")}");
                Debug.Log($"    FirePoint: {(shooter.firePoint != null ? shooter.firePoint.name : "NOT SET")}");
                Debug.Log($"    Damage: {shooter.damage}");
            }
            else
            {
                Debug.LogWarning($"  GunShootTracer: NOT FOUND");
            }
        }

        Debug.Log("");
    }
}
