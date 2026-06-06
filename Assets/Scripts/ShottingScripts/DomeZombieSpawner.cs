using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DomeZombieSpawner : MonoBehaviour
{
    [Header("Enemy Setup")]
    public GameObject[] enemyPrefabs;
    public Transform player;

    [Header("Spawn Rules")]
    public float spawnRadius = 16f;
    public float spawnHeightOffset = 0f;
    public float spawnInterval = 0.18f;
    public int maxAliveEnemies = 60;
    public int maxSpawnsPerRound = 100;

    [Header("Spawn Grounding")]
    public bool snapSpawnToGround = true;
    public float groundProbeHeight = 20f;
    public float groundProbeDistance = 80f;
    public LayerMask groundMask = ~0;

    private readonly List<GameObject> aliveEnemies = new List<GameObject>();
    private Coroutine roundRoutine;
    private float pendingSpawnDelay;

    public void BeginSpecialRound(float durationSeconds)
    {
        BeginSpecialRound(durationSeconds, 0f);
    }

    public void BeginSpecialRound(float durationSeconds, float spawnDelaySeconds)
    {
        if (roundRoutine != null)
        {
            StopCoroutine(roundRoutine);
        }

        pendingSpawnDelay = Mathf.Max(0f, spawnDelaySeconds);

        roundRoutine = StartCoroutine(SpecialRoundRoutine(durationSeconds));
    }

    public void StopSpecialRound()
    {
        if (roundRoutine != null)
        {
            StopCoroutine(roundRoutine);
            roundRoutine = null;
        }
    }

    private IEnumerator SpecialRoundRoutine(float durationSeconds)
    {
        float endTime = Time.time + Mathf.Max(0.1f, durationSeconds);
        int spawnCount = 0;

        if (pendingSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(pendingSpawnDelay);
        }

        while (Time.time < endTime)
        {
            CleanupDeadEnemies();

            bool underAliveLimit = aliveEnemies.Count < maxAliveEnemies;
            bool underRoundLimit = maxSpawnsPerRound <= 0 || spawnCount < maxSpawnsPerRound;

            if (underAliveLimit && underRoundLimit)
            {
                SpawnEnemy();
                spawnCount++;
            }

            yield return new WaitForSeconds(Mathf.Max(0.01f, spawnInterval));
        }

        roundRoutine = null;
    }

    private void SpawnEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            return;
        }

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        Vector2 circle = Random.insideUnitCircle * Mathf.Max(0.1f, spawnRadius);
        Vector3 spawnCenter = GetSpawnCenter();
        Vector3 spawnPosition = spawnCenter + new Vector3(circle.x, 0f, circle.y);
        spawnPosition = ResolveSpawnPosition(spawnPosition);

        GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);

        if (player != null)
        {
            Vector3 lookTarget = new Vector3(player.position.x, enemy.transform.position.y, player.position.z);
            enemy.transform.LookAt(lookTarget);
        }

        SnapSpawnedEnemyToGround(enemy, spawnPosition.y + spawnHeightOffset);

        // Ensure chase AI is present and tuned for immediate attacks.
        ZombieChaseAI chaseAI = enemy.GetComponent<ZombieChaseAI>();
        if (chaseAI == null)
        {
            chaseAI = enemy.AddComponent<ZombieChaseAI>();
        }
        chaseAI.enabled = true;
        chaseAI.attackRange = Mathf.Max(1.6f, chaseAI.attackRange);
        chaseAI.attackCooldown = Mathf.Max(0.35f, chaseAI.attackCooldown * 0.6f);
        chaseAI.moveSpeed = Mathf.Max(chaseAI.moveSpeed, 3.6f);
        chaseAI.ForceGroundSnap();

        aliveEnemies.Add(enemy);
    }

    private Vector3 ResolveSpawnPosition(Vector3 candidatePosition)
    {
        if (!snapSpawnToGround)
        {
            candidatePosition.y += spawnHeightOffset;
            return candidatePosition;
        }

        // Prefer a high raycast to find visible ground, fall back to NavMesh sampling.
        float resolvedY = candidatePosition.y + spawnHeightOffset;

        // Try an aggressive raycast from high above the candidate position.
        Vector3 rayStart = candidatePosition + Vector3.up * Mathf.Max(20f, groundProbeHeight);
        RaycastHit hit;
        float rayDist = Mathf.Max(groundProbeHeight, groundProbeDistance) + 50f;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, rayDist, groundMask, QueryTriggerInteraction.Ignore))
        {
            resolvedY = hit.point.y + spawnHeightOffset;
        }
        else
        {
            // Try NavMesh sample with larger radius as fallback.
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(candidatePosition, out navHit, Mathf.Max(5f, spawnRadius), NavMesh.AllAreas))
            {
                resolvedY = navHit.position.y + spawnHeightOffset;
            }
            else
            {
                // Final fallback: attempt a very high raycast straight down.
                Vector3 higherStart = candidatePosition + Vector3.up * 100f;
                if (Physics.Raycast(higherStart, Vector3.down, out hit, 200f, groundMask, QueryTriggerInteraction.Ignore))
                {
                    resolvedY = hit.point.y + spawnHeightOffset;
                }
            }
        }

        candidatePosition.y = resolvedY;
        return candidatePosition;
    }

    private Vector3 GetSpawnCenter()
    {
        Transform floor = transform.Find("DomeFloor");
        if (floor != null)
        {
            return floor.position;
        }

        return transform.position;
    }

    private void CleanupDeadEnemies()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (aliveEnemies[i] == null)
            {
                aliveEnemies.RemoveAt(i);
            }
        }
    }

    private void SnapSpawnedEnemyToGround(GameObject enemy, float desiredGroundY)
    {
        if (enemy == null)
        {
            return;
        }

        Bounds? bounds = GetEnemyWorldBounds(enemy);
        if (bounds == null)
        {
            return;
        }

        float currentMinY = bounds.Value.min.y;
        float delta = desiredGroundY - currentMinY;
        if (Mathf.Abs(delta) > 0.001f)
        {
            enemy.transform.position += Vector3.up * delta;
        }
    }

    private Bounds? GetEnemyWorldBounds(GameObject enemy)
    {
        if (enemy == null)
        {
            return null;
        }

        Collider[] colliders = enemy.GetComponentsInChildren<Collider>(true);
        if (colliders != null && colliders.Length > 0)
        {
            bool hasBounds = false;
            Bounds combined = new Bounds();
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider c = colliders[i];
                if (c == null || !c.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combined = c.bounds;
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(c.bounds);
                }
            }

            if (hasBounds)
            {
                return combined;
            }
        }

        Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            bool hasBounds = false;
            Bounds combined = new Bounds();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combined = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(r.bounds);
                }
            }

            if (hasBounds)
            {
                return combined;
            }
        }

        return null;
    }
}
