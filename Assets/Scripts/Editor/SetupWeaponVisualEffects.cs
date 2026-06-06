using UnityEngine;
using UnityEditor;

/// <summary>
/// Master tool to setup all visual effects for weapon shooting (tracers, muzzle flashes, etc.)
/// </summary>
public static class SetupWeaponVisualEffects
{
    [MenuItem("Tools/Weapons/Setup Shooting Effects (Tracers + Muzzle Flash)")]
    public static void SetupAllVisualEffects()
    {
        Debug.Log("=== Setting up weapon visual effects ===");

        // Step 1: Create tracer prefab
        Debug.Log("Step 1/4: Creating bullet tracer prefab...");
        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullets/BulletTracer.prefab") == null)
        {
            CreateTracerPrefab.CreateTracerPrefabAsset();
        }
        else
        {
            Debug.Log("  ✓ Bullet tracer prefab already exists");
        }

        // Step 2: Create muzzle flash prefab
        Debug.Log("Step 2/4: Creating muzzle flash prefab...");
        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/MuzzleFlash.prefab") == null)
        {
            CreateMuzzleFlashPrefab.CreateMuzzleFlashPrefabAsset();
        }
        else
        {
            Debug.Log("  ✓ Muzzle flash prefab already exists");
        }

        // Step 3: Assign tracer to weapons
        Debug.Log("Step 3/4: Assigning tracers to weapons...");
        CreateTracerPrefab.AssignTracerToAllWeapons();

        // Step 4: Assign muzzle flash to weapons
        Debug.Log("Step 4/4: Assigning muzzle flash to weapons...");
        CreateMuzzleFlashPrefab.AssignMuzzleFlashToAllWeapons();

        // Ensure weapon manager has default tracer
        WeaponManager manager = Object.FindObjectOfType<WeaponManager>();
        if (manager != null)
        {
            GameObject tracerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullets/BulletTracer.prefab");
            if (tracerPrefab != null && manager.defaultTracerPrefab == null)
            {
                manager.defaultTracerPrefab = tracerPrefab;
                EditorUtility.SetDirty(manager);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("=== Visual effects setup complete! ===");
        Debug.Log("✓ Bullet tracers configured - bullets will leave trails");
        Debug.Log("✓ Muzzle flashes configured - flash effect on each shot");
        Debug.Log("\nYou can now see shooting effects in-game!");
    }

    [MenuItem("Tools/Weapons/Show Visual Effects Setup Info")]
    public static void ShowSetupInfo()
    {
        string info = @"
WEAPON VISUAL EFFECTS SETUP GUIDE
==================================

This tool creates two types of visual feedback for shooting:

1. BULLET TRACERS
   - Glowing yellow projectiles that fly from gun to hit point
   - Shows players where shots are going
   - Component: BulletTracer.cs
   - Location: Assets/Prefabs/Bullets/BulletTracer.prefab

2. MUZZLE FLASHES  
   - Particle effect at the barrel when you shoot
   - Explosion of particles in the direction of fire
   - Component: ParticleSystem (on gun model)
   - Location: Assets/Prefabs/Effects/MuzzleFlash.prefab

SETUP STEPS:
1. Go to: Tools → Weapons → Setup Shooting Effects (Tracers + Muzzle Flash)
2. This will:
   - Create bullet tracer prefab if it doesn't exist
   - Create muzzle flash prefab if it doesn't exist
   - Assign both to all weapons
   
3. Play the game and shoot - you'll see:
   - Yellow/orange trails from your gun to hit targets
   - Particles bursting from barrel when you fire

CUSTOMIZE EFFECTS:
- Tracer: Select Assets/Prefabs/Bullets/BulletTracer.prefab
  > Edit speed (300 units/sec)
  > Edit maxLifeTime (1 second)
  > Edit mesh colors in the cylinder material

- Muzzle Flash: Select a weapon and find the MuzzleFlash particle system
  > Edit particle count, size, duration, colors
  > The preset uses yellow → red gradient

For more info on BulletTracer settings:
- speed: How fast the tracer travels (units per second)
- maxLifeTime: How long the tracer exists before disappearing
";
        Debug.Log(info);
    }
}
