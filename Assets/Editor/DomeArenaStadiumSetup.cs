using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DomeArenaStadiumSetup : MonoBehaviour
{
    [MenuItem("Survival/Setup Stadium As Dome Arena")]
    public static void SetupStadiumDome()
    {
        // Find or load SM_Stadium prefab
        string stadiumPath = "Assets/Hayq Art/GrantStadium/Prefabs/Buildings/SM_Stadium.prefab";
        GameObject stadiumPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(stadiumPath);
        
        if (stadiumPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not load SM_Stadium.prefab at " + stadiumPath, "OK");
            return;
        }
        
        // Check if stadium already exists in scene
        Transform existingStadium = FindObjectOfType<Transform>();
        GameObject stadiumInstance = null;
        
        // Try to find existing stadium in scene
        foreach (GameObject go in FindObjectsOfType<GameObject>())
        {
            if (go.name.Contains("SM_Stadium") || go.name.Contains("Stadium"))
            {
                stadiumInstance = go;
                break;
            }
        }
        
        // If not in scene, instantiate it
        if (stadiumInstance == null)
        {
            stadiumInstance = PrefabUtility.InstantiatePrefab(stadiumPrefab) as GameObject;
            stadiumInstance.name = "SM_Stadium_Dome";
            stadiumInstance.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(stadiumInstance, "Create Stadium Dome");
        }
        
        // Add colliders to stadium if missing
        AddCollidersToStaticGeometry(stadiumInstance);
        
        // Create floor platform inside stadium
        CreateDomeFloor(stadiumInstance);
        
        // Set up DomeArenaManager
        DomeArenaManager domeManager = FindObjectOfType<DomeArenaManager>();
        if (domeManager == null)
        {
            GameObject domeManagerGO = new GameObject("DomeArenaManager");
            domeManager = domeManagerGO.AddComponent<DomeArenaManager>();
        }

        // Create or reuse a separate dome spawner so the normal survival spawner stays untouched.
        DomeZombieSpawner domeSpawner = FindObjectOfType<DomeZombieSpawner>();
        if (domeSpawner == null)
        {
            GameObject domeSpawnerGO = new GameObject("DomeZombieSpawner");
            domeSpawnerGO.transform.position = stadiumInstance.transform.position;
            domeSpawnerGO.transform.SetParent(stadiumInstance.transform, true);
            domeSpawner = domeSpawnerGO.AddComponent<DomeZombieSpawner>();
        }

        SurvivalEnemySpawner mainSpawner = FindObjectOfType<SurvivalEnemySpawner>();
        if (mainSpawner != null && mainSpawner.enemyPrefabs != null && mainSpawner.enemyPrefabs.Length > 0)
        {
            domeSpawner.enemyPrefabs = mainSpawner.enemyPrefabs;
        }

        domeSpawner.spawnHeightOffset = 0f;
        domeSpawner.spawnRadius = 18f;
        
        // Set dome center to stadium center
        GameObject domeCenterGO = new GameObject("DomeCenter");
        Transform domeFloor = stadiumInstance.transform.Find("DomeFloor");
        Vector3 domeCenterPosition = stadiumInstance.transform.position;
        if (domeFloor != null)
        {
            domeCenterPosition = new Vector3(stadiumInstance.transform.position.x, domeFloor.position.y, stadiumInstance.transform.position.z);
        }

        domeCenterGO.transform.position = domeCenterPosition;
        if (domeManager != null)
        {
            domeManager.transform.position = domeCenterGO.transform.position;
            SerializedObject serializedManager = new SerializedObject(domeManager);
            serializedManager.FindProperty("domeSpawner").objectReferenceValue = domeSpawner;
            serializedManager.FindProperty("stadiumGameObject").objectReferenceValue = stadiumInstance;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
        }
        
        Undo.RegisterCreatedObjectUndo(domeCenterGO, "Create Dome Center");
        
        EditorUtility.DisplayDialog("Success", "Stadium dome setup complete!\n\n" +
            "- Stadium geometry positioned\n" +
            "- Colliders added to stadium\n" +
            "- Floor platform created inside\n" +
            "- DomeArenaManager configured\n\n" +
            "Next: Assign a special rifle prefab in DomeArenaManager inspector.", "OK");
    }
    
    private static void AddCollidersToStaticGeometry(GameObject stadium)
    {
        if (stadium == null) return;
        // Make stadium static
        stadium.isStatic = true;
        
        // Add box colliders to stadium geometry
        Renderer[] renderers = stadium.GetComponentsInChildren<Renderer>();
        int collidersAdded = 0;
        
        foreach (Renderer r in renderers)
        {
            GameObject go = r.gameObject;
            if (go.GetComponent<Collider>() == null)
            {
                Undo.AddComponent<BoxCollider>(go);
                BoxCollider bc = go.GetComponent<BoxCollider>();
                if (bc != null)
                {
                    Bounds b = r.bounds;
                    bc.center = go.transform.InverseTransformPoint(b.center);
                    Vector3 size = b.size;
                    Vector3 localSize = new Vector3(
                        Mathf.Abs(size.x / Mathf.Max(0.0001f, go.transform.lossyScale.x)),
                        Mathf.Abs(size.y / Mathf.Max(0.0001f, go.transform.lossyScale.y)),
                        Mathf.Abs(size.z / Mathf.Max(0.0001f, go.transform.lossyScale.z))
                    );
                    bc.size = localSize;
                    collidersAdded++;
                }
            }
        }
        
        Debug.Log($"Added {collidersAdded} colliders to stadium geometry.");
    }
    
    private static void CreateDomeFloor(GameObject stadium)
    {
        if (stadium == null) return;
        // Get stadium bounds
        Renderer[] renderers = stadium.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("No renderers found in stadium!");
            return;
        }
        
        // Calculate bounds
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }
        
        // Create floor platform
        GameObject floor = new GameObject("DomeFloor");
        floor.transform.parent = stadium.transform;
        floor.transform.position = new Vector3(bounds.center.x, bounds.min.y - 0.1f, bounds.center.z);
        
        // Add mesh collider for the floor
        BoxCollider floorCollider = floor.AddComponent<BoxCollider>();
        
        // Size the floor to match stadium footprint with some padding
        float floorWidth = bounds.size.x * 1.1f;
        float floorDepth = bounds.size.z * 1.1f;
        float floorHeight = 0.2f; // Thin platform
        
        floorCollider.size = new Vector3(floorWidth, floorHeight, floorDepth);
        floorCollider.center = Vector3.up * (floorHeight / 2f);
        
        // Add material to make it visible
        MeshFilter floorMesh = floor.AddComponent<MeshFilter>();
        MeshRenderer floorRenderer = floor.AddComponent<MeshRenderer>();
        
        // Create a simple cube mesh and scale it
        floorMesh.mesh = CreatePlatformMesh(floorWidth, floorHeight, floorDepth);
        
        // Apply material
        Material floorMat = new Material(Shader.Find("Standard"));
        floorMat.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        floorRenderer.material = floorMat;
        
        Undo.RegisterCreatedObjectUndo(floor, "Create Dome Floor");
        
        Debug.Log($"Created dome floor: {floorWidth}x{floorDepth} at Y={floor.transform.position.y}");
    }
    
    private static Mesh CreatePlatformMesh(float width, float height, float depth)
    {
        Mesh mesh = new Mesh();
        mesh.name = "DomeFloorMesh";
        
        float w = width / 2f;
        float h = height / 2f;
        float d = depth / 2f;
        
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-w, -h, -d),
            new Vector3(w, -h, -d),
            new Vector3(w, -h, d),
            new Vector3(-w, -h, d),
            new Vector3(-w, h, -d),
            new Vector3(w, h, -d),
            new Vector3(w, h, d),
            new Vector3(-w, h, d),
        };
        
        int[] triangles = new int[]
        {
            0, 2, 1, 0, 3, 2,
            4, 5, 6, 4, 6, 7,
            0, 1, 5, 0, 5, 4,
            1, 2, 6, 1, 6, 5,
            2, 3, 7, 2, 7, 6,
            3, 0, 4, 3, 4, 7,
        };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
}
