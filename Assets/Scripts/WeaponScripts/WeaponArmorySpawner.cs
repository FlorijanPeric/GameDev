using System.Collections.Generic;
using UnityEngine;

public class WeaponArmorySpawner : MonoBehaviour
{
    [Header("Armory Setup")]
    public GameObject[] weaponPrefabs;
    public Transform[] spawnPoints;
    public bool spawnOnStart = true;
    public bool clearPreviouslySpawned = true;

    [Header("Placement")]
    public bool snapToGround = true;
    public float groundProbeHeight = 4f;
    public float groundProbeDistance = 15f;
    public LayerMask groundMask = ~0;
    public bool randomizeYaw = true;

    private readonly List<GameObject> spawnedWeapons = new List<GameObject>();

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnArmory();
        }
    }

    [ContextMenu("Spawn Armory")]
    public void SpawnArmory()
    {
        if (weaponPrefabs == null || weaponPrefabs.Length == 0)
        {
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return;
        }

        if (clearPreviouslySpawned)
        {
            ClearSpawnedWeapons();
        }

        int count = Mathf.Min(weaponPrefabs.Length, spawnPoints.Length);
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = weaponPrefabs[i];
            Transform point = spawnPoints[i];
            if (prefab == null || point == null)
            {
                continue;
            }

            Vector3 spawnPos = point.position;
            if (snapToGround)
            {
                spawnPos = ResolveGroundPosition(spawnPos);
            }

            Quaternion spawnRot = point.rotation;
            if (randomizeYaw)
            {
                Vector3 euler = spawnRot.eulerAngles;
                euler.y = Random.Range(0f, 360f);
                spawnRot = Quaternion.Euler(euler);
            }

            GameObject spawned = Instantiate(prefab, spawnPos, spawnRot);
            EnsurePickupComponents(spawned);
            spawnedWeapons.Add(spawned);
        }

    }

    [ContextMenu("Clear Spawned Armory")]
    public void ClearSpawnedWeapons()
    {
        for (int i = spawnedWeapons.Count - 1; i >= 0; i--)
        {
            if (spawnedWeapons[i] != null)
            {
                Destroy(spawnedWeapons[i]);
            }
        }

        spawnedWeapons.Clear();
    }

    private void EnsurePickupComponents(GameObject weaponObject)
    {
        if (weaponObject == null)
        {
            return;
        }

        Weapon weapon = weaponObject.GetComponent<Weapon>();
        if (weapon == null)
        {
            weapon = weaponObject.GetComponentInChildren<Weapon>();
        }

        WeaponPickup pickup = weaponObject.GetComponent<WeaponPickup>();
        if (pickup == null)
        {
            pickup = weaponObject.AddComponent<WeaponPickup>();
        }

        if (weapon != null)
        {
            pickup.weapon = weapon;
            weapon.isEquipped = false;
            weapon.isPickup = true;
            if (weapon.weaponModel != null)
            {
                weapon.weaponModel.SetActive(true);
            }
        }

        Rigidbody rb = weaponObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = weaponObject.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        
    }

    private Vector3 ResolveGroundPosition(Vector3 basePosition)
    {
        Vector3 rayStart = basePosition + Vector3.up * Mathf.Max(1f, groundProbeHeight);
        RaycastHit hit;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, Mathf.Max(groundProbeDistance, groundProbeHeight) + 1f, groundMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }

        return basePosition;
    }
}
