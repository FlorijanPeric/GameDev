using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class SurvivalEnemySpawner : MonoBehaviour
{
    [Header("Spawn Setup")]
    public GameObject[] enemyPrefabs;
    public Transform[] spawnPoints;
    public Transform player;
    public LayerMask visibilityBlockers = ~0;
    public bool autoStartSpawnLoop = true;

    [Header("Wave Timing")]
    public float initialDelay = 2f;
    public float timeBetweenWaves = 4f;
    public float timeBetweenSpawns = 0.18f;

    [Header("Wave Size")]
    public int startingEnemies = 24;
    public int additionalEnemiesPerWave = 8;
    public int maxAliveEnemies = 120;
    public int additionalAliveCapPerWave = 2;
    public int hardAliveCap = 220;

    [Header("Map Playability")]
    public float minSpawnDistanceFromPlayer = 12f;
    public float maxSpawnDistanceFromPlayer = 55f;
    public bool requireSpawnOutOfSight = true;
    public int spawnPointCooldownCount = 3;
    public int maxSpawnPointPickAttempts = 12;
    public bool enableRandomMapSpawning = true;
    public float randomSpawnChance = 0.9f;

    [Header("Generated Spawn Points")]
    public bool autoGenerateSpawnPoints = true;
    public int generatedSpawnPointCount = 12;
    public float generatedSpawnRadiusMin = 18f;
    public float generatedSpawnRadiusMax = 55f;
    public float generatedSpawnPointGroundOffset = 0.05f;

    [Header("Spawn Separation")]
    public float spawnSeparationDistance = 0.8f; // minimum spacing between spawned enemies
    public float spawnSeparationImpulse = 2.25f;


    [Header("Spawn Grounding")]
    public bool snapSpawnToGround = true;
    public bool forceSpawnAtPlayerHeight = false;
    public float groundProbeHeight = 20f;
    public float groundProbeDistance = 80f;
    public float spawnGroundOffset = 0.02f;

    [Header("Enemy Animation")]
    public RuntimeAnimatorController defaultZombieAnimatorController;

    [Header("Runtime Tuning")]
    public float waveDelayMultiplier = 1f;
    public float spawnIntervalMultiplier = 1f;
    public int waveSizeBonus = 0;
    public int aliveCapBonus = 0;
    [Header("Spawn Smoothing")]
    public float maxSpawnsPerSecond = 8f;

    [Header("Continuous Reinforcement")]
    public bool replaceDeadEnemiesContinuously = true;
    public int minimumAliveEnemyCount = 14;
    public float replacementSpawnDelay = 0.15f;

    [Header("Enemy Audio (assigned to spawned enemies)")]
    public AudioClip enemyFootstepClip;
    public AudioClip enemyAttackClip;
    public AudioClip enemyHurtClip;
    public AudioClip enemyDeathClip;

    [Header("Debug")]
    public bool debugTrackOneSpawnedEnemy = true;
    public int debugTrackEnemyFrames = 60;

    private int waveIndex;
    private readonly List<GameObject> aliveEnemies = new List<GameObject>();
    private readonly List<int> recentSpawnPointIndices = new List<int>();
    private Coroutine activeRoutine;
    private float spawnTokenBucket = 0f;
    private bool directedWaveActive;
    private float directedHealthScale = 1f;
    private float directedSpeedScale = 1f;
    private float directedDamageScale = 1f;
    private readonly List<GameObject> generatedSpawnPointObjects = new List<GameObject>();
    private bool debugEnemyTracked;
    private bool replacementSpawnQueued;

    public event System.Action<int, int> WaveStarted;
    public event System.Action<int> WaveCompleted;
    public event System.Action OnEnemyKilled;
    public event System.Action<GameObject> EnemySpawned;

    public int CurrentWaveIndex => waveIndex;
    public int AliveEnemyCount
    {
        get
        {
            CleanupDeadEnemies();
            return aliveEnemies.Count;
        }
    }

    public bool IsDirectedWaveActive => directedWaveActive;

    private void Awake()
    {
        AutoAssignPlayerIfMissing();
        AutoAssignSpawnPointsIfMissing();
        RebuildGeneratedSpawnPoints();
        AutoAssignEnemyAudioIfMissing();
        AutoAssignDefaultAnimatorControllerIfMissing();
        AutoAssignEnemyPrefabsIfMissing();
    }

    private void OnValidate()
    {
        RebuildGeneratedSpawnPoints();
        AutoAssignEnemyAudioIfMissing();
        AutoAssignDefaultAnimatorControllerIfMissing();
        AutoAssignEnemyPrefabsIfMissing();
    }

    private void OnEnable()
    {
        if (autoStartSpawnLoop)
        {
            StartEndlessLoop();
        }
    }

    private void OnDisable()
    {
        StopSpawning();
    }

    public void StartEndlessLoop()
    {
        StopSpawning();
        activeRoutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        directedWaveActive = false;
        directedHealthScale = 1f;
        directedSpeedScale = 1f;
        directedDamageScale = 1f;
        replacementSpawnQueued = false;
    }

    public void BeginDirectedWave(int waveNumber, int enemiesThisWave, int aliveCapForWave, float spawnIntervalScale)
    {
        BeginDirectedWave(waveNumber, enemiesThisWave, aliveCapForWave, spawnIntervalScale, 1f, 1f, 1f);
    }

    public void BeginDirectedWave(int waveNumber, int enemiesThisWave, int aliveCapForWave, float spawnIntervalScale, float healthScale, float speedScale, float damageScale)
    {
        StopSpawning();
        waveIndex = Mathf.Max(1, waveNumber);
        directedHealthScale = Mathf.Max(0.25f, healthScale);
        directedSpeedScale = Mathf.Max(0.25f, speedScale);
        directedDamageScale = Mathf.Max(0.25f, damageScale);
        activeRoutine = StartCoroutine(DirectedWaveRoutine(
            Mathf.Max(1, enemiesThisWave),
            Mathf.Max(1, aliveCapForWave),
            Mathf.Max(0.1f, spawnIntervalScale)));
    }

    public void SetSpawnRateMultiplier(float multiplier)
    {
        spawnIntervalMultiplier = Mathf.Max(0.1f, multiplier);
    }

    // Public wrapper to allow regenerating generated spawn points from external systems (editor/runtime)
    public void RegenerateSpawnPoints()
    {
        RebuildGeneratedSpawnPoints();
    }

    private IEnumerator SpawnLoop()
    {
        Debug.Log("SurvivalEnemySpawner: SpawnLoop starting in " + initialDelay + "s", this);
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            waveIndex++;
            int baseWaveCount = startingEnemies + (waveIndex - 1) * additionalEnemiesPerWave;
            int enemiesThisWave = Mathf.Max(1, baseWaveCount + waveSizeBonus);

            int baseAliveCap = maxAliveEnemies + (waveIndex - 1) * additionalAliveCapPerWave;
            int aliveCapThisWave = Mathf.Max(1, Mathf.Min(hardAliveCap, baseAliveCap + aliveCapBonus));

            Debug.Log($"SurvivalEnemySpawner: Wave {waveIndex} starting. Enemies this wave: {enemiesThisWave}, AliveCap: {aliveCapThisWave}", this);
            for (int i = 0; i < enemiesThisWave; i++)
            {
                CleanupDeadEnemies();

                while (aliveEnemies.Count >= aliveCapThisWave)
                {
                    CleanupDeadEnemies();
                    yield return null;
                }

                // Refill spawn tokens and enforce max spawns per second
                spawnTokenBucket += Time.deltaTime * Mathf.Max(0.01f, maxSpawnsPerSecond);
                if (spawnTokenBucket < 1f)
                {
                    // Wait until a token is available
                    yield return null;
                    i--; // retry this spawn iteration
                    continue;
                }

                spawnTokenBucket = Mathf.Max(0f, spawnTokenBucket - 1f);

                Debug.Log($"SurvivalEnemySpawner: Attempting spawn #{i+1} of {enemiesThisWave}", this);
                SpawnEnemy();
                float nextSpawnDelay = Mathf.Max(0.01f, timeBetweenSpawns * Mathf.Max(0.1f, spawnIntervalMultiplier));
                yield return new WaitForSeconds(nextSpawnDelay);
            }

            float nextWaveDelay = Mathf.Max(0.2f, timeBetweenWaves * Mathf.Max(0.1f, waveDelayMultiplier));
            yield return new WaitForSeconds(nextWaveDelay);
        }
    }

    private IEnumerator DirectedWaveRoutine(int enemiesThisWave, int aliveCapForWave, float spawnIntervalScale)
    {
        directedWaveActive = true;
        WaveStarted?.Invoke(waveIndex, enemiesThisWave);

        int spawned = 0;
        while (spawned < enemiesThisWave)
        {
            CleanupDeadEnemies();

            while (aliveEnemies.Count >= aliveCapForWave)
            {
                CleanupDeadEnemies();
                yield return null;
            }
            // Token bucket for directed waves as well
            spawnTokenBucket += Time.deltaTime * Mathf.Max(0.01f, maxSpawnsPerSecond);
            if (spawnTokenBucket < 1f)
            {
                yield return null;
                continue;
            }
            spawnTokenBucket = Mathf.Max(0f, spawnTokenBucket - 1f);

            Debug.Log($"SurvivalEnemySpawner: DirectedWave spawning {spawned+1}/{enemiesThisWave}", this);
            SpawnEnemy();
            spawned++;

            float nextSpawnDelay = Mathf.Max(0.01f, timeBetweenSpawns * spawnIntervalScale * Mathf.Max(0.1f, spawnIntervalMultiplier));
            yield return new WaitForSeconds(nextSpawnDelay);
        }

        while (true)
        {
            CleanupDeadEnemies();
            if (aliveEnemies.Count == 0)
            {
                break;
            }

            yield return null;
        }

        directedWaveActive = false;
        activeRoutine = null;
        WaveCompleted?.Invoke(waveIndex);
    }

    public void SetRuntimeTuning(float newWaveDelayMultiplier, float newSpawnIntervalMultiplier, int newWaveSizeBonus, int newAliveCapBonus)
    {
        waveDelayMultiplier = Mathf.Max(0.1f, newWaveDelayMultiplier);
        spawnIntervalMultiplier = Mathf.Max(0.1f, newSpawnIntervalMultiplier);
        waveSizeBonus = newWaveSizeBonus;
        aliveCapBonus = newAliveCapBonus;
    }

    public void ResetRuntimeTuning()
    {
        waveDelayMultiplier = 1f;
        spawnIntervalMultiplier = 1f;
        waveSizeBonus = 0;
        aliveCapBonus = 0;
    }

    public void ForceSpawnEnemy(int prefabIndex = -1)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("SurvivalEnemySpawner: No enemy prefabs assigned.", this);
            return;
        }

        if (prefabIndex < 0 || prefabIndex >= enemyPrefabs.Length)
        {
            prefabIndex = Random.Range(0, enemyPrefabs.Length);
        }

        SpawnEnemy(enemyPrefabs[prefabIndex]);
    }

    private void SpawnEnemy(GameObject forcedPrefab = null)
    {
        AutoAssignPlayerIfMissing();

        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("SurvivalEnemySpawner: No enemyPrefabs assigned - cannot spawn.", this);
            return;
        }

        GameObject prefab = forcedPrefab != null ? forcedPrefab : PickEnemyPrefab();
        if (prefab == null)
        {
            return;
        }
        Vector3 spawnPosition;
        Quaternion spawnRotation = Quaternion.identity;

        // Decide between random map spawn and spawn point
        if (enableRandomMapSpawning && Random.value < randomSpawnChance && player != null)
        {
            // Try random map spawning
            if (TryGetRandomMapSpawnPosition(out spawnPosition, out spawnRotation))
            {
                // Random spawn succeeded
            }
            else if (spawnPoints != null && spawnPoints.Length > 0)
            {
                // Fallback to spawn point
                int spawnIndex = FindBestSpawnPointIndex();
                if (spawnIndex >= 0)
                {
                    Transform spawnPoint = spawnPoints[spawnIndex];
                    spawnPosition = GetSpawnPositionWithJitter(spawnPoint);
                    spawnRotation = spawnPoint.rotation;
                    if (player != null)
                    {
                        Vector3 flatToPlayer = player.position - spawnPosition;
                        flatToPlayer.y = 0f;
                        if (flatToPlayer.sqrMagnitude > 0.001f)
                        {
                            spawnRotation = Quaternion.LookRotation(flatToPlayer.normalized, Vector3.up);
                        }
                    }
                    RegisterSpawnPointUse(spawnIndex);
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }
        else if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // Use spawn point
            int spawnIndex = FindBestSpawnPointIndex();
            if (spawnIndex < 0)
            {
                return;
            }

            Transform spawnPoint = spawnPoints[spawnIndex];
            spawnPosition = GetSpawnPositionWithJitter(spawnPoint);
            spawnRotation = spawnPoint.rotation;
            if (player != null)
            {
                Vector3 flatToPlayer = player.position - spawnPosition;
                flatToPlayer.y = 0f;
                if (flatToPlayer.sqrMagnitude > 0.001f)
                {
                    spawnRotation = Quaternion.LookRotation(flatToPlayer.normalized, Vector3.up);
                }
            }
            RegisterSpawnPointUse(spawnIndex);
        }
        else
        {
            return;
        }

        spawnPosition = ResolveSpawnGroundPosition(spawnPosition);

        GameObject enemy = Instantiate(prefab, spawnPosition, spawnRotation);
        EnemySpawned?.Invoke(enemy);
        Debug.Log($"SurvivalEnemySpawner: Spawned enemy '{prefab.name}' at {spawnPosition}", enemy);
        EnemyMaterialFixer.FixObjectMaterials(enemy, false);
        EnsureEnemyRuntimeComponents(enemy);

        SnapSpawnedEnemyToGround(enemy, spawnPosition.y + spawnGroundOffset);

        // Force immediate grounding after spawn to prevent floating zombies.
        ZombieChaseAI chaseAI = enemy.GetComponent<ZombieChaseAI>();
        if (chaseAI != null)
        {
            chaseAI.ForceGroundSnap();
        }

        if (player != null)
        {
            enemy.transform.LookAt(new Vector3(player.position.x, enemy.transform.position.y, player.position.z));
        }

        aliveEnemies.Add(enemy);
        StartCoroutine(ApplySpawnSeparationPhysics(enemy));

        if (debugTrackOneSpawnedEnemy && !debugEnemyTracked)
        {
            debugEnemyTracked = true;
            StartCoroutine(DebugTrackEnemyPosition(enemy));
        }
    }

    private void AutoAssignPlayerIfMissing()
    {
        if (player != null)
        {
            return;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            player = taggedPlayer.transform;
            return;
        }

        if (Camera.main != null)
        {
            player = Camera.main.transform;
        }
    }

    private void AutoAssignSpawnPointsIfMissing()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            return;
        }

        SurvivalSpawnPoint[] found = FindObjectsOfType<SurvivalSpawnPoint>(true);
        if (found == null || found.Length == 0)
        {
            return;
        }

        List<Transform> points = new List<Transform>(found.Length);
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null)
            {
                points.Add(found[i].transform);
            }
        }

        if (points.Count > 0)
        {
            spawnPoints = points.ToArray();
        }
    }

    private void RebuildGeneratedSpawnPoints()
    {
#if UNITY_EDITOR
        for (int i = generatedSpawnPointObjects.Count - 1; i >= 0; i--)
        {
            if (generatedSpawnPointObjects[i] != null)
            {
                DestroyImmediate(generatedSpawnPointObjects[i]);
            }
        }
#else
        for (int i = generatedSpawnPointObjects.Count - 1; i >= 0; i--)
        {
            if (generatedSpawnPointObjects[i] != null)
            {
                Destroy(generatedSpawnPointObjects[i]);
            }
        }
#endif
        generatedSpawnPointObjects.Clear();

        if (!autoGenerateSpawnPoints || generatedSpawnPointCount <= 0)
        {
            return;
        }

        AutoAssignPlayerIfMissing();
        Vector3 center = player != null ? player.position : transform.position;
        Transform parent = transform.Find("GeneratedSpawnPoints");
        if (parent == null)
        {
            GameObject container = new GameObject("GeneratedSpawnPoints");
            container.transform.SetParent(transform, false);
            parent = container.transform;
        }

        List<Transform> combined = new List<Transform>();
        if (spawnPoints != null)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                Transform existing = spawnPoints[i];
                if (existing != null)
                {
                    combined.Add(existing);
                }
            }
        }

        float angleStep = 360f / Mathf.Max(1, generatedSpawnPointCount);
        for (int i = 0; i < generatedSpawnPointCount; i++)
        {
            float angle = angleStep * i + Random.Range(-angleStep * 0.2f, angleStep * 0.2f);
            float distance = Random.Range(generatedSpawnRadiusMin, generatedSpawnRadiusMax);
            Vector3 candidate = center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance;

            Vector3 resolved = ResolveSpawnGroundPosition(candidate);
            GameObject pointObject = new GameObject($"GeneratedSpawnPoint_{i:00}");
            pointObject.transform.SetParent(parent, true);
            pointObject.transform.position = resolved + Vector3.up * generatedSpawnPointGroundOffset;
            generatedSpawnPointObjects.Add(pointObject);
            combined.Add(pointObject.transform);
        }

        spawnPoints = combined.ToArray();
    }

    private Vector3 ResolveSpawnGroundPosition(Vector3 candidatePosition)
    {
        if (!snapSpawnToGround)
        {
            return candidatePosition;
        }

        float groundY = candidatePosition.y;
        bool hasGroundHit = false;
        float rayGroundY = 0f;
        float navGroundY = 0f;
        bool hasNavHit = false;

        // Primary: raycast down to place spawn exactly on visible ground (aggressive range).
        Vector3 rayStart = candidatePosition + Vector3.up * Mathf.Max(5f, groundProbeHeight);
        RaycastHit hit;
        float rayDistance = Mathf.Max(groundProbeHeight, groundProbeDistance) + 20f;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, rayDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            rayGroundY = hit.point.y + spawnGroundOffset;
            hasGroundHit = true;
        }

        // Secondary fallback: navmesh sample with larger radius.
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(candidatePosition, out navHit, 15f, NavMesh.AllAreas))
        {
            navGroundY = navHit.position.y + spawnGroundOffset;
            hasNavHit = true;
        }

        // Tertiary fallback: sample from much higher and raycast down.
        if (!hasGroundHit && !hasNavHit)
        {
            Vector3 higherStart = candidatePosition + Vector3.up * 50f;
            if (Physics.Raycast(higherStart, Vector3.down, out hit, 100f, ~0, QueryTriggerInteraction.Ignore))
            {
                rayGroundY = hit.point.y + spawnGroundOffset;
                hasGroundHit = true;
            }
        }

        if (hasGroundHit)
        {
            groundY = rayGroundY;
        }
        else if (hasNavHit)
        {
            groundY = navGroundY;
        }

        candidatePosition.y = groundY;
        return candidatePosition;
    }

    private void SnapSpawnedEnemyToGround(GameObject enemy, float desiredGroundY)
    {
        if (enemy == null)
        {
            return;
        }

        EnsureEnemyHitCollider(enemy);

        // Try to snap using bounds, but fallback to direct raycast if bounds don't work
        Bounds? bounds = GetEnemyWorldBounds(enemy);
        if (bounds != null)
        {
            float currentMinY = bounds.Value.min.y;
            float delta = desiredGroundY - currentMinY;
            if (Mathf.Abs(delta) > 0.001f)
            {
                enemy.transform.position += Vector3.up * delta;
            }
        }
        else
        {
            // Fallback: just set Y directly if no bounds found
            Vector3 pos = enemy.transform.position;
            pos.y = desiredGroundY;
            enemy.transform.position = pos;
        }

        // Force ground snap via raycast as final verification
        ForceGroundSnapViaRaycast(enemy);
    }

    private void ForceGroundSnapViaRaycast(GameObject enemy)
    {
        if (enemy == null) return;

        Vector3 pos = enemy.transform.position;
        RaycastHit hit;

        // Raycast down from current position
        if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out hit, 10f, ~0, QueryTriggerInteraction.Ignore))
        {
            pos.y = hit.point.y + spawnGroundOffset;
            enemy.transform.position = pos;
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

    private int FindBestSpawnPointIndex()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return -1;
        }

        int bestIndex = -1;
        float bestScore = float.MinValue;

        for (int attempt = 0; attempt < Mathf.Max(1, maxSpawnPointPickAttempts); attempt++)
        {
            int candidateIndex = Random.Range(0, spawnPoints.Length);
            Transform candidate = spawnPoints[candidateIndex];
            if (candidate == null)
            {
                continue;
            }

            if (recentSpawnPointIndices.Contains(candidateIndex))
            {
                continue;
            }

            if (!IsSpawnPointValid(candidate))
            {
                continue;
            }

            float score = ScoreSpawnPoint(candidate);
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = candidateIndex;
            }
        }

        if (bestIndex >= 0)
        {
            return bestIndex;
        }

        // Fallback: accept any valid point if cooldown or attempts were too restrictive.
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null && IsSpawnPointValid(spawnPoints[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsSpawnPointValid(Transform point)
    {
        if (point == null)
        {
            return false;
        }

        if (player == null)
        {
            return true;
        }

        Vector3 toPoint = point.position - player.position;
        float horizontalDistance = Vector3.ProjectOnPlane(toPoint, Vector3.up).magnitude;

        if (horizontalDistance < minSpawnDistanceFromPlayer)
        {
            return false;
        }

        if (maxSpawnDistanceFromPlayer > 0f && horizontalDistance > maxSpawnDistanceFromPlayer)
        {
            return false;
        }

        SurvivalSpawnPoint setup = point.GetComponent<SurvivalSpawnPoint>();
        if (setup != null)
        {
            if (waveIndex < Mathf.Max(1, setup.minWave))
            {
                return false;
            }

            if (setup.allowWhenVisibleToPlayer)
            {
                return true;
            }
        }

        if (!requireSpawnOutOfSight)
        {
            return true;
        }

        Vector3 eye = player.position + Vector3.up * 1.6f;
        Vector3 target = point.position + Vector3.up * 1.2f;
        Vector3 direction = target - eye;
        float rayDistance = direction.magnitude;

        if (rayDistance <= 0.01f)
        {
            return false;
        }

        bool blocked = Physics.Raycast(eye, direction.normalized, rayDistance, visibilityBlockers, QueryTriggerInteraction.Ignore);
        return blocked;
    }

    private float ScoreSpawnPoint(Transform point)
    {
        if (player == null)
        {
            return 1f;
        }

        float distance = Vector3.Distance(point.position, player.position);
        float distanceScore = Mathf.Clamp01((distance - minSpawnDistanceFromPlayer) / Mathf.Max(1f, maxSpawnDistanceFromPlayer - minSpawnDistanceFromPlayer));

        float weight = 1f;
        SurvivalSpawnPoint setup = point.GetComponent<SurvivalSpawnPoint>();
        if (setup != null)
        {
            weight = Mathf.Max(0.1f, setup.spawnWeight);
        }

        return distanceScore * weight + Random.Range(0f, 0.2f);
    }

    private Vector3 GetSpawnPositionWithJitter(Transform point)
    {
        float jitterRadius = 0f;
        SurvivalSpawnPoint setup = point.GetComponent<SurvivalSpawnPoint>();
        if (setup != null)
        {
            jitterRadius = Mathf.Max(0f, setup.horizontalJitterRadius);
        }

        if (jitterRadius <= 0.01f)
        {
            return point.position;
        }

        Vector2 circle = Random.insideUnitCircle * jitterRadius;
        return point.position + new Vector3(circle.x, 0f, circle.y);
    }

    private void RegisterSpawnPointUse(int spawnIndex)
    {
        recentSpawnPointIndices.Add(spawnIndex);
        int cooldown = Mathf.Clamp(spawnPointCooldownCount, 0, Mathf.Max(0, spawnPoints.Length - 1));
        while (recentSpawnPointIndices.Count > cooldown)
        {
            recentSpawnPointIndices.RemoveAt(0);
        }
    }

    private void EnsureEnemyRuntimeComponents(GameObject enemy)
    {
        if (enemy == null)
        {
            return;
        }

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health == null)
        {
            health = enemy.AddComponent<EnemyHealth>();
        }

        EnemyHealth[] childHealths = enemy.GetComponentsInChildren<EnemyHealth>(true);
        for (int i = 0; i < childHealths.Length; i++)
        {
            if (childHealths[i] != null && childHealths[i] != health)
            {
                Destroy(childHealths[i]);
            }
        }

        // Subscribe to health's death event to trigger spawner's OnEnemyKilled
        EnemyHealth.EnemyKilled += (h) =>
        {
            if (h.gameObject == enemy)
            {
                OnEnemyKilled?.Invoke();
            }
        };

        ZombieChaseAI ai = enemy.GetComponent<ZombieChaseAI>();
        if (ai == null)
        {
            ai = enemy.AddComponent<ZombieChaseAI>();
        }

        ZombieChaseAI[] aiInstances = enemy.GetComponentsInChildren<ZombieChaseAI>(true);
        for (int i = 0; i < aiInstances.Length; i++)
        {
            if (aiInstances[i] != null && aiInstances[i] != ai)
            {
                aiInstances[i].enabled = false;
            }
        }

        ZombieDeath death = enemy.GetComponent<ZombieDeath>();
        if (death == null)
        {
            death = enemy.GetComponentInChildren<ZombieDeath>();
            if (death == null)
            {
                death = enemy.AddComponent<ZombieDeath>();
            }
        }

        ZombieProceduralAnimation procedural = enemy.GetComponentInChildren<ZombieProceduralAnimation>();
        if (procedural == null)
        {
            procedural = enemy.AddComponent<ZombieProceduralAnimation>();
        }

        // Ensure enemy has audio player and assign clips from spawner defaults
        EnemyAudioPlayer audioPlayer = enemy.GetComponent<EnemyAudioPlayer>();
        if (audioPlayer == null)
        {
            audioPlayer = enemy.AddComponent<EnemyAudioPlayer>();
        }

        if (enemyFootstepClip != null) audioPlayer.footstepClip = enemyFootstepClip;
        if (enemyAttackClip != null) audioPlayer.attackClip = enemyAttackClip;
        if (enemyHurtClip != null) audioPlayer.hurtClip = enemyHurtClip;
        if (enemyDeathClip != null) audioPlayer.deathClip = enemyDeathClip;

        health.destroyOnDeath = false;
        death.enableRagdollOnDeath = false;
        death.disableAnimatorOnDeath = true;
        death.destroyAfterSeconds = 2f;
        death.dropToGroundOnDeath = true;

        if (ai.target == null && player != null)
        {
            ai.target = player;
        }

        ai.keepSameHeightAsTarget = false;

        ai.animator = EnsureAnimatorOnSpawn(enemy, ai.animator);
        ai.ConfigureAnimator(ai.animator);

        procedural.chaseAI = ai;

        if (ai.animator != null)
        {
            ai.animator.applyRootMotion = false;
            if (procedural.visualRoot == null)
            {
                procedural.visualRoot = ai.animator.transform;
            }
        }

        bool hasControllerAnimation = ai.animator != null && ai.animator.runtimeAnimatorController != null;
        procedural.enabled = !hasControllerAnimation;

        ai.ApplyWaveScaling(Mathf.Max(1, waveIndex));
        ai.SetCombatTuning(directedSpeedScale, directedDamageScale);

        float healthScale = 1f + (Mathf.Max(1, waveIndex) - 1) * 0.06f;
        health.ConfigureMaxHealth(60f * healthScale * directedHealthScale, true);

        // Make AI more eager to attack: ensure reasonable attack range and faster attack cadence.
        if (ai != null)
        {
            ai.attackRange = Mathf.Max(1.6f, ai.attackRange);
            ai.attackCooldown = Mathf.Max(0.35f, ai.attackCooldown * 0.6f);
            ai.moveSpeed = Mathf.Max( ai.moveSpeed, 3.6f );
            ai.enabled = true;
        }

        // Ensure generic movement backup is available for special enemy types
        GenericEnemyMovement genericMovement = enemy.GetComponent<GenericEnemyMovement>();
        if (genericMovement == null)
        {
            genericMovement = enemy.AddComponent<GenericEnemyMovement>();
        }
        if (player != null && genericMovement.target == null)
        {
            genericMovement.target = player;
        }
        genericMovement.moveSpeed = ai != null ? ai.moveSpeed : 3.5f;
        // Keep it disabled by default - only enable if primary AI fails
        genericMovement.enabled = false;

        // Ensure root rigidbody exists and is initialized to avoid spawn-launching/floating issues.
        Rigidbody rootRb = enemy.GetComponent<Rigidbody>();
        if (rootRb == null)
        {
            rootRb = enemy.GetComponentInChildren<Rigidbody>();
        }

        if (rootRb == null)
        {
            rootRb = enemy.AddComponent<Rigidbody>();
        }

        if (rootRb != null)
        {
            rootRb.isKinematic = true;
            rootRb.useGravity = false;
            rootRb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rootRb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        // Make child rigidbodies kinematic unless they are explicit ragdoll pieces (tagged "Ragdoll").
        Rigidbody[] allRbs = enemy.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < allRbs.Length; i++)
        {
            Rigidbody crb = allRbs[i];
            if (crb == null) continue;
            if (rootRb != null && crb == rootRb) continue;
            if (crb.CompareTag("Ragdoll"))
            {
                crb.isKinematic = false;
                crb.useGravity = true;
            }
            else
            {
                crb.isKinematic = true;
                crb.useGravity = false;
            }
        }
    }

    private void EnsureEnemyHitCollider(GameObject enemy)
    {
        if (enemy == null)
        {
            return;
        }

        Collider existingCollider = enemy.GetComponentInChildren<Collider>(true);
        if (existingCollider != null)
        {
            return;
        }

        Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        CapsuleCollider capsule = enemy.AddComponent<CapsuleCollider>();
        capsule.center = enemy.transform.InverseTransformPoint(bounds.center);
        capsule.radius = Mathf.Max(0.2f, Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.45f);
        capsule.height = Mathf.Max(capsule.radius * 2f, bounds.size.y * 0.9f);
        capsule.direction = 1;
        capsule.isTrigger = false;
    }

    // Small separation pass to move newly spawned enemy away from nearby alive enemies
    private void ApplySpawnSeparation(GameObject newEnemy)
    {
        if (newEnemy == null || aliveEnemies == null || aliveEnemies.Count == 0)
            return;

        Vector3 newPos = newEnemy.transform.position;
        float minDist = Mathf.Max(0.01f, spawnSeparationDistance);
        Rigidbody newRb = newEnemy.GetComponent<Rigidbody>();
        if (newRb == null)
        {
            newRb = newEnemy.GetComponentInChildren<Rigidbody>();
        }

        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            GameObject other = aliveEnemies[i];
            if (other == null || other == newEnemy) continue;

            Vector3 otherPos = other.transform.position;
            Vector3 delta = newPos - otherPos;
            float dist = delta.magnitude;
            if (dist <= 0.001f)
            {
                // random nudge if exactly overlapping
                delta = Random.onUnitSphere;
                delta.y = 0f;
                dist = 0.01f;
            }

            if (dist < minDist)
            {
                float push = (minDist - dist) + 0.02f;
                Vector3 offset = delta.normalized * push;
                newPos += offset;

                Rigidbody otherRb = other.GetComponent<Rigidbody>();
                if (otherRb == null)
                {
                    otherRb = other.GetComponentInChildren<Rigidbody>();
                }

                Vector3 pushDir = new Vector3(offset.x, 0f, offset.z).normalized;
                if (newRb != null && !newRb.isKinematic)
                {
                    newRb.AddForce(pushDir * spawnSeparationImpulse, ForceMode.Impulse);
                }

                if (otherRb != null && !otherRb.isKinematic)
                {
                    otherRb.AddForce(-pushDir * spawnSeparationImpulse, ForceMode.Impulse);
                }
            }
        }

        newEnemy.transform.position = newPos;
    }

    private IEnumerator ApplySpawnSeparationPhysics(GameObject newEnemy)
    {
        if (newEnemy == null)
        {
            yield break;
        }

        ZombieChaseAI ai = newEnemy.GetComponent<ZombieChaseAI>();
        Rigidbody rb = newEnemy.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = newEnemy.GetComponentInChildren<Rigidbody>();
        }

        if (rb == null)
        {
            rb = newEnemy.AddComponent<Rigidbody>();
        }

        bool aiWasEnabled = ai != null && ai.enabled;
        if (ai != null)
        {
            ai.enabled = false;
        }

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

        ApplySpawnSeparation(newEnemy);

        yield return new WaitForFixedUpdate();

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        if (ai != null)
        {
            ai.enabled = aiWasEnabled;
        }
    }

    private IEnumerator DebugTrackEnemyPosition(GameObject enemy)
    {
        if (enemy == null)
        {
            yield break;
        }

        int frameLimit = Mathf.Max(1, debugTrackEnemyFrames);
        for (int frame = 0; frame < frameLimit; frame++)
        {
            if (enemy == null)
            {
                yield break;
            }

            Rigidbody rb = enemy.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = enemy.GetComponentInChildren<Rigidbody>();
            }

            ZombieChaseAI ai = enemy.GetComponent<ZombieChaseAI>();
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            Debug.Log(
                $"SurvivalEnemySpawner DEBUG frame {frame}: pos={enemy.transform.position}, rbKinematic={(rb != null && rb.isKinematic)}, rbVel={(rb != null ? rb.linearVelocity : Vector3.zero)}, aiEnabled={(ai != null && ai.enabled)}, agentEnabled={(agent != null && agent.enabled)}, agentOnNavMesh={(agent != null && agent.isOnNavMesh)}, agentNextPos={(agent != null ? agent.nextPosition : Vector3.zero)}, agentVel={(agent != null ? agent.velocity : Vector3.zero)}, agentBaseOffset={(agent != null ? agent.baseOffset : 0f)}",
                enemy);

            yield return null;
        }
    }

    private void AutoAssignEnemyAudioIfMissing()
    {
        if (enemyFootstepClip != null)
        {
            return;
        }

#if UNITY_EDITOR
        string[] searchFolders = new[]
        {
            "Assets/Audio/EnemiesFootsteps",
            "Assets/audi/EnemyFootsteps"
        };

        for (int folderIndex = 0; folderIndex < searchFolders.Length; folderIndex++)
        {
            string folder = searchFolders[folderIndex];
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
            {
                continue;
            }

            string[] clipGuids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
            for (int i = 0; i < clipGuids.Length; i++)
            {
                string clipPath = UnityEditor.AssetDatabase.GUIDToAssetPath(clipGuids[i]);
                AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip != null)
                {
                    enemyFootstepClip = clip;
                    UnityEditor.EditorUtility.SetDirty(this);
                    return;
                }
            }
        }
#endif
    }

    private Animator EnsureAnimatorOnSpawn(GameObject enemy, Animator current)
    {
        Animator animator = current;
        if (animator == null)
        {
            animator = enemy.GetComponentInChildren<Animator>(true);
        }

        if (animator == null)
        {
            SkinnedMeshRenderer mesh = enemy.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (mesh != null)
            {
                animator = mesh.gameObject.AddComponent<Animator>();
            }
            else
            {
                animator = enemy.AddComponent<Animator>();
            }
        }

        // Assign default animator controller if available.
        if (animator.runtimeAnimatorController == null && defaultZombieAnimatorController != null)
        {
            animator.runtimeAnimatorController = defaultZombieAnimatorController;
        }

        // Fallback: search for any animator controller in the prefab hierarchy.
        if (animator.runtimeAnimatorController == null)
        {
            Animator[] allAnimators = enemy.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < allAnimators.Length; i++)
            {
                if (allAnimators[i] != null && allAnimators[i].runtimeAnimatorController != null)
                {
                    animator.runtimeAnimatorController = allAnimators[i].runtimeAnimatorController;
                    break;
                }
            }
        }

        // Ensure animator is properly enabled and configured.
        if (animator != null)
        {
            animator.enabled = true;
            animator.applyRootMotion = false;
        }

        return animator;
    }

    private void AutoAssignDefaultAnimatorControllerIfMissing()
    {
        if (defaultZombieAnimatorController != null || enemyPrefabs == null)
        {
            return;
        }

        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            GameObject prefab = enemyPrefabs[i];
            if (prefab == null)
            {
                continue;
            }

            Animator animator = prefab.GetComponentInChildren<Animator>(true);
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                defaultZombieAnimatorController = animator.runtimeAnimatorController;
                break;
            }
        }
    }

    private void AutoAssignEnemyPrefabsIfMissing()
    {
#if UNITY_EDITOR
        List<GameObject> foundPrefabs = new List<GameObject>();

        if (enemyPrefabs != null && enemyPrefabs.Length > 0)
        {
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                GameObject existing = enemyPrefabs[i];
                if (IsValidEnemyPrefab(existing))
                {
                    foundPrefabs.Add(existing);
                }
            }
        }

        string[] prefabGuids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Enemies" });

        void AddEnemyFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string lower = path.ToLowerInvariant();
            bool looksLikeEnemy = lower.Contains("zombie") || lower.Contains("mutant") || lower.Contains("enemy") || lower.Contains("monster") || lower.Contains("assassination") || lower.Contains("fight") || lower.Contains("run") || lower.Contains("idle");
            if (!looksLikeEnemy)
            {
                return;
            }

            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!IsValidEnemyPrefab(prefab))
            {
                return;
            }

            if (!foundPrefabs.Contains(prefab))
            {
                foundPrefabs.Add(prefab);
            }

            Animator animator = prefab.GetComponentInChildren<Animator>(true);
            if (defaultZombieAnimatorController == null && animator != null && animator.runtimeAnimatorController != null)
            {
                defaultZombieAnimatorController = animator.runtimeAnimatorController;
            }
        }

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            AddEnemyFromPath(path);
        }

        foundPrefabs.Sort((left, right) => GetEnemySpawnWeight(left).CompareTo(GetEnemySpawnWeight(right)));

        // Keep only one copy of each entry and write the discovered list back.
        if (foundPrefabs.Count > 0)
        {
            Debug.Log($"SurvivalEnemySpawner: Discovered {foundPrefabs.Count} valid enemy prefab(s): {string.Join(", ", foundPrefabs.Select(p => p.name))}");
            enemyPrefabs = foundPrefabs.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
        else
        {
            Debug.LogWarning("SurvivalEnemySpawner: No valid enemy prefabs discovered in Assets/Enemies");
        }
#endif
    }

#if UNITY_EDITOR
    private bool IsValidEnemyPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            return false;
        }

        string path = UnityEditor.AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        Animator animator = prefab.GetComponentInChildren<Animator>(true);
        SkinnedMeshRenderer skinned = prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
        return animator != null || skinned != null;
    }
#endif

    private GameObject PickEnemyPrefab()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            return null;
        }

        if (enemyPrefabs.Length == 1)
        {
            return enemyPrefabs[0];
        }

        float totalWeight = 0f;
        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            totalWeight += Mathf.Max(0.05f, GetEnemySpawnWeight(enemyPrefabs[i]));
        }

        float roll = Random.Range(0f, totalWeight);
        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            roll -= Mathf.Max(0.05f, GetEnemySpawnWeight(enemyPrefabs[i]));
            if (roll <= 0f)
            {
                return enemyPrefabs[i];
            }
        }

        return enemyPrefabs[enemyPrefabs.Length - 1];
    }

    private float GetEnemySpawnWeight(GameObject prefab)
    {
        if (prefab == null)
        {
            return 1f;
        }

        string lower = prefab.name.ToLowerInvariant();
        if (lower.Contains("fat") || lower.Contains("brute") || lower.Contains("heavy") || lower.Contains("boss"))
        {
            return 0.1f;
        }

        if (lower.Contains("mutant") || lower.Contains("monster"))
        {
            return 0.35f;
        }

        if (lower.Contains("runner") || lower.Contains("fast") || lower.Contains("light"))
        {
            return 1.5f;
        }

        return 1f;
    }

    private void CleanupDeadEnemies()
    {
        bool removedEnemy = false;
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = aliveEnemies[i];
            if (enemy == null)
            {
                aliveEnemies.RemoveAt(i);
                OnEnemyKilled?.Invoke();
                removedEnemy = true;
                continue;
            }

            // Check if enemy is dead, destroyed, or inactive.
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            bool isDead = health != null && health.IsDead;
            bool isInactive = !enemy.activeInHierarchy;
            
            if (isDead || isInactive)
            {
                aliveEnemies.RemoveAt(i);
                OnEnemyKilled?.Invoke();
                removedEnemy = true;
            }
        }

        if (removedEnemy && replaceDeadEnemiesContinuously && !replacementSpawnQueued && activeRoutine != null)
        {
            if (aliveEnemies.Count < Mathf.Max(1, minimumAliveEnemyCount))
            {
                StartCoroutine(SpawnReplacementAfterDelay());
            }
        }
    }

    private IEnumerator SpawnReplacementAfterDelay()
    {
        replacementSpawnQueued = true;
        yield return new WaitForSeconds(Mathf.Max(0f, replacementSpawnDelay));

        while (replaceDeadEnemiesContinuously && aliveEnemies.Count < Mathf.Max(1, minimumAliveEnemyCount))
        {
            SpawnEnemy();
            yield return new WaitForSeconds(Mathf.Max(0.05f, replacementSpawnDelay));
        }

        replacementSpawnQueued = false;
    }

    private bool TryGetRandomMapSpawnPosition(out Vector3 spawnPosition, out Quaternion spawnRotation)
    {
        spawnPosition = Vector3.zero;
        spawnRotation = Quaternion.identity;

        if (player == null)
        {
            return false;
        }

        // Try multiple random attempts to find a valid spawn position
        for (int attempt = 0; attempt < 10; attempt++)
        {
            // Generate random position in a ring around the player
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(minSpawnDistanceFromPlayer, maxSpawnDistanceFromPlayer);
            
            Vector3 randomOffset = new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance
            );

            Vector3 candidatePosition = player.position + randomOffset;

            // Check out-of-sight requirement
            if (requireSpawnOutOfSight)
            {
                Vector3 eye = player.position + Vector3.up * 1.6f;
                Vector3 target = candidatePosition + Vector3.up * 1.2f;
                Vector3 direction = target - eye;
                float rayDistance = direction.magnitude;

                if (rayDistance > 0.01f)
                {
                    bool blocked = Physics.Raycast(eye, direction.normalized, rayDistance, visibilityBlockers, QueryTriggerInteraction.Ignore);
                    if (!blocked)
                    {
                        continue; // Not blocked from view, skip this spot
                    }
                }
            }

            // Try to get ground position
            candidatePosition = ResolveSpawnGroundPosition(candidatePosition);

            // Validate with NavMesh if available
            NavMeshHit navHit;
            if (!NavMesh.SamplePosition(candidatePosition, out navHit, 3f, NavMesh.AllAreas))
            {
                continue; // Position not on NavMesh, skip
            }

            spawnPosition = navHit.position;

            // Quick sanity checks: reject positions extremely far above/below player (likely under/over map)
            if (Mathf.Abs(spawnPosition.y - player.position.y) > 40f)
            {
                continue;
            }

            // Probe downward from a small height to ensure there is visible ground close by
            RaycastHit groundProbeHit;
            Vector3 probeStart = spawnPosition + Vector3.up * 6f;
            if (!Physics.Raycast(probeStart, Vector3.down, out groundProbeHit, 12f, ~0, QueryTriggerInteraction.Ignore))
            {
                // No nearby visible ground, skip this candidate
                continue;
            }

            // Use the precise ground point for spawn to avoid tiny floating offsets or being inside geometry
            spawnPosition = groundProbeHit.point;

            // Calculate rotation toward player
            Vector3 flatToPlayer = player.position - spawnPosition;
            flatToPlayer.y = 0f;
            if (flatToPlayer.sqrMagnitude > 0.001f)
            {
                spawnRotation = Quaternion.LookRotation(flatToPlayer.normalized, Vector3.up);
            }
            else
            {
                spawnRotation = Quaternion.identity;
            }

            return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(player.position, Mathf.Max(0f, minSpawnDistanceFromPlayer));

        if (maxSpawnDistanceFromPlayer > 0f)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.7f, 0.8f);
            Gizmos.DrawWireSphere(player.position, maxSpawnDistanceFromPlayer);
        }
    }
}
