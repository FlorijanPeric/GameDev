using UnityEngine;
using UnityEditor;

/// <summary>
/// Creates a bullet tracer prefab that shows visual feedback when shooting
/// </summary>
public static class CreateTracerPrefab
{
    private const string TRACER_PREFAB_PATH = "Assets/Prefabs/Bullets/BulletTracer.prefab";

    [MenuItem("Tools/Weapons/Create Bullet Tracer Prefab")]
    public static void CreateTracerPrefabAsset()
    {
        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Bullets"))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Bullets");
        }

        // Check if prefab already exists
        if (AssetDatabase.LoadAssetAtPath<GameObject>(TRACER_PREFAB_PATH) != null)
        {
            Debug.Log("Bullet tracer prefab already exists at " + TRACER_PREFAB_PATH);
            return;
        }

        // Create the tracer GameObject
        GameObject tracerObj = CreateTracerGameObject();

        // Save as prefab
        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(TRACER_PREFAB_PATH);
        PrefabUtility.SaveAsPrefabAsset(tracerObj, uniquePath);
        
        // Clean up
        Object.DestroyImmediate(tracerObj);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created bullet tracer prefab at {uniquePath}");
    }

    private static GameObject CreateTracerGameObject()
    {
        // Create main container
        GameObject tracer = new GameObject("BulletTracer");

        // Add BulletTracer component
        BulletTracer bulletTracer = tracer.AddComponent<BulletTracer>();
        bulletTracer.speed = 300f;
        bulletTracer.maxLifeTime = 1f;

        // Create visual - a line/trail renderer for velocity effect
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(tracer.transform);
        visual.transform.localPosition = Vector3.zero;

        // Add a simple cylinder to represent the tracer bullet
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = "TracerMesh";
        cylinder.transform.SetParent(visual.transform);
        cylinder.transform.localPosition = Vector3.zero;
        cylinder.transform.localScale = new Vector3(0.05f, 0.3f, 0.05f); // Thin and elongated

        // Remove collider from the visual
        Collider col = cylinder.GetComponent<Collider>();
        if (col != null)
        {
            Object.DestroyImmediate(col);
        }

        // Create glowing material for the tracer
        Material tracerMaterial = new Material(Shader.Find("Standard"));
        tracerMaterial.name = "TracerMaterial";
        tracerMaterial.color = new Color(1f, 0.8f, 0.1f, 1f); // Yellow-orange glow
        tracerMaterial.SetFloat("_Metallic", 0.8f);
        tracerMaterial.SetFloat("_Glossiness", 0.9f);
        
        Renderer rend = cylinder.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material = tracerMaterial;
        }

        // Add a Trail Renderer for extended visual effect
        TrailRenderer trail = visual.AddComponent<TrailRenderer>();
        trail.time = 0.1f;  // Short trail duration
        trail.startWidth = 0.08f;
        trail.endWidth = 0.02f;
        
        // Configure trail material
        Material trailMaterial = new Material(Shader.Find("Sprites/Default"));
        trailMaterial.color = new Color(1f, 1f, 0f, 0.8f); // Yellow trail
        trail.material = trailMaterial;

        return tracer;
    }

    [MenuItem("Tools/Weapons/Assign Tracer To All Weapons")]
    public static void AssignTracerToAllWeapons()
    {
        // Load the tracer prefab
        GameObject tracerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TRACER_PREFAB_PATH);
        if (tracerPrefab == null)
        {
            Debug.LogError($"Tracer prefab not found at {TRACER_PREFAB_PATH}. Create it first using 'Tools/Weapons/Create Bullet Tracer Prefab'");
            return;
        }

        // Find all weapon prefabs
        string[] weaponGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Weapons" });
        if (weaponGuids.Length == 0)
        {
            Debug.LogWarning("No weapon prefabs found. Create them first using 'Tools/Weapons/Create All Weapon Prefabs'");
            return;
        }

        int assignedCount = 0;
        foreach (string guid in weaponGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject weaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (weaponPrefab == null) continue;

            // Find GunShootTracer on the weapon or its children
            GunShootTracer[] shooters = weaponPrefab.GetComponentsInChildren<GunShootTracer>();
            foreach (GunShootTracer shooter in shooters)
            {
                if (shooter.tracerPrefab == null)
                {
                    shooter.tracerPrefab = tracerPrefab;
                    assignedCount++;
                    Debug.Log($"Assigned tracer to {weaponPrefab.name}");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Tracer assigned to {assignedCount} weapons!");
    }

    [MenuItem("Tools/Weapons/Assign Tracer To WeaponManager Default")]
    public static void AssignTracerToWeaponManager()
    {
        GameObject tracerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TRACER_PREFAB_PATH);
        if (tracerPrefab == null)
        {
            Debug.LogError($"Tracer prefab not found at {TRACER_PREFAB_PATH}");
            return;
        }

        WeaponManager manager = Object.FindObjectOfType<WeaponManager>();
        if (manager == null)
        {
            Debug.LogWarning("No WeaponManager found in scene. Add it to your Player object first.");
            return;
        }

        manager.defaultTracerPrefab = tracerPrefab;
        EditorUtility.SetDirty(manager);

        Debug.Log("Assigned tracer prefab to WeaponManager.defaultTracerPrefab");
    }

    [MenuItem("Tools/Weapons/Setup Tracers Complete")]
    public static void SetupTracersComplete()
    {
        Debug.Log("=== Setting up tracers ===");

        // Step 1: Create tracer prefab
        Debug.Log("Step 1/3: Creating tracer prefab...");
        CreateTracerPrefabAsset();

        // Wait a frame for assets to save
        EditorApplication.delayCall += () =>
        {
            // Step 2: Assign to all weapons
            Debug.Log("Step 2/3: Assigning tracer to all weapons...");
            AssignTracerToAllWeapons();

            EditorApplication.delayCall += () =>
            {
                // Step 3: Assign to WeaponManager default
                Debug.Log("Step 3/3: Assigning tracer to WeaponManager...");
                AssignTracerToWeaponManager();

                Debug.Log("=== Tracer setup complete! ===");
                Debug.Log("You should now see bullet trails when shooting!");
            };
        };
    }
}
