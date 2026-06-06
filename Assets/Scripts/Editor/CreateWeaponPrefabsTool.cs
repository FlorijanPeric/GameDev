using UnityEngine;
using UnityEditor;

public class CreateWeaponPrefabsTool
{
    private static readonly string PREFABS_PATH = "Assets/Prefabs/Weapons";

    [MenuItem("Tools/Weapons/Create All Weapon Prefabs")]
    public static void CreateAllWeaponPrefabs()
    {
        // Create directory if it doesn't exist
        if (!AssetDatabase.IsValidFolder(PREFABS_PATH))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Weapons");
        }

        CreateWeaponPrefab("Rifle", "Assets/GunsAndShit/Low Poly Weapons VOL.1/Prefabs/M4_8.prefab", WeaponSlot.Primary, new Vector3(0.18f, -0.18f, 0.35f));
        CreateWeaponPrefab("Pistol", "Assets/GunsAndShit/FPS Gun Pack Vol. 1/Pistol 2/Prefabs/Pistol 2.prefab", WeaponSlot.Secondary, new Vector3(0.15f, -0.20f, 0.30f));
        CreateWeaponPrefab("Shotgun", "Assets/GunsAndShit/Low Poly Weapons VOL.1/Prefabs/Bennelli_M4.prefab", WeaponSlot.Primary, new Vector3(0.20f, -0.18f, 0.37f));
        CreateWeaponPrefab("SMG", "Assets/GunsAndShit/Low Poly Weapons VOL.1/Prefabs/Uzi.prefab", WeaponSlot.Primary, new Vector3(0.16f, -0.19f, 0.32f));
        CreateWeaponPrefab("AssaultRifle", "Assets/GunsAndShit/Low Poly Weapons VOL.1/Prefabs/AK74.prefab", WeaponSlot.Primary, new Vector3(0.19f, -0.18f, 0.36f));
        CreateWeaponPrefab("Sniper", "Assets/GunsAndShit/Low Poly Weapons VOL.1/Prefabs/M107.prefab", WeaponSlot.Primary, new Vector3(0.22f, -0.16f, 0.40f));
        CreateWeaponPrefab("LMG", "Assets/GunsAndShit/Low Poly Weapons VOL.1/Prefabs/M249.prefab", WeaponSlot.Primary, new Vector3(0.21f, -0.19f, 0.38f));
        CreateWeaponPrefab("RPG", "Assets/GunsAndShit/Low Poly Weapons VOL.1/Prefabs/RPG7.prefab", WeaponSlot.Primary, new Vector3(0.25f, -0.17f, 0.42f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All weapon prefabs created successfully!");
    }

    [MenuItem("Tools/Weapons/Create Only Missing Weapon Prefabs")]
    public static void CreateMissingWeaponPrefabs()
    {
        if (!AssetDatabase.IsValidFolder(PREFABS_PATH))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Weapons");
        }

        if (!AssetDatabase.LoadAssetAtPath($"{PREFABS_PATH}/Shotgun.prefab", typeof(GameObject)))
            CreateWeaponPrefab("Shotgun", "Assets/GunsAndShit/Low Poly Weapons VOL.1/Prefabs/Bennelli_M4.prefab", WeaponSlot.Primary, new Vector3(0.20f, -0.18f, 0.37f));

        if (!AssetDatabase.LoadAssetAtPath($"{PREFABS_PATH}/SMG.prefab", typeof(GameObject)))
            CreateWeaponPrefab("SMG", "Assets/GunsAndShit/Low Poly Weapons VOL.1/Prefabs/Uzi.prefab", WeaponSlot.Primary, new Vector3(0.16f, -0.19f, 0.32f));

        if (!AssetDatabase.LoadAssetAtPath($"{PREFABS_PATH}/AssaultRifle.prefab", typeof(GameObject)))
            CreateWeaponPrefab("AssaultRifle", "Assets/GunsAndShit/Low Poly Weapons VOL.1/Prefabs/AK74.prefab", WeaponSlot.Primary, new Vector3(0.19f, -0.18f, 0.36f));

        if (!AssetDatabase.LoadAssetAtPath($"{PREFABS_PATH}/Sniper.prefab", typeof(GameObject)))
            CreateWeaponPrefab("Sniper", "Assets/GunsAndShit/Low Poly Weapons VOL.1/Prefabs/M107.prefab", WeaponSlot.Primary, new Vector3(0.22f, -0.16f, 0.40f));

        if (!AssetDatabase.LoadAssetAtPath($"{PREFABS_PATH}/LMG.prefab", typeof(GameObject)))
            CreateWeaponPrefab("LMG", "Assets/GunsAndShit/Low Poly Weapons VOL.1/Prefabs/M249.prefab", WeaponSlot.Primary, new Vector3(0.21f, -0.19f, 0.38f));

        if (!AssetDatabase.LoadAssetAtPath($"{PREFABS_PATH}/RPG.prefab", typeof(GameObject)))
            CreateWeaponPrefab("RPG", "Assets/GunsAndShit/Low Poly Weapons VOL.1/Prefabs/RPG7.prefab", WeaponSlot.Primary, new Vector3(0.25f, -0.17f, 0.42f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Missing weapon prefabs created!");
    }

    private static void CreateWeaponPrefab(string weaponName, string modelAssetPath, WeaponSlot slot, Vector3 position)
    {
        // Load the model asset
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelAssetPath);
        if (modelAsset == null)
        {
            Debug.LogWarning($"Could not find model at {modelAssetPath}");
            return;
        }

        // Create a new container GameObject
        GameObject weaponContainer = new GameObject(weaponName);
        
        // Add Weapon component
        Weapon weaponComponent = weaponContainer.AddComponent<Weapon>();
        weaponComponent.slot = slot;
        weaponComponent.ApplyDefaultCombatProfile();
        
        // Add Rigidbody for dropping
        Rigidbody rb = weaponContainer.AddComponent<Rigidbody>();
        rb.mass = 2f;
        rb.linearDamping = 0.2f;
        rb.angularDamping = 0.2f;
        rb.constraints = RigidbodyConstraints.FreezeRotation; // Prevent spinning when dropped
        
        // Instantiate the model as a child
        GameObject modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
        if (modelInstance == null)
        {
            // If prefab instantiation fails, try direct instantiation
            modelInstance = Object.Instantiate(modelAsset);
        }
        
        modelInstance.name = "Model";
        modelInstance.transform.SetParent(weaponContainer.transform);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;

        // Set the weaponModel reference
        weaponComponent.weaponModel = modelInstance;
        
        // Configure positioning
        weaponComponent.equippedLocalPosition = position;
        weaponComponent.equippedLocalEuler = Vector3.zero;
        weaponComponent.equippedLocalScale = Vector3.one;

        // Add and configure GunShootTracer component on the model
        GunShootTracer shooter = modelInstance.GetComponent<GunShootTracer>();
        if (shooter == null)
        {
            shooter = modelInstance.AddComponent<GunShootTracer>();
        }

        // Configure GunShootTracer with default values
        shooter.damage = Mathf.Max(1f, weaponComponent.bodyDamage);
        shooter.range = 100f;
        shooter.fireRate = 0.1f;
        shooter.magazineSize = 30;
        shooter.reserveAmmo = 120;
        shooter.reloadDuration = 1.6f;

        // Try to find FirePoint in the model
        Transform firePoint = FindTransformByName(modelInstance.transform, "FirePoint");
        if (firePoint == null)
        {
            firePoint = FindTransformByName(modelInstance.transform, "Muzzle");
        }
        if (firePoint != null)
        {
            shooter.firePoint = firePoint;
        }

        // Create prefab asset
        string prefabPath = $"{PREFABS_PATH}/{weaponName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(weaponContainer, prefabPath);
        
        // Clean up temporary instantiation
        Object.DestroyImmediate(weaponContainer);
        
        Debug.Log($"Created {weaponName} prefab at {prefabPath}");
    }

    private static Transform FindTransformByName(Transform root, string targetName)
    {
        if (root == null) return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == targetName)
            {
                return child;
            }

            Transform found = FindTransformByName(child, targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
