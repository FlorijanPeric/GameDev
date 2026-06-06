using UnityEditor;
using UnityEngine;

public static class EnsureCollidersForStaticObjects
{
    [MenuItem("Survival/Ensure Colliders For Static Scene Objects")]
    public static void AddCollidersToStaticObjects()
    {
        int added = 0;
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject go = allObjects[i];
            if (go == null) continue;
            if (!go.isStatic) continue;

            // Skip terrains and UI/particles
            if (go.GetComponent<Terrain>() != null) continue;
            if (go.GetComponent<ParticleSystem>() != null) continue;

            Collider existing = go.GetComponent<Collider>();
            if (existing != null) continue;

            // Look for renderer in this object or children
            Renderer r = go.GetComponentInChildren<Renderer>();
            if (r == null) continue;

            // Add BoxCollider sized to renderer bounds
            Undo.AddComponent<BoxCollider>(go);
            BoxCollider bc = go.GetComponent<BoxCollider>();
            if (bc != null)
            {
                Bounds b = r.bounds;
                bc.center = go.transform.InverseTransformPoint(b.center);
                Vector3 size = b.size;
                // Convert world size to local by removing scale
                Vector3 localSize = new Vector3(
                    Mathf.Abs(size.x / Mathf.Max(0.0001f, go.transform.lossyScale.x)),
                    Mathf.Abs(size.y / Mathf.Max(0.0001f, go.transform.lossyScale.y)),
                    Mathf.Abs(size.z / Mathf.Max(0.0001f, go.transform.lossyScale.z))
                );
                bc.size = localSize;
                added++;
            }
        }

        Debug.Log($"EnsureCollidersForStaticObjects: Added {added} BoxCollider(s) to static objects in the scene.");
    }

    [MenuItem("Survival/Ensure Colliders For Selected Static Objects")]
    public static void AddCollidersToSelected()
    {
        int added = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            if (go == null) continue;
            if (!go.isStatic) continue;
            if (go.GetComponent<Collider>() != null) continue;

            Renderer r = go.GetComponentInChildren<Renderer>();
            if (r == null) continue;

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
                added++;
            }
        }

        Debug.Log($"EnsureCollidersForStaticObjects: Added {added} BoxCollider(s) to selected static objects.");
    }
}
