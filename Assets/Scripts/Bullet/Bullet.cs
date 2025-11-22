using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private BulletData _data;
    private Rigidbody _rb;
    private CancellationTokenSource _cts;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void Initialize(BulletData bulletData)
    {
        _data = bulletData;

        // Unity 6 / 2023+ syntax. Use 'velocity' for older versions.
        if (_rb != null) _rb.linearVelocity = transform.forward * _data.speed;

        if (_cts != null) _cts.Dispose();
        _cts = new CancellationTokenSource();

        ReturnToPoolAfterTime(_data.lifetime, _cts.Token).Forget();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Layer Check (Existing logic)
        if (_data == null || ((1 << other.gameObject.layer) & _data.hitLayers) == 0) return;

        // 2. APPLY DAMAGE (This was missing)
        // We look for the IDamageable interface on the object we hit
        if (other.TryGetComponent<IDamageable>(out IDamageable target))
        {
            target.TakeDamage(_data.damage, _data.elementType);
        }

        // 3. Visuals & Audio (Existing logic)
        if (_data.hitSound != null) AudioSource.PlayClipAtPoint(_data.hitSound, transform.position);

        // 4. Destroy (Existing logic)
        if (_data.destroyOnHit) ReturnToPool();
    }

    private void ReturnToPool()
    {
        _cts?.Cancel();

        // *** UPDATED LINE ***
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