using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enlarges the playable map by scaling colliders and expanding spawn zones.
/// Extends walls outward and increases playable area for round 2+
/// </summary>
public class MapEnlargementSystem : MonoBehaviour
{
    [Header("Map Expansion Settings")]
    public float expansionScale = 1.5f; // 1.5x = 50% larger
    public float wallHeightMultiplier = 1.2f; // Make walls taller too
    public bool expandNavMesh = true;

    [Header("Colliders to Expand")]
    public BoxCollider[] wallColliders;
    public BoxCollider[] terrainColliders;

    [Header("Spawn Point Expansion")]
    public SurvivalSpawnPoint[] spawnPoints;
    public float spawnRadiusMultiplier = 1.3f;

    private Vector3 mapCenterPoint;
    private bool hasExpanded = false;

    public void ExpandMap()
    {
        if (hasExpanded)
        {
            Debug.LogWarning("Map has already been expanded!");
            return;
        }

        Debug.Log("MapEnlargementSystem: Expanding map...");

        // Find map center if not already set
        if (mapCenterPoint == Vector3.zero)
        {
            FindMapCenter();
        }

        // Expand wall colliders
        if (wallColliders != null)
        {
            for (int i = 0; i < wallColliders.Length; i++)
            {
                if (wallColliders[i] != null)
                {
                    ExpandCollider(wallColliders[i], expansionScale, wallHeightMultiplier);
                }
            }
        }

        // Expand terrain/floor colliders
        if (terrainColliders != null)
        {
            for (int i = 0; i < terrainColliders.Length; i++)
            {
                if (terrainColliders[i] != null)
                {
                    ExpandCollider(terrainColliders[i], expansionScale, 1f);
                }
            }
        }

        // Expand spawn points
        if (spawnPoints != null)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    ExpandSpawnPoint(spawnPoints[i]);
                }
            }
        }

        // Rebuild NavMesh if needed.
        // Note: runtime NavMesh baking APIs are limited; use editor-only rebake when running in the Editor.
        if (expandNavMesh)
        {
#if UNITY_EDITOR
            try
            {
                UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"MapEnlargementSystem: Editor NavMesh rebuild failed: {ex.Message}");
            }
#else
            Debug.LogWarning("MapEnlargementSystem: Runtime NavMesh rebake is not supported. Please rebake the NavMesh in the Editor.");
#endif
        }

        hasExpanded = true;
        Debug.Log($"MapEnlargementSystem: Map expanded by {expansionScale}x");
    }

    private void ExpandCollider(BoxCollider collider, float scaleMultiplier, float heightMult)
    {
        if (collider == null) return;

        // Move collider away from center to expand outward
        Vector3 centerToDist = collider.transform.position - mapCenterPoint;
        Vector3 newPos = mapCenterPoint + centerToDist * scaleMultiplier;

        collider.transform.position = newPos;
        collider.size = new Vector3(
            collider.size.x * scaleMultiplier,
            collider.size.y * heightMult,
            collider.size.z * scaleMultiplier
        );

        collider.center = new Vector3(
            collider.center.x * scaleMultiplier,
            collider.center.y * heightMult,
            collider.center.z * scaleMultiplier
        );
    }

    private void ExpandSpawnPoint(SurvivalSpawnPoint spawnPoint)
    {
        if (spawnPoint == null) return;

        // Move spawn point away from center
        Vector3 centerToDist = spawnPoint.transform.position - mapCenterPoint;
        spawnPoint.transform.position = mapCenterPoint + centerToDist * spawnRadiusMultiplier;

        // Increase jitter radius
        spawnPoint.horizontalJitterRadius *= spawnRadiusMultiplier;
    }

    private void FindMapCenter()
    {
        // Find center of all walls/terrain
        Vector3 sum = Vector3.zero;
        int count = 0;

        if (wallColliders != null)
        {
            for (int i = 0; i < wallColliders.Length; i++)
            {
                if (wallColliders[i] != null)
                {
                    sum += wallColliders[i].transform.position;
                    count++;
                }
            }
        }

        if (terrainColliders != null)
        {
            for (int i = 0; i < terrainColliders.Length; i++)
            {
                if (terrainColliders[i] != null)
                {
                    sum += terrainColliders[i].transform.position;
                    count++;
                }
            }
        }

        if (count > 0)
        {
            mapCenterPoint = sum / count;
        }
        else
        {
            // Fallback to scene center
            mapCenterPoint = Vector3.zero;
        }

        Debug.Log($"MapEnlargementSystem: Map center found at {mapCenterPoint}");
    }

    public bool HasExpanded => hasExpanded;
}
