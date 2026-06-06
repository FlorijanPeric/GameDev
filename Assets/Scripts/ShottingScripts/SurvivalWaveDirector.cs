using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SurvivalWaveDirector : MonoBehaviour
{
    private const string BestWavePrefKey = "survival_best_wave";

    public enum RunState
    {
        Idle,
        Preparing,
        WaveActive,
        Intermission,
        Victory,
        Defeat
    }

    [Header("References")]
    public SurvivalEnemySpawner spawner;
    public Transform player;
    public PlayerSurvivalHealth playerHealth;

    [Header("Wave Flow")]
    public bool autoStart = true;
    public float initialDelay = 3f;
    public float intermissionDuration = 8f;
    public int maxWavesToWin = 10;

    [Header("Run Economy")]
    public int creditsPerKill = 10;

    [Header("Intermission Recovery")]
    [Range(0f, 1f)] public float intermissionHealPercent = 0.2f;
    public int intermissionAmmoReserveTopUp = 60;

    [Header("Intermission Upgrades")]
    public int healthUpgradeBaseCost = 80;
    public int damageUpgradeBaseCost = 100;
    public int speedUpgradeBaseCost = 90;
    public float healthUpgradeAmount = 15f;
    public float damageUpgradeStep = 0.12f;
    public float speedUpgradeStep = 0.08f;

    [Header("Restart")]
    public bool allowKeyboardRestart = true;
    public Key restartKey = Key.Enter;

    [Header("Wave Scaling")]
    public int baseEnemies = 16;
    public int enemiesPerWave = 6;
    public int baseAliveCap = 28;
    public int aliveCapPerWave = 4;
    public float baseSpawnIntervalScale = 1f;
    public float spawnIntervalScaleDropPerWave = 0.03f;
    public float minSpawnIntervalScale = 0.45f;

    [Header("Pacing")]
    public int quietWaveEvery = 3;
    public int spikeWaveEvery = 4;
    public int eliteWaveEvery = 7;
    public float quietSpawnIntervalMultiplier = 1.2f;
    public float quietIntermissionMultiplier = 1.25f;
    public float spikeEnemyMultiplier = 1.25f;
    public float spikeSpawnIntervalMultiplier = 0.78f;
    public float eliteEnemyMultiplier = 0.7f;
    public float eliteHealthMultiplier = 1.85f;
    public float eliteDamageMultiplier = 1.35f;
    public float eliteSpeedMultiplier = 1.12f;

    private int currentWave;
    private Coroutine flowRoutine;
    private RunState runState = RunState.Idle;
    private string currentPhaseLabel = "IDLE";
    private float phaseTimeRemaining;
    private int totalKills;
    private int credits;
    private int bestWave;
    private int healthUpgradeLevel;
    private int damageUpgradeLevel;
    private int speedUpgradeLevel;
    private FPSMovement cachedPlayerMovement;

    public int CurrentWave => currentWave;
    public int MaxWavesToWin => maxWavesToWin;
    public RunState CurrentState => runState;
    public string CurrentPhaseLabel => currentPhaseLabel;
    public float PhaseTimeRemaining => Mathf.Max(0f, phaseTimeRemaining);
    public int TotalKills => totalKills;
    public int Credits => credits;
    public int BestWave => bestWave;
    public int HealthUpgradeLevel => healthUpgradeLevel;
    public int DamageUpgradeLevel => damageUpgradeLevel;
    public int SpeedUpgradeLevel => speedUpgradeLevel;
    public int HealthUpgradePrice => ComputeUpgradeCost(healthUpgradeBaseCost, healthUpgradeLevel);
    public int DamageUpgradePrice => ComputeUpgradeCost(damageUpgradeBaseCost, damageUpgradeLevel);
    public int SpeedUpgradePrice => ComputeUpgradeCost(speedUpgradeBaseCost, speedUpgradeLevel);

    public bool CanBuyHealthUpgrade => credits >= HealthUpgradePrice;
    public bool CanBuyDamageUpgrade => credits >= DamageUpgradePrice;
    public bool CanBuySpeedUpgrade => credits >= SpeedUpgradePrice;

    public event System.Action<RunState, int, string> StateChanged;

    private void Start()
    {
        bestWave = Mathf.Max(0, PlayerPrefs.GetInt(BestWavePrefKey, 0));

        if (spawner == null)
        {
            spawner = FindObjectOfType<SurvivalEnemySpawner>();
        }

        ResolvePlayerReferences(true);

        if (spawner != null && spawner.player == null && player != null)
        {
            spawner.player = player;
        }

        if (spawner == null)
        {
            Debug.LogError("SurvivalWaveDirector: No SurvivalEnemySpawner found in scene.", this);
            enabled = false;
            return;
        }

        spawner.autoStartSpawnLoop = false;
        spawner.StopSpawning();

        if (autoStart)
        {
            StartDirector();
        }
    }

    private void Update()
    {
        if (runState == RunState.Intermission)
        {
            HandleIntermissionUpgradeInput();
        }

        if (!allowKeyboardRestart)
        {
            return;
        }

        if (runState != RunState.Victory && runState != RunState.Defeat)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[restartKey].wasPressedThisFrame)
        {
            RestartRun();
        }
    }

    private void OnEnable()
    {
        EnemyHealth.EnemyKilled += OnEnemyKilled;
    }

    private void OnDisable()
    {
        EnemyHealth.EnemyKilled -= OnEnemyKilled;

        if (playerHealth != null)
        {
            playerHealth.Died -= OnPlayerDied;
        }

        StopDirector();
    }

    public void StartDirector()
    {
        StopDirector();
        Time.timeScale = 1f;
        ResolvePlayerReferences(true);
        currentWave = 0;
        totalKills = 0;
        credits = 0;
        healthUpgradeLevel = 0;
        damageUpgradeLevel = 0;
        speedUpgradeLevel = 0;

        if (playerHealth != null)
        {
            playerHealth.Died -= OnPlayerDied;
            playerHealth.Died += OnPlayerDied;
            playerHealth.ResetHealth();
        }

        SetState(RunState.Preparing, "GET READY", Mathf.Max(0f, initialDelay));
        ApplyUpgradeEffects();
        flowRoutine = StartCoroutine(DirectorLoop());
    }

    public void RestartRun()
    {
        Time.timeScale = 1f;
        StartDirector();
    }

    public bool TryPurchaseHealthUpgrade()
    {
        return TryBuyUpgrade(UpgradeType.Health);
    }

    public bool TryPurchaseDamageUpgrade()
    {
        return TryBuyUpgrade(UpgradeType.Damage);
    }

    public bool TryPurchaseSpeedUpgrade()
    {
        return TryBuyUpgrade(UpgradeType.Speed);
    }

    public void StopDirector()
    {
        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
            flowRoutine = null;
        }

        if (spawner != null)
        {
            spawner.StopSpawning();
        }

        if (runState != RunState.Victory && runState != RunState.Defeat)
        {
            SetState(RunState.Idle, "IDLE", 0f);
        }
    }

    private IEnumerator DirectorLoop()
    {
        if (initialDelay > 0f)
        {
            yield return WaitRealtimeOrDefeat(initialDelay);
            if (IsPlayerDefeated())
            {
                TriggerDefeat("YOU DIED");
                yield break;
            }
        }

        while (true)
        {
            currentWave++;
            WaveProfile profile = BuildProfile(currentWave);

            int enemiesThisWave = Mathf.Max(1, baseEnemies + (currentWave - 1) * enemiesPerWave);
            int aliveCap = Mathf.Max(1, baseAliveCap + (currentWave - 1) * aliveCapPerWave);

            float intervalScale = baseSpawnIntervalScale - (currentWave - 1) * spawnIntervalScaleDropPerWave;
            intervalScale = Mathf.Max(minSpawnIntervalScale, intervalScale);

            enemiesThisWave = Mathf.Max(1, Mathf.RoundToInt(enemiesThisWave * profile.enemyCountMultiplier));
            intervalScale *= profile.spawnIntervalMultiplier;
            intervalScale = Mathf.Max(minSpawnIntervalScale * 0.75f, intervalScale);

            string waveLabel = $"WAVE {currentWave} - {profile.label}";
            SetState(RunState.WaveActive, waveLabel, 0f);
            ApplyUpgradeEffects();

            spawner.BeginDirectedWave(currentWave, enemiesThisWave, aliveCap, intervalScale, profile.healthMultiplier, profile.speedMultiplier, profile.damageMultiplier);

            while (spawner.IsDirectedWaveActive)
            {
                if (IsPlayerDefeated())
                {
                    spawner.StopSpawning();
                    TriggerDefeat("YOU DIED");
                    yield break;
                }

                yield return null;
            }

            TrySetBestWave(currentWave);

            if (maxWavesToWin > 0 && currentWave >= maxWavesToWin)
            {
                TriggerVictory();
                yield break;
            }

            float intermission = Mathf.Max(0.2f, intermissionDuration * profile.intermissionMultiplier);
            ApplyIntermissionRecovery();
            SetState(RunState.Intermission, "INTERMISSION", intermission);
            yield return WaitRealtimeOrDefeat(intermission);

            if (IsPlayerDefeated())
            {
                TriggerDefeat("YOU DIED");
                yield break;
            }
        }
    }

    private void OnPlayerDied()
    {
        TriggerDefeat("YOU DIED");
    }

    private bool IsPlayerDefeated()
    {
        ResolvePlayerReferences(false);

        if (playerHealth != null)
        {
            return playerHealth.IsDead;
        }

        // Missing player health should not auto-end the run.
        return false;
    }

    private void ResolvePlayerReferences(bool ensureHealthComponent)
    {
        if (player == null)
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
            {
                player = taggedPlayer.transform;
            }
        }

        if (player == null)
        {
            FPSMovement movement = FindObjectOfType<FPSMovement>();
            if (movement != null)
            {
                player = movement.transform;
            }
        }

        if (player == null)
        {
            PlayerInput input = FindObjectOfType<PlayerInput>();
            if (input != null)
            {
                player = input.transform;
            }
        }

        if (playerHealth == null && player != null)
        {
            playerHealth = player.GetComponent<PlayerSurvivalHealth>();
        }

        if (ensureHealthComponent && playerHealth == null && player != null)
        {
            playerHealth = player.gameObject.AddComponent<PlayerSurvivalHealth>();
        }

        if (cachedPlayerMovement == null && player != null)
        {
            cachedPlayerMovement = player.GetComponent<FPSMovement>();
        }

        if (spawner != null && spawner.player == null && player != null)
        {
            spawner.player = player;
        }
    }

    private void TriggerVictory()
    {
        if (runState == RunState.Victory)
        {
            return;
        }

        StopDirector();
        Time.timeScale = 0f;
        SetState(RunState.Victory, $"VICTORY - SURVIVED {currentWave} WAVES - BEST {bestWave} - PRESS ENTER TO RESTART", 0f);
    }

    private void TriggerDefeat(string label)
    {
        if (runState == RunState.Defeat)
        {
            return;
        }

        StopDirector();
        Time.timeScale = 0f;
        SetState(RunState.Defeat, label + " - PRESS ENTER TO RESTART", 0f);
    }

    private void TrySetBestWave(int wave)
    {
        if (wave <= bestWave)
        {
            return;
        }

        bestWave = wave;
        PlayerPrefs.SetInt(BestWavePrefKey, bestWave);
        PlayerPrefs.Save();
    }

    private void OnEnemyKilled(EnemyHealth _)
    {
        totalKills++;
        credits += Mathf.Max(0, creditsPerKill);
    }

    private void ApplyIntermissionRecovery()
    {
        if (playerHealth != null && !playerHealth.IsDead && intermissionHealPercent > 0f)
        {
            float healAmount = playerHealth.MaxHealth * Mathf.Clamp01(intermissionHealPercent);
            playerHealth.Heal(healAmount);
        }

        GunShootTracer[] shooters = FindObjectsOfType<GunShootTracer>(true);
        for (int i = 0; i < shooters.Length; i++)
        {
            if (shooters[i] == null)
            {
                continue;
            }

            shooters[i].RefillMagazineAndReserve(intermissionAmmoReserveTopUp);
        }

        ApplyUpgradeEffects();
    }

    private IEnumerator WaitRealtimeOrDefeat(float seconds)
    {
        phaseTimeRemaining = Mathf.Max(0f, seconds);
        while (phaseTimeRemaining > 0f)
        {
            if (IsPlayerDefeated())
            {
                yield break;
            }

            phaseTimeRemaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        phaseTimeRemaining = 0f;
    }

    private void SetState(RunState newState, string label, float remainingTime)
    {
        runState = newState;
        currentPhaseLabel = BuildStateLabel(newState, label);
        phaseTimeRemaining = Mathf.Max(0f, remainingTime);
        StateChanged?.Invoke(runState, currentWave, currentPhaseLabel);
    }

    private string BuildStateLabel(RunState state, string baseLabel)
    {
        if (state != RunState.Intermission)
        {
            return baseLabel;
        }

        return baseLabel +
            " - [1]HP " + HealthUpgradePrice +
            " [2]DMG " + DamageUpgradePrice +
            " [3]SPD " + SpeedUpgradePrice;
    }

    private void HandleIntermissionUpgradeInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            TryBuyUpgrade(UpgradeType.Health);
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            TryBuyUpgrade(UpgradeType.Damage);
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            TryBuyUpgrade(UpgradeType.Speed);
        }
    }

    private bool TryBuyUpgrade(UpgradeType type)
    {
        if (runState != RunState.Intermission)
        {
            return false;
        }

        int cost = GetUpgradePrice(type);
        if (credits < cost)
        {
            return false;
        }

        credits -= cost;

        switch (type)
        {
            case UpgradeType.Health:
                healthUpgradeLevel++;
                if (playerHealth != null)
                {
                    playerHealth.IncreaseMaxHealth(healthUpgradeAmount, true);
                }
                break;
            case UpgradeType.Damage:
                damageUpgradeLevel++;
                break;
            case UpgradeType.Speed:
                speedUpgradeLevel++;
                break;
        }

        ApplyUpgradeEffects();
        currentPhaseLabel = BuildStateLabel(RunState.Intermission, "INTERMISSION");
        return true;
    }

    private int GetUpgradePrice(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.Health:
                return HealthUpgradePrice;
            case UpgradeType.Damage:
                return DamageUpgradePrice;
            case UpgradeType.Speed:
                return SpeedUpgradePrice;
            default:
                return 999999;
        }
    }

    private int ComputeUpgradeCost(int baseCost, int currentLevel)
    {
        int safeBase = Mathf.Max(1, baseCost);
        int level = Mathf.Max(0, currentLevel);
        return safeBase + (safeBase / 2) * level;
    }

    private void ApplyUpgradeEffects()
    {
        float damageMultiplier = 1f + Mathf.Max(0, damageUpgradeLevel) * Mathf.Max(0f, damageUpgradeStep);
        GunShootTracer[] shooters = FindObjectsOfType<GunShootTracer>(true);
        for (int i = 0; i < shooters.Length; i++)
        {
            if (shooters[i] != null)
            {
                shooters[i].SetDamageMultiplier(damageMultiplier);
            }
        }

        if (cachedPlayerMovement == null && player != null)
        {
            cachedPlayerMovement = player.GetComponent<FPSMovement>();
        }

        if (cachedPlayerMovement != null)
        {
            cachedPlayerMovement.speedMultiplier = 1f + Mathf.Max(0, speedUpgradeLevel) * Mathf.Max(0f, speedUpgradeStep);
        }
    }

    private enum UpgradeType
    {
        Health,
        Damage,
        Speed
    }

    private WaveProfile BuildProfile(int wave)
    {
        WaveProfile profile = new WaveProfile
        {
            label = "STANDARD",
            enemyCountMultiplier = 1f,
            spawnIntervalMultiplier = 1f,
            intermissionMultiplier = 1f,
            healthMultiplier = 1f,
            speedMultiplier = 1f,
            damageMultiplier = 1f
        };

        bool isElite = eliteWaveEvery > 0 && wave % eliteWaveEvery == 0;
        bool isSpike = spikeWaveEvery > 0 && wave % spikeWaveEvery == 0;
        bool isQuiet = quietWaveEvery > 0 && wave % quietWaveEvery == 0;

        if (isElite)
        {
            profile.label = "ELITE ROUND";
            profile.enemyCountMultiplier *= eliteEnemyMultiplier;
            profile.healthMultiplier *= eliteHealthMultiplier;
            profile.damageMultiplier *= eliteDamageMultiplier;
            profile.speedMultiplier *= eliteSpeedMultiplier;
            profile.intermissionMultiplier *= 1.2f;
        }
        else if (isSpike)
        {
            profile.label = "SPIKE";
            profile.enemyCountMultiplier *= spikeEnemyMultiplier;
            profile.spawnIntervalMultiplier *= spikeSpawnIntervalMultiplier;
        }
        else if (isQuiet)
        {
            profile.label = "QUIET WINDOW";
            profile.spawnIntervalMultiplier *= quietSpawnIntervalMultiplier;
            profile.intermissionMultiplier *= quietIntermissionMultiplier;
            profile.enemyCountMultiplier *= 0.9f;
        }

        return profile;
    }

    private struct WaveProfile
    {
        public string label;
        public float enemyCountMultiplier;
        public float spawnIntervalMultiplier;
        public float intermissionMultiplier;
        public float healthMultiplier;
        public float speedMultiplier;
        public float damageMultiplier;
    }
}
