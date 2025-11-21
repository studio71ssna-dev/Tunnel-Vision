using Cysharp.Threading.Tasks; // Important: Add this namespace
using System.Threading; // For CancellationToken
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private BulletData data;
    private Rigidbody rb;
    // UniTask doesn't use Coroutine objects directly, but we might keep a CancellationTokenSource
    // to manage the lifecycle of our async tasks if we need to explicitly cancel them.
    private CancellationTokenSource lifetimeCancellationTokenSource;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Called when the bullet is retrieved from the pool.
    /// Sets its properties and starts its life cycle.
    /// </summary>
    public void Initialize(BulletData bulletData)
    {
        data = bulletData;

        if (rb != null)
        {
            rb.linearVelocity = transform.forward * data.speed;
        }

        // Create a new CancellationTokenSource for this bullet's lifetime.
        // This allows us to cancel the task if the bullet is returned to the pool early.
        lifetimeCancellationTokenSource = new CancellationTokenSource();

        // Start the UniTask to automatically return the bullet to the pool after its lifetime.
        // .Forget() is used when you don't need to await the task in the calling method.
        // However, use .Forget() with caution. If there's an unhandled exception, it will be
        // swallowed. For tasks that might throw, it's better to await them or handle exceptions.
        // For simple timed returns, it's generally fine.
        ReturnToPoolAfterTime(data.lifetime, lifetimeCancellationTokenSource.Token).Forget();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the bullet hit something on its designated layer
        if (data == null || ((1 << other.gameObject.layer) & data.hitLayers) == 0)
        {
            return;
        }

        // Try to apply damage if it's an enemy
        if (other.TryGetComponent<IEnemy>(out IEnemy enemy))
        {
            enemy.TakeDamage(data.damage, data.elementType);
        }

        // Using a local variable for PlayerHealth, no need for a field
        if (other.TryGetComponent<PlayerHealth>(out PlayerHealth playerComponent))
        {
            // If the bullet hits the player, apply damage
            playerComponent.TakeDamage(data.damage);
        }

        // Play hit sound
        if (data.hitSound != null)
        {
            AudioSource.PlayClipAtPoint(data.hitSound, transform.position);
        }

        // If configured, return the bullet to the pool on impact.
        if (data.destroyOnHit)
        {
            ReturnToPool();
        }
    }

    /// <summary>
    /// Returns the bullet to the object pool.
    /// </summary>
    private void ReturnToPool()
    {
        // Cancel any pending lifetime tasks associated with this bullet.
        // This is crucial to prevent the UniTask from trying to act on a disabled/pooled object.
        if (lifetimeCancellationTokenSource != null)
        {
            lifetimeCancellationTokenSource.Cancel();
            lifetimeCancellationTokenSource.Dispose(); // Dispose the CancellationTokenSource
            lifetimeCancellationTokenSource = null; // Clear the reference
        }

        // Use the tag from the BulletData to return to the correct pool.
        BulletPooler.Instance.ReturnToPool(data.poolTag, gameObject);
    }

    /// <summary>
    /// UniTask to return the bullet to the pool after a set time.
    /// </summary>
    private async UniTaskVoid ReturnToPoolAfterTime(float delay, CancellationToken cancellationToken)
    {
        try
        {
            // UniTask.Delay supports cancellation tokens
            await UniTask.Delay(System.TimeSpan.FromSeconds(delay), ignoreTimeScale: false, cancellationToken: cancellationToken);

            // If the task was cancelled, this line won't be reached, or
            // a OperationCanceledException will be thrown and caught.
            if (!cancellationToken.IsCancellationRequested)
            {
                ReturnToPool();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"An error occurred in ReturnToPoolAfterTime: {ex.Message}");
        }
    }

    /// <summary>
    /// When the bullet is disabled (returned to the pool), reset its state.
    /// </summary>
    private void OnDisable()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Ensure any active UniTask is cancelled when the object is disabled.
        // This prevents the UniTask from trying to access components of a disabled object.
        if (lifetimeCancellationTokenSource != null)
        {
            lifetimeCancellationTokenSource.Cancel();
            lifetimeCancellationTokenSource.Dispose();
            lifetimeCancellationTokenSource = null;
        }
    }
}