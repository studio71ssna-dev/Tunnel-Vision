using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>
/// A versatile enemy script that can behave as a melee or ranged attacker
/// based on the provided EnemyData. Implements IEnemy to be damageable.
/// </summary>
public class BehavioralEnemy : MonoBehaviour
{
    [Tooltip("The ScriptableObject that defines this enemy's properties.")]
    [SerializeField] private EnemyData enemyData;
    [Tooltip("The tag used by the EnemyPooler for this enemy type.")]
    public string poolTag;

    private float currentHealth;
    private Transform playerTarget;
    private float lastShotTime;
    private PlayerHealth player;
    private CancellationTokenSource meleeDamageTokenSource;
    [SerializeField] private Transform enmy_gp;

    // UnityEvents to trigger UI updates or other game logic
    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDeath;

    private void Awake()
    {
        // Find the player in the scene. This assumes the player has the "Player" tag.
        // For better performance, this could be passed in by a manager class.
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }
    }

    private void OnEnable()
    {
        ResetEnemyState();
    }

    private void Update()
    {
        if (enemyData == null || playerTarget == null || currentHealth <= 0)
        {
            return; // Do nothing if not properly configured or is dead
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // Only activate behavior if player is within detection range
        if (distanceToPlayer <= enemyData.detectionRange)
        {
            // Use a switch to handle different behaviors
            switch (enemyData.attackType)
            {
                case AttackType.Melee:
                    HandleMeleeBehavior(distanceToPlayer);
                    break;
                case AttackType.Ranged:
                    HandleRangedBehavior();
                    break;
            }
        }
    }

    private void HandleMeleeBehavior(float distanceToPlayer)
    {
        // Always look at the player
        transform.LookAt(playerTarget);

        // Move towards the player if not within stopping distance
        if (distanceToPlayer > enemyData.stoppingDistance)
        {
            transform.position = Vector3.MoveTowards(transform.position, playerTarget.position, enemyData.moveSpeed * Time.deltaTime);
        }
    }

    private void HandleRangedBehavior()
    {
        // Always look at the player
        transform.LookAt(playerTarget);

        // Check if it's time to fire again
        if (Time.time > lastShotTime + enemyData.fireRate)
        {
            ShootProjectile();
            lastShotTime = Time.time;
        }
    }

    private void ShootProjectile()
    {
        if (enemyData.projectileData == null)
        {
            Debug.LogError($"Ranged enemy '{enemyData.name}' is missing projectile data.");
            return;
        }

        // Get a bullet from the pool
        GameObject bulletObject = BulletPooler.Instance.GetFromPool(enemyData.projectileData.poolTag);
        if (bulletObject != null)
        {
            bulletObject.transform.position = enmy_gp.transform.position;
            bulletObject.transform.rotation = enmy_gp.transform.rotation; // Use the enemy's rotation

            // Initialize the bullet (make sure its hitLayers are set correctly in the BulletData SO)
            bulletObject.GetComponent<Bullet>().Initialize(enemyData.projectileData);
        }
    }

    private async UniTaskVoid DealDamageOverTimeLoopAsync(PlayerHealth playerHealth, CancellationToken token)
    {
        Debug.Log($"{enemyData.name} started melee damage loop.");

        // This loop will continue until the CancellationToken is cancelled
        while (!token.IsCancellationRequested)
        {
            // Deal damage to the player
            playerHealth.TakeDamage(enemyData.contactDamage);
            Debug.Log($"{enemyData.name} dealt {enemyData.contactDamage} melee damage.");

            // Wait for the specified interval, but stop waiting if the token is cancelled
            await UniTask.Delay(System.TimeSpan.FromSeconds(enemyData.meleeDamageInterval), ignoreTimeScale: false, cancellationToken: token);
        }

        Debug.Log($"{enemyData.name} stopped melee damage loop.");
    }
    // This function handles dealing damage on contact for melee enemies
    // This function handles dealing damage on contact for melee enemies
    private void OnTriggerEnter(Collider other)
    {
        // Only applies to Melee enemies and only if they are alive
        if (enemyData.attackType != AttackType.Melee || currentHealth <= 0) return;

        if (other.CompareTag("Player"))
        {
            // Cancel any existing loop before starting a new one (safety check)
            meleeDamageTokenSource?.Cancel();
            meleeDamageTokenSource = new CancellationTokenSource();

            // Start the damage loop, passing the player's health component and the cancellation token
            DealDamageOverTimeLoopAsync(other.GetComponent<PlayerHealth>(), meleeDamageTokenSource.Token).Forget();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // If the player leaves the trigger, cancel the damage loop
        if (other.CompareTag("Player"))
        {
            meleeDamageTokenSource?.Cancel();
        }
    }

    private void OnDisable()
    {
        // ... existing code in OnDisable if any ...

        // Ensure the damage loop is stopped if the enemy is disabled for any reason
        meleeDamageTokenSource?.Cancel();
        ResetEnemyState(); // Assuming you want to keep this from the original code
    }

    public void ResetEnemyState()
    {
        if (enemyData != null)
        {
            currentHealth = enemyData.maxHealth;
            lastShotTime = Time.time; // Reset shot timer
            OnHealthChanged?.Invoke(1f);
        }
    }

    // --- NEW GIZMOS METHOD ---
    /// <summary>
    /// Draws visual aids in the editor to show the enemy's ranges.
    /// This is only called when the GameObject is selected.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Ensure we have data to draw with to avoid errors.
        if (enemyData == null)
        {
            return;
        }

        // Draw the detection range (applies to all enemy types)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, enemyData.detectionRange);

        // Draw ranges specific to the attack type
        switch (enemyData.attackType)
        {
            case AttackType.Melee:
                // Draw the stopping distance for melee enemies
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, enemyData.stoppingDistance);
                break;

            case AttackType.Ranged:
                // Ranged enemies don't have a second range to visualize,
                // but this is where you could add one for a minimum fire distance, for example.
                break;
        }
    }
}
