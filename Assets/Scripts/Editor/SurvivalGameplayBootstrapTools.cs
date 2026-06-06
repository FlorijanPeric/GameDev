using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SurvivalGameplayBootstrapTools
{
    private const string PickupRootName = "WeaponPickups_Auto";
    private const string ArmoryRootName = "WeaponArmory_Auto";
    private const string ArmoryPointsRootName = "WeaponArmoryPoints_Auto";
    private const string ZombieControllerPath = "Assets/ArtStore3D/Fat Zombie(Low Poly)/Anim/ZombieAnimator.controller";

    [MenuItem("Tools/Survival/Full Auto Setup")]
    public static void FullAutoSetup()
    {
        Debug.Log("=== Survival Full Auto Setup ===");

        // 1) Build/expand weapon prefabs and assign a usable loadout.
        ExpandWeaponAssetsTool.ExpandAndAssignLoadoutInScene();
        SetupAllWeaponsForGameplay.SetupAllWeapons();

        // 2) Configure enemy/wave runtime and support components.
        QuickStartEnemiesSpawnMoveAttack();
        EnsurePlayerCombatAnimationBridge();
        EnsureWeaponArmorySpawner();

        // 3) Ensure scene changes are persisted.
        MarkSceneDirtySafe();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Full Auto Setup complete. Enter Play mode to verify HUD health, waves, upgrades, and weapon loadout.");
    }

    [MenuItem("Tools/Academic/Prepare Survival FPS Demo")]
    public static void PrepareAcademicSurvivalDemo()
    {
        Debug.Log("=== Preparing academic survival FPS demo ===");

        SetupAllWeaponsForGameplay.SetupAllWeapons();
        QuickStartEnemiesSpawnMoveAttack();
        EnsurePlayerCombatAnimationBridge();
        EnsureWeaponArmorySpawner();

        MarkSceneDirtySafe();
        Debug.Log("Academic demo setup complete: weapons, animation bridge, armory, and horde systems are ready.");
    }

    [MenuItem("Tools/Weapons/Pickups/Scatter Random Pickups Around Map")]
    public static void ScatterRandomPickupsAroundMap()
    {
        List<GameObject> weaponPrefabs = LoadWeaponPrefabs();
        if (weaponPrefabs.Count == 0)
        {
            Debug.LogError("No weapon prefabs found in Assets/Prefabs/Weapons.");
            return;
        }

        Transform player = ResolvePlayerTransform();
        Vector3 center = player != null ? player.position : Vector3.zero;

        GameObject root = GetOrCreateRoot(PickupRootName);
        ClearChildren(root.transform);

        int count = 18;
        int created = 0;

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = weaponPrefabs[Random.Range(0, weaponPrefabs.Count)];
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                continue;
            }

            instance.transform.SetParent(root.transform, false);

            Vector3 spawnPosition = FindGroundSpawnPosition(center, 20f, 120f, 10f, 250f, i * 17 + 31);
            instance.transform.position = spawnPosition;
            instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            Weapon weapon = instance.GetComponent<Weapon>();
            if (weapon == null)
            {
                weapon = instance.AddComponent<Weapon>();
            }

            weapon.isEquipped = false;
            if (weapon.weaponModel != null)
            {
                weapon.weaponModel.SetActive(true);
            }

            WeaponPickup pickup = instance.GetComponent<WeaponPickup>();
            if (pickup == null)
            {
                pickup = instance.AddComponent<WeaponPickup>();
            }

            pickup.weapon = weapon;
            pickup.pickupRange = 3f;

            Collider col = instance.GetComponentInChildren<Collider>();
            if (col == null)
            {
                SphereCollider sphere = instance.AddComponent<SphereCollider>();
                sphere.radius = 0.35f;
                sphere.center = Vector3.up * 0.35f;
            }

            Rigidbody rb = instance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            created++;
        }

        MarkSceneDirtySafe();
        Debug.Log($"Spawned {created} random weapon pickups under '{PickupRootName}'.");
    }

    [MenuItem("Tools/Survival/Quick Start Enemies (Spawn Move Attack)")]
    public static void QuickStartEnemiesSpawnMoveAttack()
    {
        SurvivalEnemySpawner spawner = Object.FindObjectOfType<SurvivalEnemySpawner>();
        if (spawner == null)
        {
            GameObject go = new GameObject("SurvivalEnemySpawner");
            spawner = go.AddComponent<SurvivalEnemySpawner>();
        }

        spawner.player = ResolvePlayerTransform();

        SurvivalSpawnPoint[] scenePoints = Object.FindObjectsOfType<SurvivalSpawnPoint>(true);
        List<Transform> pointList = new List<Transform>();
        for (int i = 0; i < scenePoints.Length; i++)
        {
            if (scenePoints[i] != null)
            {
                pointList.Add(scenePoints[i].transform);
            }
        }

        spawner.spawnPoints = pointList.ToArray();
        spawner.enemyPrefabs = LoadEnemyPrefabs().ToArray();
        AssignDefaultZombieAnimatorController(spawner);
        spawner.autoStartSpawnLoop = false;

        SurvivalWaveDirector director = Object.FindObjectOfType<SurvivalWaveDirector>();
        if (director == null)
        {
            GameObject d = new GameObject("SurvivalWaveDirector");
            director = d.AddComponent<SurvivalWaveDirector>();
        }

        director.spawner = spawner;
        director.autoStart = true;

        SetDirtySafe(spawner);
        SetDirtySafe(director);
        MarkSceneDirtySafe();

        if (spawner.enemyPrefabs == null || spawner.enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("Quick Start configured but no enemy prefabs were found. Add enemy prefabs with names including zombie/enemy.");
            return;
        }

        if (spawner.spawnPoints == null || spawner.spawnPoints.Length == 0)
        {
            Debug.LogWarning("Quick Start configured but no SurvivalSpawnPoint objects were found.");
            return;
        }

        if (Application.isPlaying)
        {
            director.StartDirector();
        }

        Debug.Log($"Survival enemies ready. Prefabs={spawner.enemyPrefabs.Length}, SpawnPoints={spawner.spawnPoints.Length}. Enter Play mode to start spawning.");
    }

    private static List<GameObject> LoadWeaponPrefabs()
    {
        List<GameObject> result = new List<GameObject>();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Weapons" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.GetComponent<Weapon>() != null)
            {
                result.Add(prefab);
            }
        }

        return result;
    }

    private static List<GameObject> LoadEnemyPrefabs()
    {
        List<GameObject> found = new List<GameObject>();
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            string lower = path.ToLowerInvariant();
            bool looksLikeEnemy =
                lower.Contains("zombie") ||
                lower.Contains("enemy") ||
                lower.Contains("undead") ||
                lower.Contains("monster");

            if (!looksLikeEnemy)
            {
                continue;
            }

            if (lower.Contains("icon") || lower.Contains("lod") || lower.Contains("pose"))
            {
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                found.Add(prefab);
            }
        }

        return found;
    }

    private static void AssignDefaultZombieAnimatorController(SurvivalEnemySpawner spawner)
    {
        if (spawner == null || spawner.defaultZombieAnimatorController != null)
        {
            return;
        }

        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ZombieControllerPath);
        if (controller == null)
        {
            return;
        }

        spawner.defaultZombieAnimatorController = controller;
    }

    private static Transform ResolvePlayerTransform()
    {
        WeaponManager manager = Object.FindObjectOfType<WeaponManager>();
        if (manager != null)
        {
            return manager.transform;
        }

        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null)
        {
            return tagged.transform;
        }

        return null;
    }

    private static GameObject GetOrCreateRoot(string name)
    {
        GameObject root = GameObject.Find(name);
        if (root == null)
        {
            root = new GameObject(name);
            if (!EditorApplication.isPlaying)
            {
                Undo.RegisterCreatedObjectUndo(root, "Create pickup root");
            }
        }

        return root;
    }

    private static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            if (EditorApplication.isPlaying)
            {
                Object.Destroy(root.GetChild(i).gameObject);
            }
            else
            {
                Object.DestroyImmediate(root.GetChild(i).gameObject);
            }
        }
    }

    private static Vector3 FindGroundSpawnPosition(Vector3 center, float minRadius, float maxRadius, float castHeight, float maxCastDistance, int seed)
    {
        Random.State prev = Random.state;
        Random.InitState(seed);

        Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(minRadius, maxRadius);
        Vector3 start = new Vector3(center.x + circle.x, center.y + castHeight, center.z + circle.y);

        Random.state = prev;

        RaycastHit hit;
        if (Physics.Raycast(start, Vector3.down, out hit, maxCastDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * 0.25f;
        }

        return new Vector3(start.x, center.y + 0.25f, start.z);
    }

    private static void EnsurePlayerCombatAnimationBridge()
    {
        WeaponManager manager = Object.FindObjectOfType<WeaponManager>();
        if (manager == null)
        {
            Debug.LogWarning("No WeaponManager found for animation bridge setup.");
            return;
        }

        PlayerCombatAnimationBridge bridge = manager.GetComponent<PlayerCombatAnimationBridge>();
        if (bridge == null)
        {
            if (EditorApplication.isPlaying)
            {
                bridge = manager.gameObject.AddComponent<PlayerCombatAnimationBridge>();
            }
            else
            {
                bridge = Undo.AddComponent<PlayerCombatAnimationBridge>(manager.gameObject);
            }
        }

        bridge.weaponManager = manager;
        if (bridge.animator == null)
        {
            bridge.animator = manager.GetComponentInChildren<Animator>(true);
        }

        SetDirtySafe(bridge);
    }

    private static void EnsureWeaponArmorySpawner()
    {
        List<GameObject> weaponPrefabs = LoadWeaponPrefabs();
        if (weaponPrefabs.Count == 0)
        {
            Debug.LogWarning("No weapon prefabs found for armory setup.");
            return;
        }

        Transform player = ResolvePlayerTransform();
        Vector3 center = player != null ? player.position : Vector3.zero;

        GameObject armoryRoot = GetOrCreateRoot(ArmoryRootName);
        WeaponArmorySpawner armory = armoryRoot.GetComponent<WeaponArmorySpawner>();
        if (armory == null)
        {
            if (EditorApplication.isPlaying)
            {
                armory = armoryRoot.AddComponent<WeaponArmorySpawner>();
            }
            else
            {
                armory = Undo.AddComponent<WeaponArmorySpawner>(armoryRoot);
            }
        }

        GameObject pointsRoot = GetOrCreateRoot(ArmoryPointsRootName);
        if (pointsRoot.transform.childCount == 0)
        {
            BuildDefaultArmoryPoints(pointsRoot.transform, center, Mathf.Max(weaponPrefabs.Count, 6));
        }

        List<Transform> points = new List<Transform>();
        for (int i = 0; i < pointsRoot.transform.childCount; i++)
        {
            points.Add(pointsRoot.transform.GetChild(i));
        }

        armory.weaponPrefabs = weaponPrefabs.ToArray();
        armory.spawnPoints = points.ToArray();
        armory.spawnOnStart = true;
        armory.clearPreviouslySpawned = true;
        armory.snapToGround = true;

        SetDirtySafe(armory);
    }

    private static void BuildDefaultArmoryPoints(Transform pointsRoot, Vector3 center, int count)
    {
        float radius = 8f;
        int safeCount = Mathf.Max(4, count);

        for (int i = 0; i < safeCount; i++)
        {
            float t = (float)i / safeCount;
            float angle = t * Mathf.PI * 2f;

            Vector3 pos = center + new Vector3(Mathf.Cos(angle) * radius, 2f, Mathf.Sin(angle) * radius);
            RaycastHit hit;
            if (Physics.Raycast(pos, Vector3.down, out hit, 20f, ~0, QueryTriggerInteraction.Ignore))
            {
                pos = hit.point + Vector3.up * 0.1f;
            }

            GameObject point = new GameObject("ArmoryPoint_" + (i + 1).ToString("00"));
            if (!EditorApplication.isPlaying)
            {
                Undo.RegisterCreatedObjectUndo(point, "Create armory point");
            }
            point.transform.SetParent(pointsRoot, false);
            point.transform.position = pos;
            point.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
    }

    private static void MarkSceneDirtySafe()
    {
        if (EditorApplication.isPlaying)
        {
            return;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static void SetDirtySafe(Object obj)
    {
        if (obj == null || EditorApplication.isPlaying)
        {
            return;
        }

        EditorUtility.SetDirty(obj);
    }
}
