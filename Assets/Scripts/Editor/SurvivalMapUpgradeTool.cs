using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SurvivalMapUpgradeTool
{
    private const string RootName = "Map_Upgrade_Auto";
    private const string PropsRootName = "Map_Props_Auto";
    private const string SpawnRootName = "SurvivalSpawnPoints_Auto";

    private static readonly string[] PropPrefabPaths =
    {
        "Assets/Prefabs/BarrelEnaNaDrugi.prefab",
        "Assets/Prefabs/Lamp1.prefab"
    };

    [MenuItem("Tools/Map/Survival/Expand And Decorate")]
    public static void ExpandAndDecorate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("No active scene found.");
            return;
        }

        GameObject root = GetOrCreateRoot(RootName);
        GameObject propsRoot = GetOrCreateChild(root, PropsRootName);
        GameObject spawnRoot = GetOrCreateChild(root, SpawnRootName);

        CreatePlayableGround(root.transform, 280f, 4, 0f);
        CreatePerimeterWalls(root.transform, 280f, 16f, 1.2f);
        CreateCoverClusters(propsRoot.transform, 280f, 140, 24f, 12345);
        CreateSurvivalSpawnPoints(spawnRoot.transform, 280f, 32, 28f, 54321);
        TryAssignSpawnPointsToSpawner(spawnRoot.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("Survival map upgraded: larger play area, props, and spawn points added.");
    }

    [MenuItem("Tools/Map/Survival/Clear Auto Upgrade")]
    public static void ClearAutoUpgrade()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            Debug.Log("No auto-upgrade root found to clear.");
            return;
        }

        Undo.DestroyObjectImmediate(root);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Cleared auto-generated map upgrade objects.");
    }

    private static GameObject GetOrCreateRoot(string name)
    {
        GameObject root = GameObject.Find(name);
        if (root != null)
        {
            return root;
        }

        root = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(root, "Create map upgrade root");
        return root;
    }

    private static GameObject GetOrCreateChild(GameObject parent, string name)
    {
        Transform existing = parent.transform.Find(name);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject child = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(child, "Create map upgrade child");
        child.transform.SetParent(parent.transform);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        return child;
    }

    private static void CreatePlayableGround(Transform parent, float totalSize, int tilesPerSide, float y)
    {
        GameObject groundRoot = GetOrCreateChild(parent.gameObject, "Ground_Auto");
        ClearChildren(groundRoot.transform);

        float tileSize = totalSize / Mathf.Max(1, tilesPerSide);
        float half = totalSize * 0.5f;

        for (int x = 0; x < tilesPerSide; x++)
        {
            for (int z = 0; z < tilesPerSide; z++)
            {
                Vector3 center = new Vector3(
                    -half + tileSize * 0.5f + x * tileSize,
                    y,
                    -half + tileSize * 0.5f + z * tileSize);

                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(tile, "Create ground tile");
                tile.name = $"GroundTile_{x}_{z}";
                tile.transform.SetParent(groundRoot.transform);
                tile.transform.position = center;
                tile.transform.localScale = new Vector3(tileSize, 1f, tileSize);
                tile.layer = 0;
            }
        }
    }

    private static void CreatePerimeterWalls(Transform parent, float totalSize, float wallHeight, float wallThickness)
    {
        GameObject wallRoot = GetOrCreateChild(parent.gameObject, "Walls_Auto");
        ClearChildren(wallRoot.transform);

        float half = totalSize * 0.5f;

        CreateWallPiece(wallRoot.transform, new Vector3(0f, wallHeight * 0.5f, half), new Vector3(totalSize, wallHeight, wallThickness), "Wall_North");
        CreateWallPiece(wallRoot.transform, new Vector3(0f, wallHeight * 0.5f, -half), new Vector3(totalSize, wallHeight, wallThickness), "Wall_South");
        CreateWallPiece(wallRoot.transform, new Vector3(half, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, totalSize), "Wall_East");
        CreateWallPiece(wallRoot.transform, new Vector3(-half, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, totalSize), "Wall_West");
    }

    private static void CreateWallPiece(Transform parent, Vector3 position, Vector3 scale, string name)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(wall, "Create wall piece");
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.localPosition = position;
        wall.transform.localScale = scale;
        wall.layer = 0;
    }

    private static void CreateCoverClusters(Transform propsRoot, float totalSize, int count, float safeRadius, int seed)
    {
        ClearChildren(propsRoot);

        List<GameObject> prefabs = LoadPropPrefabs();
        if (prefabs.Count == 0)
        {
            Debug.LogWarning("No prop prefabs found for auto map decoration. Expected Barrel/Lamp prefabs in Assets/Prefabs.");
            return;
        }

        Random.InitState(seed);
        float half = totalSize * 0.5f - 6f;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-half, half), 0.5f, Random.Range(-half, half));
            if (new Vector2(pos.x, pos.z).magnitude < safeRadius)
            {
                continue;
            }

            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null)
            {
                continue;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Create map prop");
            instance.transform.SetParent(propsRoot);
            instance.transform.localPosition = pos;
            instance.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            float scale = Random.Range(0.9f, 1.2f);
            instance.transform.localScale *= scale;
        }
    }

    private static List<GameObject> LoadPropPrefabs()
    {
        List<GameObject> prefabs = new List<GameObject>();
        for (int i = 0; i < PropPrefabPaths.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PropPrefabPaths[i]);
            if (prefab != null)
            {
                prefabs.Add(prefab);
            }
        }

        return prefabs;
    }

    private static void CreateSurvivalSpawnPoints(Transform spawnRoot, float totalSize, int count, float safeRadius, int seed)
    {
        ClearChildren(spawnRoot);

        Random.InitState(seed);
        float half = totalSize * 0.5f - 8f;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-half, half), 1f, Random.Range(-half, half));
            float dist = new Vector2(pos.x, pos.z).magnitude;
            if (dist < safeRadius)
            {
                pos = pos.normalized * safeRadius;
                pos.y = 1f;
            }

            GameObject point = new GameObject($"SpawnPoint_Auto_{i + 1}");
            Undo.RegisterCreatedObjectUndo(point, "Create spawn point");
            point.transform.SetParent(spawnRoot);
            point.transform.localPosition = pos;
            point.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            SurvivalSpawnPoint setup = point.AddComponent<SurvivalSpawnPoint>();
            setup.spawnWeight = Random.Range(0.8f, 1.3f);
            setup.minWave = Random.Range(1, 6);
            setup.horizontalJitterRadius = Random.Range(1f, 2.2f);
        }
    }

    private static void TryAssignSpawnPointsToSpawner(Transform spawnRoot)
    {
        SurvivalEnemySpawner spawner = Object.FindObjectOfType<SurvivalEnemySpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("No SurvivalEnemySpawner found in scene. Spawn points were created but not auto-assigned.");
            return;
        }

        Transform[] points = new Transform[spawnRoot.childCount];
        for (int i = 0; i < spawnRoot.childCount; i++)
        {
            points[i] = spawnRoot.GetChild(i);
        }

        Undo.RecordObject(spawner, "Assign spawn points");
        spawner.spawnPoints = points;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
        }
    }
}
