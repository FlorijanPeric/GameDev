using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SurvivalSpawnIntensityZone : MonoBehaviour
{
    [Header("References")]
    public SurvivalEnemySpawner spawner;
    public string playerTag = "Player";

    [Header("Spawner Tuning In Zone")]
    public float waveDelayMultiplierInZone = 0.8f;
    public float spawnIntervalMultiplierInZone = 0.75f;
    public int waveSizeBonusInZone = 3;
    public int aliveCapBonusInZone = 6;

    private int playersInside;

    private void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c != null)
        {
            c.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playersInside++;
        if (spawner != null)
        {
            spawner.SetRuntimeTuning(
                waveDelayMultiplierInZone,
                spawnIntervalMultiplierInZone,
                waveSizeBonusInZone,
                aliveCapBonusInZone);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playersInside = Mathf.Max(0, playersInside - 1);
        if (playersInside == 0 && spawner != null)
        {
            spawner.ResetRuntimeTuning();
        }
    }
}
