
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Round 2 system: Triggers after 40 enemy kills.
/// Expands map, increases spawn rate, spawns enemies from all directions with higher intensity.
/// </summary>
public class SurvivalRound2System : MonoBehaviour
{
    [Header("Round 2 Trigger")]
    public int killsRequiredForRound2 = 40;
    public int round2EnemySpawnWave = 15; // Number of enemies that spawn at round 2 start

    [Header("Round 2 Multipliers")]
    public float round2SpawnRateMultiplier = 2.2f; // Spawn enemies 2.2x faster
    public float round2MaxEnemiesMultiplier = 1.8f; // Allow more enemies alive at once
    public int round2WaveSizeBonus = 12; // Extra enemies per wave

    [Header("References")]
    public SurvivalEnemySpawner enemySpawner;
    public MapEnlargementSystem mapEnlarger;
    public Text round2AnnounceText; // UI text to show "ROUND 2!" message
    public AudioSource round2AudioSource;
    public AudioClip round2AudioClip;
    [Range(0f,1f)] public float round2AudioVolume = 1f;

    [Header("Debug")]
    public bool debugLogRound2 = true;

    private int totalEnemiesKilled = 0;
    private bool round2Active = false;
    private float originalSpawnInterval;
    private int originalMaxAliveEnemies;
    private int originalWaveSizeBonus;
    private float worldGroundYReference;
    private void Start()
    {
        if (enemySpawner == null)
        {
            enemySpawner = FindObjectOfType<SurvivalEnemySpawner>();
        }

        if (mapEnlarger == null)
        {
            mapEnlarger = FindObjectOfType<MapEnlargementSystem>();
        }

        // Auto-assign audio source if missing
        if (round2AudioSource == null)
        {
            round2AudioSource = FindObjectOfType<AudioSource>();
        }

        // Subscribe to enemy kill event
        if (enemySpawner != null)
        {
            enemySpawner.OnEnemyKilled += HandleEnemyKilled;
        }

        // Cache original spawner settings
        if (enemySpawner != null)
        {
            originalSpawnInterval = enemySpawner.timeBetweenSpawns;
            originalMaxAliveEnemies = enemySpawner.maxAliveEnemies;
            originalWaveSizeBonus = enemySpawner.waveSizeBonus;
        }
        worldGroundYReference = transform.position.y;
    }

    private void OnDestroy()
    {
        if (enemySpawner != null)
        {
            enemySpawner.OnEnemyKilled -= HandleEnemyKilled;
        }
    }

    private void HandleEnemyKilled()
    {
        totalEnemiesKilled++;

        if (debugLogRound2)
        {
            Debug.Log($"SurvivalRound2System: Enemy killed. Total kills: {totalEnemiesKilled}/{killsRequiredForRound2}");
        }

        if (!round2Active && totalEnemiesKilled >= killsRequiredForRound2)
        {
            StartCoroutine(NextRoundLoop());
        }
    }

    private void ActivateRound2()
    {
        round2Active = true;

        Debug.Log("=== ROUND 2 ACTIVATED ===");

        // Announce Round 2
        AnnounceRound2();

    
        

        // Update spawner settings for increased difficulty
        if (enemySpawner != null)
        {
            // Reduce spawn interval (faster spawning)
            enemySpawner.spawnIntervalMultiplier = round2SpawnRateMultiplier;

            // Allow more enemies alive
            enemySpawner.maxAliveEnemies = Mathf.RoundToInt(originalMaxAliveEnemies * round2MaxEnemiesMultiplier);

            // Increase wave size
            enemySpawner.waveSizeBonus = originalWaveSizeBonus + round2WaveSizeBonus;

            // Enable continuous reinforcement if not already
            enemySpawner.replaceDeadEnemiesContinuously = true;
            enemySpawner.minimumAliveEnemyCount = Mathf.RoundToInt(enemySpawner.minimumAliveEnemyCount * 1.5f);

            Debug.Log($"Round2: Spawn multiplier={enemySpawner.spawnIntervalMultiplier}, " +
                     $"MaxAlive={enemySpawner.maxAliveEnemies}, " +
                     $"WaveSizeBonus={enemySpawner.waveSizeBonus}");
        }
        killsRequiredForRound2 += 40;
        round2EnemySpawnWave += 5;
        // Spawn initial wave of enemies from all directions
        SpawnInitialRound2Wave();
    }

    private void AnnounceRound2()
    {
        if (round2AnnounceText != null)
        {
            StartCoroutine(ShowRound2Announcement());
        }

        // Play audio cue if available
        if (round2AudioSource != null && round2AudioClip != null)
        {
            round2AudioSource.PlayOneShot(round2AudioClip, round2AudioVolume);
        }
    }

    private System.Collections.IEnumerator ShowRound2Announcement()
    {
        round2AnnounceText.text = "ROUND 2!";
        round2AnnounceText.color = new Color(1f, 0f, 0f, 1f); // Red
        round2AnnounceText.gameObject.SetActive(true);

        yield return new WaitForSeconds(3f);

        round2AnnounceText.gameObject.SetActive(false);
    }

    private void SpawnInitialRound2Wave()
    {
        if (enemySpawner == null) return;

        Debug.Log($"SurvivalRound2System: Spawning initial Round 2 wave of {round2EnemySpawnWave} enemies");

        // Queue up a bunch of spawns from all directions
        for (int i = 0; i < round2EnemySpawnWave; i++)
        {
            enemySpawner.ForceSpawnEnemy(Random.Range(0, enemySpawner.enemyPrefabs.Length));
        }
    }

    public bool IsRound2Active => round2Active;
    public int TotalEnemiesKilled => totalEnemiesKilled;

    private IEnumerator NextRoundLoop(){
        round2Active = true;

        while (enemySpawner != null)
        {
            ActivateRound2();

            yield return new WaitForSeconds(2f);

            if (enemySpawner.CurrentWaveIndex >= 10)
            {
                 if (round2AnnounceText != null)
                     round2AnnounceText.text = "FINAL ROUND";
                     break;
            }

            totalEnemiesKilled = 0;
            round2Active = false;

            yield return new WaitForSeconds(1f);
        }
    }
}
