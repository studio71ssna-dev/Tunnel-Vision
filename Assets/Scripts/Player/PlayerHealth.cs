using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;

    // Current health (runtime)
    private int _currentHealth;

    // Events
    // Invoked when the player takes damage: (currentHealth, maxHealth)
    public UnityEvent<int, int> OnTakeDmg;
    // Optional death event to hook UI/game over behavior
    public UnityEvent OnDeath;

    private void Awake()
    {
        _currentHealth = maxHealth;
    }

    // Public accessor
    public int CurrentHealth => _currentHealth;
    public int MaxHealth => maxHealth;

    // Lowers current health by amount, invokes OnTakeDmg, and handles death when health reaches0
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        _currentHealth -= amount;
        if (_currentHealth < 0) _currentHealth = 0;

        OnTakeDmg?.Invoke(_currentHealth, maxHealth);

        if (_currentHealth == 0)
        {
            Die();
        }
    }

    private void Die()
    {
        OnDeath?.Invoke();
        // Default behavior: disable the player GameObject. Override by subscribing to OnDeath in inspector.
        gameObject.SetActive(false);
    }
}
