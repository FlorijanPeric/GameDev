using UnityEditor;
using UnityEngine;

public static class CreateDomePrefabTool
{
    private const string PrefabFolder = "Assets/Prefabs";
    private const string PrefabPath = "Assets/Prefabs/DomeSpecialRound.prefab";
    private const string GlassMaterialPath = "Assets/Prefabs/DomeSpecialRound_Glass.mat";
    private const string FloorMaterialPath = "Assets/Prefabs/DomeSpecialRound_Concrete.mat";

    [MenuItem("Tools/Special Round/Create Dome Prefab")]
    public static void CreateDomePrefab()
    {
        EnsureFolderExists(PrefabFolder);

        GameObject domeRoot = new GameObject("DomeSpecialRound");
        const float domeRadius = 30f;
        const float domeHeight = 20f;

        // Concrete floor.
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        floor.name = "DomeFloor";
        floor.transform.SetParent(domeRoot.transform, false);
        floor.transform.localPosition = new Vector3(0f, -0.2f, 0f);
        floor.transform.localScale = new Vector3(domeRadius, 0.2f, domeRadius);

        MeshRenderer floorRenderer = floor.GetComponent<MeshRenderer>();
        if (floorRenderer != null)
        {
            floorRenderer.sharedMaterial = GetOrCreateConcreteMaterial();
        }

        // Glass side wall.
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wall.name = "DomeGlassWall";
        wall.transform.SetParent(domeRoot.transform, false);
        wall.transform.localPosition = new Vector3(0f, domeHeight * 0.5f, 0f);
        wall.transform.localScale = new Vector3(domeRadius, domeHeight * 0.5f, domeRadius);

        MeshRenderer wallRenderer = wall.GetComponent<MeshRenderer>();
        if (wallRenderer != null)
        {
            wallRenderer.sharedMaterial = GetOrCreateGlassMaterial();
            wallRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            wallRenderer.receiveShadows = false;
        }

        Collider wallCollider = wall.GetComponent<Collider>();
        if (wallCollider != null)
        {
            Object.DestroyImmediate(wallCollider);
        }

        // Entry door object that slides upward to open the arena.
        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = "DomeEntryDoor";
        door.transform.SetParent(domeRoot.transform, false);
        door.transform.localScale = new Vector3(6f, 8f, 0.6f);
        door.transform.localPosition = new Vector3(0f, 4f, domeRadius - 0.3f);

        MeshRenderer doorRenderer = door.GetComponent<MeshRenderer>();
        if (doorRenderer != null)
        {
            doorRenderer.sharedMaterial = GetOrCreateConcreteMaterial();
        }

        DomeEntryDoor entryDoor = door.AddComponent<DomeEntryDoor>();
        entryDoor.openOffset = new Vector3(0f, 9f, 0f);
        entryDoor.holdClosedTime = 0.5f;
        entryDoor.doorOpenTime = 1.1f;

        SphereCollider trigger = domeRoot.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = domeRadius;
        trigger.center = new Vector3(0f, domeHeight * 0.5f, 0f);

        DomeZombieSpawner spawner = domeRoot.AddComponent<DomeZombieSpawner>();
        spawner.spawnRadius = domeRadius - 4f;
        spawner.spawnHeightOffset = 0.2f;
        spawner.spawnInterval = 0.4f;
        spawner.maxAliveEnemies = 40;
        spawner.maxSpawnsPerRound = 0;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(domeRoot, PrefabPath);
        Object.DestroyImmediate(domeRoot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        Debug.Log("Created Dome prefab at " + PrefabPath);
    }

    private static Material GetOrCreateGlassMaterial()
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(GlassMaterialPath);
        if (existing != null)
        {
            return existing;
        }

        Shader shader = FindBestDomeShader();
        Material material = new Material(shader);
        material.name = "DomeSpecialRound_Glass";

        Color tint = new Color(0.15f, 0.85f, 1f, 0.22f);
        material.color = tint;

        // Try to enable transparency for common pipelines.
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", 0f);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", tint);
        }

        material.renderQueue = 3000;

        AssetDatabase.CreateAsset(material, GlassMaterialPath);
        return material;
    }

    private static Material GetOrCreateConcreteMaterial()
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(FloorMaterialPath);
        if (existing != null)
        {
            return existing;
        }

        Shader shader = FindBestDomeShader();
        Material material = new Material(shader);
        material.name = "DomeSpecialRound_Concrete";

        Color baseColor = new Color(0.42f, 0.42f, 0.44f, 1f);
        material.color = baseColor;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", baseColor);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.15f);
        }

        AssetDatabase.CreateAsset(material, FloorMaterialPath);
        return material;
    }

    private static Shader FindBestDomeShader()
    {
        string[] shaderNames =
        {
            "HDRP/Unlit",
            "Universal Render Pipeline/Unlit",
            "Standard"
        };

        for (int i = 0; i < shaderNames.Length; i++)
        {
            Shader shader = Shader.Find(shaderNames[i]);
            if (shader != null)
            {
                return shader;
            }
        }

        return Shader.Find("Standard");
    }

    private static void EnsureFolderExists(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string[] parts = path.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
