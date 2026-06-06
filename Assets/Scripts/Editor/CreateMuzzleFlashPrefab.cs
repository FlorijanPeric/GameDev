using UnityEngine;
using UnityEditor;

/// <summary>
/// Creates MuzzleFlash particle system prefab for visual gun effects
/// </summary>
public static class CreateMuzzleFlashPrefab
{
    private const string MUZZLE_FLASH_PATH = "Assets/Prefabs/Effects/MuzzleFlash.prefab";

    [MenuItem("Tools/Weapons/Create Muzzle Flash Prefab")]
    public static void CreateMuzzleFlashPrefabAsset()
    {
        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Effects"))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Effects");
        }

        // Check if prefab already exists
        if (AssetDatabase.LoadAssetAtPath<GameObject>(MUZZLE_FLASH_PATH) != null)
        {
            Debug.Log("Muzzle flash prefab already exists at " + MUZZLE_FLASH_PATH);
            return;
        }

        // Create the muzzle flash GameObject
        GameObject muzzleFlashObj = CreateMuzzleFlashGameObject();

        // Save as prefab
        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(MUZZLE_FLASH_PATH);
        PrefabUtility.SaveAsPrefabAsset(muzzleFlashObj, uniquePath);
        
        // Clean up
        Object.DestroyImmediate(muzzleFlashObj);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created muzzle flash prefab at {uniquePath}");
    }

    private static GameObject CreateMuzzleFlashGameObject()
    {
        GameObject muzzleFlash = new GameObject("MuzzleFlash");

        // Create a simple particle system for the flash effect
        GameObject particleObj = new GameObject("FlashParticles");
        particleObj.transform.SetParent(muzzleFlash.transform);
        particleObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
        
        // Configure particle system for muzzle flash
        var main = ps.main;
        main.duration = 0.1f;
        main.loop = false;
        main.startLifetime = 0.1f;
        main.startSpeed = 5f;
        main.startSize = 0.3f;
        main.maxParticles = 20;

        var emission = ps.emission;
        emission.rateOverTime = 100f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.yellow, 0f), new GradientColorKey(Color.red, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        // Disable auto-destroy for now (it will be destroyed after a short time)
        ps.Stop();

        return muzzleFlash;
    }

    [MenuItem("Tools/Weapons/Assign Muzzle Flash To All Weapons")]
    public static void AssignMuzzleFlashToAllWeapons()
    {
        // Load the muzzle flash prefab
        GameObject muzzleFlashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MUZZLE_FLASH_PATH);
        if (muzzleFlashPrefab == null)
        {
            Debug.LogError($"Muzzle flash prefab not found at {MUZZLE_FLASH_PATH}. Create it first.");
            return;
        }

        // Find all weapon prefabs
        string[] weaponGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Weapons" });
        if (weaponGuids.Length == 0)
        {
            Debug.LogWarning("No weapon prefabs found.");
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
                if (shooter.muzzleFlash == null)
                {
                    // Instantiate the muzzle flash as a child of the weapon
                    GameObject muzzleInstance = PrefabUtility.InstantiatePrefab(muzzleFlashPrefab) as GameObject;
                    muzzleInstance.name = "MuzzleFlash";
                    muzzleInstance.transform.SetParent(shooter.transform);
                    muzzleInstance.transform.localPosition = Vector3.zero;
                    muzzleInstance.transform.localRotation = Quaternion.identity;

                    ParticleSystem ps = muzzleInstance.GetComponent<ParticleSystem>() ?? muzzleInstance.GetComponentInChildren<ParticleSystem>();
                    if (ps != null)
                    {
                        shooter.muzzleFlash = ps;
                        assignedCount++;
                        Debug.Log($"Assigned muzzle flash to {weaponPrefab.name}");
                    }

                    Object.DestroyImmediate(muzzleInstance);
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Muzzle flash effect configured for {assignedCount} weapons!");
    }
}
