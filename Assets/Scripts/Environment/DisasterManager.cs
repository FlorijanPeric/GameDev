using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DisasterManager : MonoBehaviour
{
    [Header("Player")]
    public Transform player;

    [Header("Lightning")]
    public GameObject lightningPrefab;
    public AudioClip lightningClip;
    public Vector2 lightningIntervalRange = new Vector2(8f, 24f);
    public float lightningRangeMin = 12f;
    public float lightningRangeMax = 60f;
    public float lightningDamage = 60f;
    public float lightningRadius = 4f;
    public LayerMask groundMask = ~0;

    [Header("Earthquake")]
    public AudioClip earthquakeClip;
    public Vector2 earthquakeIntervalRange = new Vector2(30f, 90f);
    public float earthquakeRadius = 28f;
    public float earthquakeForce = 700f;
    public float earthquakeDuration = 3f;
    public float earthquakeShakeMagnitude = 0.4f;
    public int earthquakeCrackCount = 8;
    public float earthquakeCrackRadius = 7f;
    public float earthquakeCrackLifetime = 10f;

    [Header("Debug")]
    public bool autoStart = true;

    private Coroutine lightningRoutine;
    private Coroutine earthquakeRoutine;

    private void OnValidate()
    {
        AutoAssignPlayerIfMissing();
        AutoAssignAssetsIfMissing();
    }

    private void Start()
    {
        AutoAssignPlayerIfMissing();
        AutoAssignAssetsIfMissing();
        if (autoStart)
        {
            StartRoutines();
        }
    }

    public void AutoAssignPlayerIfMissing()
    {
        if (player != null) return;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        else if (Camera.main != null) player = Camera.main.transform;
    }

    public void AutoAssignAssetsIfMissing()
    {
#if UNITY_EDITOR
        if (lightningPrefab == null)
        {
            lightningPrefab = FindPrefabByKeywords(new[] { "Bullet" });
        }

        if (lightningClip == null)
        {
            lightningClip = FindClipByKeywords(new[] { "lightning", "thunder", "strike" });
        }

        if (earthquakeClip == null)
        {
            earthquakeClip = FindClipByKeywords(new[] { "earthquake", "quake", "rumble", "rock", "ground" });
        }
#endif
    }

    public void StartRoutines()
    {
        StopRoutines();
        lightningRoutine = StartCoroutine(LightningLoop());
        earthquakeRoutine = StartCoroutine(EarthquakeLoop());
    }

    public void StopRoutines()
    {
        if (lightningRoutine != null) { StopCoroutine(lightningRoutine); lightningRoutine = null; }
        if (earthquakeRoutine != null) { StopCoroutine(earthquakeRoutine); earthquakeRoutine = null; }
    }

    private IEnumerator LightningLoop()
    {
        while (true)
        {
            float wait = Random.Range(lightningIntervalRange.x, lightningIntervalRange.y);
            yield return new WaitForSeconds(wait);
            TriggerLightning();
        }
    }

    private IEnumerator EarthquakeLoop()
    {
        while (true)
        {
            float wait = Random.Range(earthquakeIntervalRange.x, earthquakeIntervalRange.y);
            yield return new WaitForSeconds(wait);
            StartCoroutine(TriggerEarthquake());
        }
    }

    public void TriggerLightning()
    {
        if (player == null) AutoAssignPlayerIfMissing();
        if (player == null) return;

        // pick random position in ring around player
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist = Random.Range(lightningRangeMin, lightningRangeMax);
        Vector3 candidate = player.position + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

        // probe to ground
        Vector3 rayStart = candidate + Vector3.up * 100f;
        RaycastHit hit;
        if (!Physics.Raycast(rayStart, Vector3.down, out hit, 200f, groundMask, QueryTriggerInteraction.Ignore))
        {
            return; // no ground found
        }

        Vector3 strikePos = hit.point;

        if (lightningPrefab != null)
        {
            Instantiate(lightningPrefab, strikePos, Quaternion.identity);
        }

        if (lightningClip != null)
        {
            AudioSource.PlayClipAtPoint(lightningClip, strikePos);
        }

        // Damage nearby enemies and apply force to rigidbodies
        Collider[] cols = Physics.OverlapSphere(strikePos, lightningRadius, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < cols.Length; i++)
        {
            EnemyHealth eh = cols[i].GetComponentInParent<EnemyHealth>();
            if (eh != null)
            {
                eh.TakeDamage(lightningDamage);
            }

            Rigidbody rb = cols[i].attachedRigidbody;
            if (rb != null)
            {
                rb.AddExplosionForce(800f, strikePos, lightningRadius, 0.5f, ForceMode.Impulse);
            }
        }
    }

    private IEnumerator TriggerEarthquake()
    {
        if (player == null) AutoAssignPlayerIfMissing();
        if (player == null)
            yield break;

        Vector3 center = player.position;

        SpawnEarthquakeCracks(center);

        if (earthquakeClip != null && Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(earthquakeClip, Camera.main.transform.position);
        }

        float endTime = Time.time + earthquakeDuration;

        // find nearby agents to stop
        List<NavMeshAgent> stoppedAgents = new List<NavMeshAgent>();
        Collider[] cols = Physics.OverlapSphere(center, earthquakeRadius, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < cols.Length; i++)
        {
            Rigidbody rb = cols[i].attachedRigidbody;
            if (rb != null && !rb.CompareTag("Player"))
            {
                rb.AddExplosionForce(earthquakeForce, center, earthquakeRadius, 1f, ForceMode.Impulse);
            }

            NavMeshAgent nav = cols[i].GetComponentInParent<NavMeshAgent>();
            if (nav != null && !stoppedAgents.Contains(nav))
            {
                if (nav.enabled && nav.isOnNavMesh)
                {
                    nav.isStopped = true;
                    stoppedAgents.Add(nav);
                }
            }
        }

        // camera shake
        Transform cam = Camera.main != null ? Camera.main.transform : null;
        Vector3 originalLocal = Vector3.zero;
        if (cam != null) originalLocal = cam.localPosition;

        while (Time.time < endTime)
        {
            if (cam != null)
            {
                cam.localPosition = originalLocal + Random.insideUnitSphere * earthquakeShakeMagnitude;
            }
            yield return null;
        }

        if (cam != null) cam.localPosition = originalLocal;

        // restore agents
        for (int i = 0; i < stoppedAgents.Count; i++)
        {
            if (stoppedAgents[i] != null)
            {
                stoppedAgents[i].isStopped = false;
            }
        }
    }

    private void SpawnEarthquakeCracks(Vector3 center)
    {
        if (earthquakeCrackCount <= 0 || earthquakeCrackLifetime <= 0f)
        {
            return;
        }

        for (int i = 0; i < earthquakeCrackCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(0.5f, earthquakeCrackRadius);
            Vector3 origin = center + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);

            Vector3 rayStart = origin + Vector3.up * 40f;
            RaycastHit hit;
            if (!Physics.Raycast(rayStart, Vector3.down, out hit, 80f, groundMask, QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            GameObject crack = new GameObject("EarthquakeCrack");
            crack.transform.position = hit.point + hit.normal * 0.02f;
            crack.transform.rotation = Quaternion.LookRotation(Vector3.Cross(hit.normal, Vector3.right), hit.normal);

            int segments = Random.Range(4, 7);
            for (int s = 0; s < segments; s++)
            {
                GameObject seg = new GameObject($"CrackSeg_{s}");
                seg.transform.SetParent(crack.transform, false);

                LineRenderer line = seg.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.useWorldSpace = false;
                line.startWidth = 0.06f;
                line.endWidth = 0.01f;
                line.numCapVertices = 0;
                line.numCornerVertices = 0;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = new Color(0.08f, 0.08f, 0.08f, 0.95f);
                line.endColor = new Color(0.08f, 0.08f, 0.08f, 0.95f);

                Vector3 a = new Vector3(0f, 0.01f, 0f);
                Vector3 b = new Vector3(Random.Range(-earthquakeCrackRadius * 0.4f, earthquakeCrackRadius * 0.4f), 0.01f, Random.Range(earthquakeCrackRadius * 0.12f, earthquakeCrackRadius * 0.45f));
                line.SetPosition(0, a);
                line.SetPosition(1, b);
            }

            Destroy(crack, earthquakeCrackLifetime);
        }
    }

#if UNITY_EDITOR
    private GameObject FindPrefabByKeywords(string[] keywords)
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            string lower = path.ToLowerInvariant();
            for (int k = 0; k < keywords.Length; k++)
            {
                if (lower.Contains(keywords[k].ToLowerInvariant()))
                {
                    GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        return prefab;
                    }
                }
            }
        }

        return null;
    }

    private AudioClip FindClipByKeywords(string[] keywords)
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            string lower = path.ToLowerInvariant();
            for (int k = 0; k < keywords.Length; k++)
            {
                if (lower.Contains(keywords[k].ToLowerInvariant()))
                {
                    AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip != null)
                    {
                        return clip;
                    }
                }
            }
        }

        return null;
    }
#endif
}
