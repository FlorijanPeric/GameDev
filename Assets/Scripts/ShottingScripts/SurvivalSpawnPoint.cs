using UnityEngine;

public class SurvivalSpawnPoint : MonoBehaviour
{
    [Header("Point Rules")]
    public float spawnWeight = 1f;
    public int minWave = 1;
    public bool allowWhenVisibleToPlayer = false;

    [Header("Jitter")]
    public float horizontalJitterRadius = 1.5f;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, horizontalJitterRadius));
    }
}
