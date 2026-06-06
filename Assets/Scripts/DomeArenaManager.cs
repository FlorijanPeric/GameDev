using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class DomeArenaManager : MonoBehaviour
{
    [Header("Stadium/Arena Configuration")]
    [SerializeField] private GameObject stadiumGameObject = null;
    [SerializeField] private Transform domeCenter = null;
    [SerializeField] private float domeRadius = 40f;
    [SerializeField] private float domeHeight = 35f;
    [SerializeField] private Vector3 spawnPointsCenter = Vector3.zero;
    [SerializeField] private DomeZombieSpawner domeSpawner = null;
    
    [Header("Teleport Configuration")]
    [SerializeField] private int killCountThresholdMin = 50;
    [SerializeField] private int killCountThresholdMax = 100;
    [SerializeField] private int domeEnemiesNeeded = 30;
    [SerializeField] private float enemySpawnRateInDome = 2.0f;
    [SerializeField] private float domeEnemySpawnDistance = 20f;
    
    [Header("Special Rifle")]
    [SerializeField] private Weapon specialRiflePrefab = null;
    [SerializeField] private float specialRifleDamageMultiplier = 1.5f;
    
    private int totalEnemyKills = 0;
    private int domeEnemyKills = 0;
    private bool isInDome = false;
    private Vector3 playerExitPosition;
    private Weapon previousWeapon = null;
    private Weapon equippedSpecialRifle = null;
    private SurvivalEnemySpawner spawner;
    private DomeZombieSpawner activeDomeSpawner;
    private Transform originalSpawnerPlayer = null;
    private WeaponManager weaponManager;
    private Transform playerTransform;
    private float originalSpawnIntervalMultiplier = 1f;

    private float GetArenaFloorY()
    {
        if (stadiumGameObject != null)
        {
            Transform floor = stadiumGameObject.transform.Find("DomeFloor");
            if (floor != null)
            {
                return floor.position.y;
            }
        }

        return domeCenter != null ? domeCenter.position.y - 10f : 0f;
    }
    
    private void Awake()
    {
        spawner = FindObjectOfType<SurvivalEnemySpawner>();
        weaponManager = FindObjectOfType<WeaponManager>();
        playerTransform = FindObjectOfType<PlayerInput>()?.transform;
        activeDomeSpawner = domeSpawner;
        
        // Auto-find stadium if not assigned
        if (stadiumGameObject == null)
        {
            stadiumGameObject = GameObject.Find("SM_Stadium_Dome");
            if (stadiumGameObject == null)
            {
                stadiumGameObject = GameObject.Find("SM_Stadium");
            }
        }
        
        // Auto-find dome center
        if (domeCenter == null)
        {
            Transform foundCenter = FindObjectOfType<Transform>();
            foreach (GameObject go in FindObjectsOfType<GameObject>())
            {
                if (go.name == "DomeCenter")
                {
                    domeCenter = go.transform;
                    break;
                }
            }
        }
        
        // Create dome center if still missing
        if (domeCenter == null)
        {
            GameObject domeCenterObj = new GameObject("DomeCenter");
            if (stadiumGameObject != null)
            {
                domeCenterObj.transform.position = stadiumGameObject.transform.position + Vector3.up * 15f;
            }
            else
            {
                domeCenterObj.transform.position = Vector3.zero;
            }
            domeCenter = domeCenterObj.transform;
        }
        
        // Set spawn points center to dome center
        if (stadiumGameObject != null)
        {
            spawnPointsCenter = stadiumGameObject.transform.position;
        }
    }
    
    private void Start()
    {
        if (activeDomeSpawner != null)
        {
            activeDomeSpawner.player = playerTransform;
        }

        EnemyHealth.EnemyKilled += OnAnyEnemyKilled;
    }
    
    private void OnDestroy()
    {
        if (activeDomeSpawner != null)
        {
            activeDomeSpawner.StopSpecialRound();
        }

        EnemyHealth.EnemyKilled -= OnAnyEnemyKilled;
    }

    private void OnAnyEnemyKilled(EnemyHealth enemyHealth)
    {
        if (enemyHealth == null)
        {
            return;
        }

        if (isInDome)
        {
            domeEnemyKills++;
            Debug.Log($"Dome Arena: {domeEnemyKills}/{domeEnemiesNeeded} enemies killed");

            if (domeEnemyKills >= domeEnemiesNeeded)
            {
                ExitDome();
            }
            return;
        }

        totalEnemyKills++;
        Debug.Log($"Total kills: {totalEnemyKills}");

        if (totalEnemyKills >= killCountThresholdMin)
        {
            int randomThreshold = Random.Range(killCountThresholdMin, killCountThresholdMax + 1);
            if (totalEnemyKills == randomThreshold)
            {
                EnterDome();
            }
        }
    }
    
    private void EnterDome()
    {
        if (isInDome || playerTransform == null) return;
        if (activeDomeSpawner == null)
        {
            activeDomeSpawner = domeSpawner;
        }
        
        isInDome = true;
        domeEnemyKills = 0;
        
        // Save player position and current weapon
        playerExitPosition = playerTransform.position;
        if (weaponManager != null)
        {
            previousWeapon = weaponManager.CurrentWeapon;
        }
        
        // Teleport player to dome
        playerTransform.position = new Vector3(domeCenter.position.x, GetArenaFloorY() + 1.5f, domeCenter.position.z);
        
        // Give special rifle
        GiveSpecialRifle();
        
        // Increase spawn rate temporarily and center spawns inside stadium
        if (activeDomeSpawner != null)
        {
            activeDomeSpawner.player = playerTransform;
            activeDomeSpawner.BeginSpecialRound(9999f, 0f);
        }
        
        Debug.Log("Player entered the Dome Arena! Kill 30 enemies to escape!");
    }
    
    private void ExitDome()
    {
        if (!isInDome) return;
        
        isInDome = false;
        
        // Restore spawn rate and spawn configuration
        if (activeDomeSpawner != null)
        {
            activeDomeSpawner.StopSpecialRound();
        }
        // Teleport back to exit position
        if (playerTransform != null)
        {
            playerTransform.position = playerExitPosition;
        }
        
        // Restore previous weapon
        if (weaponManager != null && previousWeapon != null)
        {
            weaponManager.Equip(previousWeapon);
        }
        
        // Destroy special rifle if it was equipped
        if (equippedSpecialRifle != null)
        {
            if (equippedSpecialRifle.weaponModel != null)
            {
                Destroy(equippedSpecialRifle.weaponModel);
            }
            Destroy(equippedSpecialRifle.gameObject);
        }
        
        // Reset kill counter for next dome entry
        totalEnemyKills = 0;
        
        Debug.Log("You escaped the Dome Arena!");
    }
    
    private void GiveSpecialRifle()
    {
        if (weaponManager == null) return;
        
        if (specialRiflePrefab == null)
        {
            Debug.LogWarning("DomeArenaManager: Special Rifle prefab not assigned!");
            return;
        }
        
        // Instantiate special rifle
        equippedSpecialRifle = Instantiate(specialRiflePrefab, weaponManager.weaponHolder);
        equippedSpecialRifle.name = "SpecialRifle_Dome";
        
        // Boost damage if possible
        GunShootTracer gunTracer = equippedSpecialRifle.GetComponent<GunShootTracer>();
        if (gunTracer == null)
        {
            gunTracer = equippedSpecialRifle.GetComponentInChildren<GunShootTracer>();
        }
        
        if (gunTracer != null)
        {
            // GunShootTracer uses `damage` and `damageMultiplier` fields
            gunTracer.damage *= specialRifleDamageMultiplier;
            Debug.Log($"Special Rifle damage boosted to {gunTracer.damage}");
        }
        
        // Equip the special rifle
        weaponManager.Equip(equippedSpecialRifle);
        
        Debug.Log("Special Rifle equipped!");
    }
    
    public bool IsInDome() => isInDome;
    public int GetDomeEnemiesKilled() => domeEnemyKills;
    public int GetDomeEnemiesNeeded() => domeEnemiesNeeded;
    public int GetTotalEnemyKills() => totalEnemyKills;
}
