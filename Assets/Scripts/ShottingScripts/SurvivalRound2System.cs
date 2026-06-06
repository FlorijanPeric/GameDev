/*
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
private int currentRound = 1;

private void HandleEnemyKilled()
{
    totalEnemiesKilled++;

    if (debugLogRound2)
        Debug.Log($"Kills: {totalEnemiesKilled}");

    if (totalEnemiesKilled >= killsRequiredForRound2)
    {
        ActivateNextRound();
    }
}
    private void ActivateRound2()
    {
        currentRound++;

    if (currentRound > 10)
    {
        if (round2AnnounceText != null)
            round2AnnounceText.text = "FINAL ROUND";
        return;
    }

    Debug.Log($"=== ROUND {currentRound} ===");

    AnnounceRound2(currentRound);

    // scale difficulty
    enemySpawner.spawnIntervalMultiplier *= 0.85f;
    enemySpawner.maxAliveEnemies += 3;
    enemySpawner.waveSizeBonus += 2;

    SpawnWave(currentRound);

    killsRequiredForRound2 += 40;
    totalEnemiesKilled = 0;

    if (currentRound == 2)
    {
        ActivateRound2Extras();
    }
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

    private void ActivateRound2Extras()
{
    Debug.Log("ROUND 2 SPECIAL EFFECTS");


    enemySpawner.replaceDeadEnemiesContinuously = false;
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
       int amount = round2EnemySpawnWave + (round * 5);

    for (int i = 0; i < amount; i++)
    {
        enemySpawner.ForceSpawnEnemy(Random.Range(0, enemySpawner.enemyPrefabs.Length));
    }
    }

    public bool IsRound2Active => round2Active;
    public int TotalEnemiesKilled => totalEnemiesKilled;


private void ActivateNextRound()
{
    if (round2Active) return;

    round2Active = true;

    currentRound++;

    Debug.Log($"=== ROUND {currentRound} ===");

    AnnounceRound(currentRound);

    // scale difficulty each round
    enemySpawner.spawnIntervalMultiplier *= 0.85f;
    enemySpawner.maxAliveEnemies += 3;
    enemySpawner.waveSizeBonus += 2;

    SpawnWave(currentRound);

    // reset kills for next round
    totalEnemiesKilled = 0;

    round2Active = false;
}

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
*/
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SurvivalRound2System : MonoBehaviour
{
    [Header("Round Settings")]
    public int killsRequiredBase = 40;
    public int maxRounds = 10;

    [Header("Enemy Scaling")]
    public int baseWaveSize = 15;
    public float spawnRateMultiplierStep = 0.85f;
    public int maxAliveBonusPerRound = 3;
    public int waveBonusPerRound = 2;

    [Header("References")]
    public SurvivalEnemySpawner enemySpawner;
    public Text roundText;
    public AudioSource audioSource;
    public AudioClip roundClip;

    private int currentRound = 1;
    private int killCount = 0;
    private int killsRequired;

    private bool transitioning = false;

    void Start()
    {
        if (enemySpawner == null)
            enemySpawner = FindObjectOfType<SurvivalEnemySpawner>();

        killsRequired = killsRequiredBase;

        if (enemySpawner != null)
            enemySpawner.OnEnemyKilled += OnEnemyKilled;
    }

    void OnDestroy()
    {
        if (enemySpawner != null)
            enemySpawner.OnEnemyKilled -= OnEnemyKilled;
    }



    IEnumerator NextRound()
{
    transitioning = true;

    yield return new WaitForSeconds(1f);

    currentRound++;

    if (currentRound > maxRounds)
    {
        ShowText("FINAL ROUND");
        transitioning = false;
        yield break;
    }

    ShowText("ROUND " + currentRound);

    ApplyScaling();

    yield return new WaitForSeconds(0.2f);

    SpawnWave();

    killCount = 0;
    killsRequired += 40;

    yield return new WaitForSeconds(1f);

    transitioning = false;
}

    void ApplyScaling()
    {
        if (enemySpawner == null) return;

        enemySpawner.spawnIntervalMultiplier *= spawnRateMultiplierStep;
        enemySpawner.maxAliveEnemies += maxAliveBonusPerRound;
        enemySpawner.waveSizeBonus += waveBonusPerRound;

        enemySpawner.replaceDeadEnemiesContinuously = true;
    }

    void SpawnWave()
{
    if (enemySpawner == null || enemySpawner.enemyPrefabs == null || enemySpawner.enemyPrefabs.Length == 0)
    {
        Debug.LogWarning("No enemy prefabs assigned!");
        return;
    }

    int amount = baseWaveSize + (currentRound * 5);

    for (int i = 0; i < amount; i++)
    {
        enemySpawner.ForceSpawnEnemy(
            Random.Range(0, enemySpawner.enemyPrefabs.Length)
        );
    }
}
   public void OnEnemyKilled()
{
    if (transitioning || enemySpawner == null) return;

    killCount++;

    Debug.Log($"Kill Count: {killCount}/{killsRequired}");

    if (killCount >= killsRequired)
    {
        if (!transitioning)
            StartCoroutine(NextRound());
    }
}

    void ShowText(string msg)
    {
     if (roundText == null) return;

        StartCoroutine(ShowMsg(msg));
    }

    IEnumerator ShowMsg(string msg)
    {
        roundText.text = msg;
        roundText.gameObject.SetActive(true);

        yield return new WaitForSeconds(3f);

        roundText.gameObject.SetActive(false);
    }
}