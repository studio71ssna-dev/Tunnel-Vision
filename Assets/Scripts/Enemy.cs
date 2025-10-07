using UnityEngine;
using UnityEngine.Events;

public class Enemy : MonoBehaviour, IEnemy
{
    [Tooltip("The ScriptableObject that defines this enemy's properties.")]
    [SerializeField] private EnemyData enemyData;
    [Tooltip("The tag used by the EnemyPooler for this enemy type.")]
    public string poolTag;

    private float currentHealth;

    public UnityEvent<float> OnHealthChanged;// Sends health as a percentage (0 to 1)
    public UnityEvent OnDeath;
    private void OnEnable()
    {
        ResetEnemyState();
    }

    public void ResetEnemyState()
    {
        if (enemyData != null)
        {
            currentHealth = enemyData.maxHealth;
            OnHealthChanged?.Invoke(1f);
        }
        else
        {
            Debug.LogError("EnemyData is not assigned on " + gameObject.name);
        }
    }

    /// <param name="baseDamage">The base damage of the attack.</param>
    /// <param name="damageType">The elemental type of the attack.</param>
    public void TakeDamage(float baseDamage, ElementType damageType)
    {
        if (currentHealth <= 0) return;

        // Get the damage multiplier from EnemyData
        float multiplier = enemyData.GetDamageMultiplier(damageType);
        float finalDamage = baseDamage * multiplier;

        currentHealth -= finalDamage;

        // Invoke event to update UI (e.g., a health bar)
        OnHealthChanged?.Invoke(currentHealth / enemyData.maxHealth);

        // Play hit sound effect
        if (enemyData.hitSound != null)
        {
            AudioSource.PlayClipAtPoint(enemyData.hitSound, transform.position);
        }

        Debug.Log($"{enemyData.enemyName} took {finalDamage} ({damageType}) damage. Health: {currentHealth}/{enemyData.maxHealth}");

        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{enemyData.enemyName} has been defeated.");
        OnDeath?.Invoke();

        // Play death sound effect
        if (enemyData.deathSound != null)
        {
            AudioSource.PlayClipAtPoint(enemyData.deathSound, transform.position);
        }
        EnemyPooler.Instance.ReturnToPool(poolTag, gameObject);
    }
}
