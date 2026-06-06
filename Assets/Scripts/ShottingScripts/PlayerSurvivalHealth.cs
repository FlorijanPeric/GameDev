using UnityEngine;

public class PlayerSurvivalHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public bool autoHealOnEnable = true;

    private float currentHealth;
    private bool isDead;

    public event System.Action<float, float> HealthChanged;
    public event System.Action Died;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => Mathf.Max(1f, maxHealth);
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = MaxHealth;
    }

    private void OnEnable()
    {
        if (autoHealOnEnable)
        {
            ResetHealth();
        }
    }

    public void ResetHealth()
    {
        isDead = false;
        currentHealth = MaxHealth;
        HealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    public bool TakeDamage(float amount)
    {
        if (isDead)
        {
            return false;
        }

        currentHealth -= Mathf.Max(0f, amount);
        currentHealth = Mathf.Max(0f, currentHealth);
        HealthChanged?.Invoke(currentHealth, MaxHealth);

        if (currentHealth > 0f)
        {
            return false;
        }

        isDead = true;
        Died?.Invoke();
        return true;
    }

    public void ApplyDamage(float amount)
    {
        TakeDamage(amount);
    }

    public void Heal(float amount)
    {
        if (isDead)
        {
            return;
        }

        currentHealth = Mathf.Min(MaxHealth, currentHealth + Mathf.Max(0f, amount));
        HealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    public void IncreaseMaxHealth(float amount, bool refillByIncreaseAmount)
    {
        float safeIncrease = Mathf.Max(0f, amount);
        if (safeIncrease <= 0f)
        {
            return;
        }

        maxHealth = Mathf.Max(1f, maxHealth + safeIncrease);
        if (refillByIncreaseAmount)
        {
            currentHealth = Mathf.Min(MaxHealth, currentHealth + safeIncrease);
        }

        HealthChanged?.Invoke(currentHealth, MaxHealth);
    }
}
