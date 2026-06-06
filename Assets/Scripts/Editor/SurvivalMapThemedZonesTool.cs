using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SurvivalMapThemedZonesTool
{
    private const string ZoneRootName = "ThemedZones_Auto";

    public enum ZoneType
    {
        Industrial,
        ChokePoint,
        OpenKillField,
        CoverCastle,
        MixedTerrain
    }

    [System.Serializable]
    private struct ZoneConfig
    {
        public ZoneType type;
        public Vector3 center;
        public float radius;
        public int propCount;
        public float propDensity;
        public float spawnIntensity;
        public string description;
    }

    [MenuItem("Tools/Map/Survival/Add Themed Zones")]
    public static void AddThemedZones()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("No active scene found.");
            return;
        }

        GameObject zoneRoot = GetOrCreateRoot(ZoneRootName);

        // Define zone layout (adjust positions for your map)
        ZoneConfig[] zones = new ZoneConfig[]
        {
            new ZoneConfig
            {
                type = ZoneType.OpenKillField,
                center = new Vector3(0f, 0f, -80f),
                radius = 60f,
                propCount = 20,
                propDensity = 0.3f,
                spawnIntensity = 1.3f,
                description = "Open arena - high visibility, lots of spawning"
            },
            new ZoneConfig
            {
                type = ZoneType.Industrial,
                center = new Vector3(80f, 0f, 0f),
                radius = 50f,
                propCount = 70,
                propDensity = 1.2f,
                spawnIntensity = 0.9f,
                description = "Industrial cluster - tight cover, moderate spawning"
            },
            new ZoneConfig
            {
                type = ZoneType.CoverCastle,
                center = new Vector3(-80f, 0f, 0f),
                radius = 45f,
                propCount = 100,
                propDensity = 1.5f,
                spawnIntensity = 0.8f,
                description = "Heavy cover area - defendable but slow respawn"
            },
            new ZoneConfig
            {
                type = ZoneType.ChokePoint,
                center = new Vector3(0f, 0f, 60f),
                radius = 35f,
                propCount = 45,
                propDensity = 1.0f,
                spawnIntensity = 1.2f,
                description = "Funnel zone - corridor play, medium pressure"
            },
            new ZoneConfig
            {
                type = ZoneType.MixedTerrain,
                center = new Vector3(-50f, 0f, -50f),
                radius = 40f,
                propCount = 55,
                propDensity = 0.9f,
                spawnIntensity = 1.0f,
                description = "Balanced zone - mix of open and cover"
            },
            new ZoneConfig
            {
                type = ZoneType.MixedTerrain,
                center = new Vector3(50f, 0f, -50f),
                radius = 40f,
                propCount = 55,
                propDensity = 0.9f,
                spawnIntensity = 1.0f,
                description = "Balanced zone - mix of open and cover"
            }
        };

        foreach (ZoneConfig zone in zones)
        {
            CreateThemedZone(zoneRoot.transform, zone);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"Added {zones.Length} themed zones. Map now has strategic flow and varied difficulty.");
    }

    [MenuItem("Tools/Map/Survival/Clear Themed Zones")]
    public static void ClearThemedZones()
    {
        GameObject root = GameObject.Find(ZoneRootName);
        if (root == null)
        {
            Debug.Log("No themed zones found to clear.");
            return;
        }

        Undo.DestroyObjectImmediate(root);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Cleared all themed zones.");
    }

    private static GameObject GetOrCreateRoot(string name)
    {
        GameObject root = GameObject.Find(name);
        if (root != null)
        {
            return root;
        }

        root = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(root, "Create themed zones root");
        return root;
    }

    private static void CreateThemedZone(Transform parent, ZoneConfig zone)
    {
        GameObject zoneGO = new GameObject($"{zone.type}_Zone");
        Undo.RegisterCreatedObjectUndo(zoneGO, "Create themed zone");
        zoneGO.transform.SetParent(parent);
        zoneGO.transform.localPosition = zone.center;

        // Add intensity zone component for spawn tuning
        SurvivalSpawnIntensityZone intensityZone = zoneGO.AddComponent<SurvivalSpawnIntensityZone>();
        ConfigureIntensityZone(intensityZone, zone.type);

        // Add collider for trigger
        SphereCollider col = zoneGO.AddComponent<SphereCollider>();
        col.radius = zone.radius;
        col.isTrigger = true;

        // Spawn props within zone
        ScatterPropsInZone(zoneGO.transform, zone);

        // Visualize zone in gizmo
        DrawZoneGizmo(zoneGO, zone.radius, zone.type);

        Debug.Log($"Created {zone.type} zone at {zone.center}: {zone.description}");
    }

    private static void ConfigureIntensityZone(SurvivalSpawnIntensityZone zone, ZoneType type)
    {
        switch (type)
        {
            case ZoneType.OpenKillField:
                zone.waveDelayMultiplierInZone = 0.85f;
                zone.spawnIntervalMultiplierInZone = 0.8f;
                zone.waveSizeBonusInZone = 5;
                zone.aliveCapBonusInZone = 10;
                break;

            case ZoneType.Industrial:
                zone.waveDelayMultiplierInZone = 0.95f;
                zone.spawnIntervalMultiplierInZone = 0.9f;
                zone.waveSizeBonusInZone = 2;
                zone.aliveCapBonusInZone = 4;
                break;

            case ZoneType.CoverCastle:
                zone.waveDelayMultiplierInZone = 1.2f;
                zone.spawnIntervalMultiplierInZone = 1.0f;
                zone.waveSizeBonusInZone = -2;
                zone.aliveCapBonusInZone = 0;
                break;

            case ZoneType.ChokePoint:
                zone.waveDelayMultiplierInZone = 0.9f;
                zone.spawnIntervalMultiplierInZone = 0.85f;
                zone.waveSizeBonusInZone = 4;
                zone.aliveCapBonusInZone = 7;
                break;

            case ZoneType.MixedTerrain:
                zone.waveDelayMultiplierInZone = 1.0f;
                zone.spawnIntervalMultiplierInZone = 0.95f;
                zone.waveSizeBonusInZone = 1;
                zone.aliveCapBonusInZone = 3;
                break;
        }
    }

    private static void ScatterPropsInZone(Transform zoneRoot, ZoneConfig zone)
    {
        Random.InitState(zone.center.GetHashCode());

        List<GameObject> prefabs = LoadPropPrefabs();
        if (prefabs.Count == 0)
        {
            Debug.LogWarning("No prop prefabs found for zone scatter.");
            return;
        }

        for (int i = 0; i < zone.propCount; i++)
        {
            Vector2 circle = Random.insideUnitCircle * zone.radius;
            Vector3 pos = zone.center + new Vector3(circle.x, 0.5f, circle.y);

            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null)
            {
                continue;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Create zone prop");
            instance.transform.SetParent(zoneRoot);
            instance.transform.position = pos;
            instance.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            float scale = Random.Range(0.85f, 1.3f);
            instance.transform.localScale *= scale;
        }
    }

    private static List<GameObject> LoadPropPrefabs()
    {
        List<GameObject> prefabs = new List<GameObject>();
        string[] paths = new string[]
        {
            "Assets/Prefabs/BarrelEnaNaDrugi.prefab",
            "Assets/Prefabs/Lamp1.prefab"
        };

        foreach (string path in paths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                prefabs.Add(prefab);
            }
        }

        return prefabs;
    }

    private static void DrawZoneGizmo(GameObject zoneGO, float radius, ZoneType type)
    {
        // This is just for organization; actual gizmos drawn in scene view
        // when you select the zone during edit time
    }
}
