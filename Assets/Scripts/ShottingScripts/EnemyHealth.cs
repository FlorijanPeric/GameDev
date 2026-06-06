using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    public static event System.Action<EnemyHealth> EnemyKilled;

    [Header("Health")]
    public float maxHealth = 100f;
    public bool destroyOnDeath = true;

    private float currentHealth;
    private bool isDead;

    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = Mathf.Max(1f, maxHealth);
    }

    public bool TakeDamage(float amount)
    {
        if (isDead)
        {
            return false;
        }

        currentHealth -= Mathf.Max(0f, amount);

        // Debug log damage for hit registration troubleshooting
        if (Debug.isDebugBuild)
        {
            Debug.Log($"EnemyHealth: {gameObject.name} took {amount} damage, currentHealth={currentHealth}", this);
        }

        // Play hurt SFX if available
        EnemyAudioPlayer audio = GetComponent<EnemyAudioPlayer>();
        if (audio == null)
        {
            audio = GetComponentInChildren<EnemyAudioPlayer>();
        }
        if (audio != null)
        {
            audio.PlayHurt();
        }

        if (currentHealth > 0f)
        {
            return false;
        }

        Die();
        // Play death SFX
        if (audio != null)
        {
            audio.PlayDeath();
        }

        return true;
    }

    public void ConfigureMaxHealth(float newMaxHealth, bool refillCurrent)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);
        if (refillCurrent || currentHealth <= 0f)
        {
            currentHealth = maxHealth;
        }
        else
        {
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        EnemyKilled?.Invoke(this);

        // Let ZombieDeath own post-death behavior (drop, pose, cleanup timing).
        ZombieDeath deathHandler = GetComponent<ZombieDeath>();
        if (deathHandler == null)
        {
            deathHandler = GetComponentInChildren<ZombieDeath>();
        }

        if (deathHandler != null)
        {
            deathHandler.TriggerDeath();
            return;
        }

        // Fallback if no ZombieDeath component
        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
