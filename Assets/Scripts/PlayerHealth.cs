using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement; // Required for reloading the scene

/// <summary>
/// Manages the player's health, including taking damage and handling death.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("The maximum health of the player.")]
    [SerializeField] private float maxHealth = 100f;

    // The player's current health.
    private float currentHealth;

    [Header("Events")]
    [Tooltip("Event fired when health changes. Sends current health as a percentage (0-1).")]
    public UnityEvent<float> OnHealthChanged;
    [Tooltip("Event fired when the player's health reaches zero.")]
    public UnityEvent OnPlayerDied;

    private void Start()
    {
        // Initialize health at the start of the game.
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Reduces the player's health by a specified amount.
    /// </summary>
    /// <param name="damageAmount">The amount of damage to take.</param>
    public void TakeDamage(float damageAmount)
    {
        // Ignore damage if already dead.
        if (currentHealth <= 0) return;

        currentHealth -= damageAmount;

        // Ensure health doesn't go below zero.
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        // Fire the health changed event for UI elements like health bars.
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        Debug.Log($"Player took {damageAmount} damage. Health is now {currentHealth}/{maxHealth}");

        // Check for death.
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Handles the player's death.
    /// </summary>
    private void Die()
    {
        Debug.Log("Player has been defeated!");
        OnPlayerDied?.Invoke();

        // As a simple death mechanic, we'll reload the level after a short delay.
        // In a full game, you might show a "Game Over" screen here.
        Invoke(nameof(ReloadScene), 2f);
    }

    /// <summary>
    /// Reloads the currently active scene.
    /// </summary>
    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
