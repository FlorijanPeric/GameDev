using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ExpandWeaponAssetsTool
{
    private const string ExpandedFolder = "Assets/Prefabs/Weapons/Expanded";

    private struct WeaponSource
    {
        public string outputName;
        public string sourcePrefabPath;
        public WeaponSlot slot;
        public bool melee;
        public Vector3 equippedLocalPosition;
        public Vector3 equippedLocalEuler;
        public Vector3 equippedLocalScale;
        public float damage;
        public float fireRate;
        public int magazine;
        public int reserve;
    }

    private static readonly WeaponSource[] Sources =
    {
        new WeaponSource
        {
            outputName = "Expanded_M4",
            sourcePrefabPath = "Assets/GunsAndShit/Low Poly Weapons VOL.1/Prefabs/M4_8.prefab",
            slot = WeaponSlot.Primary,
            melee = false,
            equippedLocalPosition = new Vector3(0.18f, -0.18f, 0.35f),
            equippedLocalEuler = Vector3.zero,
            equippedLocalScale = Vector3.one,
            damage = 10f,
            fireRate = 0.095f,
            magazine = 30,
            reserve = 150
        },
        new WeaponSource
        {
            outputName = "Expanded_Pistol",
            sourcePrefabPath = "Assets/GunsAndShit/FPS Gun Pack Vol. 1/Pistol 2/Prefabs/Pistol 2.prefab",
            slot = WeaponSlot.Secondary,
            melee = false,
            equippedLocalPosition = new Vector3(0.15f, -0.2f, 0.3f),
            equippedLocalEuler = new Vector3(0f, -10f, 0f),
            equippedLocalScale = Vector3.one,
            damage = 20f,
            fireRate = 0.2f,
            magazine = 15,
            reserve = 90
        },
        new WeaponSource
        {
            outputName = "Expanded_AutoRifle",
            sourcePrefabPath = "Assets/GunsAndShit/Easy FPS/Resources/NewGun_auto.prefab",
            slot = WeaponSlot.Primary,
            melee = false,
            equippedLocalPosition = new Vector3(0.18f, -0.17f, 0.33f),
            equippedLocalEuler = Vector3.zero,
            equippedLocalScale = Vector3.one,
            damage = 10f,
            fireRate = 0.085f,
            magazine = 35,
            reserve = 180
        },
        new WeaponSource
        {
            outputName = "Expanded_Katana",
            sourcePrefabPath = "Assets/GunsAndShit/JTS_TheSamuraiSword/Prefabs/Katana_Generic01.prefab",
            slot = WeaponSlot.Melee,
            melee = true,
            equippedLocalPosition = new Vector3(0.17f, -0.18f, 0.33f),
            equippedLocalEuler = new Vector3(0f, -10f, 0f),
            equippedLocalScale = Vector3.one,
            damage = 50f,
            fireRate = 0f,
            magazine = 0,
            reserve = 0
        }
    };

    [MenuItem("Tools/Weapons/Expand Asset Pack Weapons")]
    public static void ExpandAssetPackWeapons()
    {
        EnsureExpandedFolder();

        int created = 0;
        int skipped = 0;

        for (int i = 0; i < Sources.Length; i++)
        {
            if (CreateExpandedPrefab(Sources[i]))
            {
                created++;
            }
            else
            {
                skipped++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"ExpandWeaponAssetsTool: Created/updated {created} prefabs, skipped {skipped} missing sources.");
    }

    [MenuItem("Tools/Weapons/Expand And Assign Loadout In Scene")]
    public static void ExpandAndAssignLoadoutInScene()
    {
        ExpandAssetPackWeapons();
        AssignExpandedLoadoutInScene();
    }

    [MenuItem("Tools/Weapons/Assign Expanded Loadout In Scene")]
    public static void AssignExpandedLoadoutInScene()
    {
        WeaponManager manager = Object.FindObjectOfType<WeaponManager>();
        if (manager == null)
        {
            Debug.LogWarning("ExpandWeaponAssetsTool: No WeaponManager found in active scene.");
            return;
        }

        Weapon primary = LoadExpandedWeapon("Expanded_M4");
        Weapon secondary = LoadExpandedWeapon("Expanded_Pistol");
        Weapon melee = LoadExpandedWeapon("Expanded_Katana");

        if (primary != null)
        {
            manager.primaryWeapon = primary;
        }

        if (secondary != null)
        {
            manager.secondaryWeapon = secondary;
        }

        if (melee != null)
        {
            manager.meleeWeapon = melee;
        }

        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("ExpandWeaponAssetsTool: Assigned expanded loadout to WeaponManager (Primary/Secondary/Melee).");
    }

    private static bool CreateExpandedPrefab(WeaponSource source)
    {
        GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(source.sourcePrefabPath);
        if (sourcePrefab == null)
        {
            Debug.LogWarning("ExpandWeaponAssetsTool: Missing source prefab at " + source.sourcePrefabPath);
            return false;
        }

        GameObject root = new GameObject(source.outputName);

        Weapon weapon = root.AddComponent<Weapon>();
        weapon.slot = source.slot;
        weapon.equippedLocalPosition = source.equippedLocalPosition;
        weapon.equippedLocalEuler = source.equippedLocalEuler;
        weapon.equippedLocalScale = source.equippedLocalScale;
        weapon.ApplyDefaultCombatProfile();

        WeaponPickup pickup = root.AddComponent<WeaponPickup>();
        pickup.pickupRange = 3f;

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        GameObject modelInstance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
        if (modelInstance == null)
        {
            modelInstance = Object.Instantiate(sourcePrefab);
        }

        modelInstance.name = "Model";
        modelInstance.transform.SetParent(root.transform, false);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;

        weapon.weaponModel = modelInstance;
        pickup.weapon = weapon;

        if (source.melee)
        {
            SetupMeleeWeapon(modelInstance, weapon);
        }
        else
        {
            SetupFirearm(modelInstance, source);
        }

        EnsurePickupCollider(root, modelInstance);

        string savePath = ExpandedFolder + "/" + source.outputName + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, savePath);
        Object.DestroyImmediate(root);
        return true;
    }

    private static void SetupFirearm(GameObject modelInstance, WeaponSource source)
    {
        MeleeSlashAttack oldMelee = modelInstance.GetComponent<MeleeSlashAttack>();
        if (oldMelee != null)
        {
            oldMelee.enabled = false;
        }

        GunShootTracer shooter = modelInstance.GetComponent<GunShootTracer>();
        if (shooter == null)
        {
            shooter = modelInstance.AddComponent<GunShootTracer>();
        }

        shooter.damage = Mathf.Max(1f, source.damage);
        shooter.fireRate = Mathf.Max(0.02f, source.fireRate);
        shooter.magazineSize = Mathf.Max(1, source.magazine);
        shooter.reserveAmmo = Mathf.Max(0, source.reserve);
        shooter.reloadDuration = 1.5f;
        shooter.autoReloadWhenEmpty = true;

        if (shooter.firePoint == null)
        {
            shooter.firePoint = FindChildByName(modelInstance.transform, "FirePoint");
            if (shooter.firePoint == null)
            {
                shooter.firePoint = FindChildByName(modelInstance.transform, "Muzzle");
            }
        }
    }

    private static void SetupMeleeWeapon(GameObject modelInstance, Weapon weapon)
    {
        GunShootTracer shooter = modelInstance.GetComponent<GunShootTracer>();
        if (shooter != null)
        {
            shooter.enabled = false;
        }

        MeleeSlashAttack slash = modelInstance.GetComponent<MeleeSlashAttack>();
        if (slash == null)
        {
            slash = modelInstance.AddComponent<MeleeSlashAttack>();
        }

        slash.weaponVisual = modelInstance.transform;
        slash.idleEuler = weapon.equippedLocalEuler;
        slash.damage = Mathf.Max(1f, weapon.meleeDamage);
    }

    private static void EnsurePickupCollider(GameObject root, GameObject modelInstance)
    {
        Collider collider = root.GetComponent<Collider>();
        if (collider != null)
        {
            return;
        }

        Bounds bounds = new Bounds(root.transform.position, Vector3.one * 0.4f);
        Renderer[] renderers = modelInstance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        BoxCollider box = root.AddComponent<BoxCollider>();
        Vector3 localCenter = root.transform.InverseTransformPoint(bounds.center);
        box.center = localCenter;
        box.size = new Vector3(
            Mathf.Max(0.2f, bounds.size.x),
            Mathf.Max(0.2f, bounds.size.y),
            Mathf.Max(0.2f, bounds.size.z));
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == name)
            {
                return child;
            }

            Transform found = FindChildByName(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Weapon LoadExpandedWeapon(string prefabName)
    {
        string path = ExpandedFolder + "/" + prefabName + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            return null;
        }

        return prefab.GetComponent<Weapon>();
    }

    private static void EnsureExpandedFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Weapons"))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Weapons");
        }

        if (!AssetDatabase.IsValidFolder(ExpandedFolder))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs/Weapons", "Expanded");
        }
    }
}
