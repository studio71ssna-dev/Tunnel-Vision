using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    private BulletData _data;
    private Rigidbody _rb;
    private TrailRenderer _trail; // *** NEW REFERENCE ***
    private CancellationTokenSource _cts;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _trail = GetComponent<TrailRenderer>(); // Auto-find the component
    }

    public void Initialize(BulletData bulletData)
    {
        _data = bulletData;

        // 1. Physics Setup
        // Unity 6 / 2023+ syntax. Use 'velocity' for older versions.
        if (_rb != null) _rb.linearVelocity = transform.forward * _data.speed;

        // 2. Trail Setup (Visuals)
        if (_trail != null)
        {
            // Apply the color from your BulletData (e.g., Red for Fire)
            _trail.startColor = _data.elementColor;
            // Fade out the end of the trail
            _trail.endColor = new Color(_data.elementColor.r, _data.elementColor.g, _data.elementColor.b, 0f);

            // *** CRITICAL FIX FOR POOLING ***
            // Clears the old path so you don't see a line stretch from the death point to spawn point
            _trail.Clear();
            _trail.emitting = true;
        }

        if (_cts != null) _cts.Dispose();
        _cts = new CancellationTokenSource();

        ReturnToPoolAfterTime(_data.lifetime, _cts.Token).Forget();
    }

    private void OnTriggerEnter(Collider other)
    {
        // DEBUG: Helps track what we hit
        // Debug.Log($"Bullet hit: {other.name}");

        if (_data == null || ((1 << other.gameObject.layer) & _data.hitLayers) == 0) return;

        // Damage Logic
        if (other.TryGetComponent<IDamageable>(out IDamageable target))
        {
            target.TakeDamage(_data.damage, _data.elementType);
        }

        if (_data.hitSound != null) AudioSource.PlayClipAtPoint(_data.hitSound, transform.position);

        if (_data.destroyOnHit) ReturnToPool();
    }

    private void ReturnToPool()
    {
        // Stop drawing the trail immediately
        if (_trail != null) _trail.emitting = false;

        _cts?.Cancel();

        if (ObjectPooler.Instance != null)
            ObjectPooler.Instance.ReturnToPool(_data.poolTag, gameObject);
        else
            Destroy(gameObject);
    }

    private async UniTaskVoid ReturnToPoolAfterTime(float delay, CancellationToken token)
    {
        bool canceled = await UniTask.Delay(System.TimeSpan.FromSeconds(delay), cancellationToken: token).SuppressCancellationThrow();
        if (!canceled) ReturnToPool();
    }

    private void OnDisable()
    {
        if (_rb != null) _rb.linearVelocity = Vector3.zero;
        _cts?.Cancel();
    }
}