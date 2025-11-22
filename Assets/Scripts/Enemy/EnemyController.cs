using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;
using System.Threading;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(LineRenderer))]
public class EnemyController : MonoBehaviour
{
    [Header("Data Config")]
    [SerializeField] private EnemyType _enemyType; // Drag 'PoisonData' here
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private string _lootTag = "LootOrb";

    [Header("AI Settings")]
    [SerializeField] private float _stopDistance = 8f;
    [SerializeField] private float _rotationSpeed = 5f;

    [Header("Laser Attack")]
    [SerializeField] private Transform _firePoint; // Center of the symbol
    [SerializeField] private int _attackIntervalMS = 2000; // 2 seconds
    [SerializeField] private int _laserDurationMS = 200;   // 0.2 seconds
    [SerializeField] private int _damageToPlayer = 10;

    private NavMeshAgent _agent;
    private LineRenderer _lineRenderer;
    private Transform _player;
    private float _currentHealth;
    private CancellationTokenSource _cts; // To cancel tasks on death
    private bool _isAttacking = false;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _lineRenderer = GetComponent<LineRenderer>();
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        _lineRenderer.enabled = false;
    }

    private void OnEnable()
    {
        _currentHealth = _maxHealth;
        _agent.isStopped = false;

        // Start the Logic Loop
        if (_cts != null) _cts.Dispose();
        _cts = new CancellationTokenSource();

        LifeCycleRoutine(_cts.Token).Forget();
    }

    // The Main AI Loop (replaces Update)
    private async UniTaskVoid LifeCycleRoutine(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _currentHealth > 0)
        {
            if (_player == null) break;

            float dist = Vector3.Distance(transform.position, _player.position);

            if (dist > _stopDistance && !_isAttacking)
            {
                // Move
                _agent.isStopped = false;
                _agent.SetDestination(_player.position);
            }
            else
            {
                // Stop and Attack
                _agent.isStopped = true;
                RotateTowardsPlayer();

                if (!_isAttacking)
                {
                    await FireRayAttack(token);
                    // Wait interval before next check
                    await UniTask.Delay(_attackIntervalMS, cancellationToken: token);
                }
            }

            // Yield to next frame to prevent freezing
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }

    private void RotateTowardsPlayer()
    {
        if (_player == null) return;
        Vector3 dir = (_player.position - transform.position).normalized;
        dir.y = 0; // Keep upright
        if (dir != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * _rotationSpeed);
        }
    }

    // REPLACES COROUTINE
    private async UniTask FireRayAttack(CancellationToken token)
    {
        _isAttacking = true;

        // 1. Visuals On
        _lineRenderer.SetPosition(0, _firePoint.position);
        _lineRenderer.SetPosition(1, _player.position); // Simplification: Aim perfectly at player center
        _lineRenderer.enabled = true;

        // 2. Apply Damage (Hitscan logic)
        // Simple distance check or Raycast
        if (Physics.Raycast(_firePoint.position, (_player.position - _firePoint.position).normalized, out RaycastHit hit, 100f))
        {
            if (hit.collider.CompareTag("Player"))
            {
                _player.GetComponent<PlayerHealth>()?.TakeDamage(_damageToPlayer);
            }
        }

        // 3. Wait for duration
        await UniTask.Delay(_laserDurationMS, cancellationToken: token);

        // 4. Visuals Off
        _lineRenderer.enabled = false;
        _isAttacking = false;
    }

    // LOGIC: Called by WeakPoint
    public void TakeDamage(float baseDamage, ElementType incomingType)
    {
        if (_currentHealth <= 0) return;

        // Calculate using SO
        float multiplier = _enemyType.GetDamageMultiplier(incomingType);
        float finalDamage = baseDamage * multiplier;

        _currentHealth -= finalDamage;

        // Feedback (Optional)
        // PopupTextPooler.Spawn(finalDamage, transform.position, Color.white);

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        _cts.Cancel(); // Stop AI loop immediately

        ObjectPooler.Instance.SpawnFromPool(_lootTag, transform.position, Quaternion.identity);
        ObjectPooler.Instance.ReturnToPool(GetPoolTag(), gameObject);
    }

    private string GetPoolTag()
    {
        // Helper to return correct tag based on type
        // Ensure these match your ObjectPooler tags
        return "Enemy" + _enemyType.myType.ToString();
    }

    private void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}