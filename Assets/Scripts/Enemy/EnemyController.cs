using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;
using System.Threading;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(LineRenderer))]
public class EnemyController : MonoBehaviour
{
    [Header("Data Config")]
    [SerializeField] private EnemyType _enemyType;
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private ElementType lootType;


    [Header("AI Settings")]
    [SerializeField] private float _stopDistance = 8f;
    [SerializeField] private float _rotationSpeed = 5f;

    [Header("Laser Attack")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private int _attackIntervalMS = 2000; // 2 seconds
    [SerializeField] private int _laserDurationMS = 200;   // 0.2 seconds
    [SerializeField] private float _damageToPlayer = 10f;
    [SerializeField] private float _attackRange = 50f;

    [Header("UI")]
    [SerializeField] private EnemyHealthUI _healthUI; // Drag your new UI script here

    private NavMeshAgent _agent;
    private LineRenderer _lineRenderer;
    private Transform _player;
    private PlayerHealth _playerHealth;
    private float _currentHealth;
    private CancellationTokenSource _cts;
    private bool _isAttacking = false;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _lineRenderer = GetComponent<LineRenderer>();

        // FIND PLAYER AND HEALTH SCRIPT
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _playerHealth = playerObj.GetComponent<PlayerHealth>(); // Cache it here
        }

        // Ensure visuals are off at start
        _lineRenderer.enabled = false;
        _lineRenderer.useWorldSpace = true;
    }

    private void OnEnable()
    {
        _currentHealth = _maxHealth;
        _agent.isStopped = false;
        _isAttacking = false;
        _lineRenderer.enabled = false;

        // Initialize Health UI
        if (_healthUI != null)
        {
            // If your EnemyType has a color, use it. Otherwise default to red.
            // Assuming you might add color to EnemyType later, using generic Color.red for now.
            _healthUI.Initialize(_maxHealth, Color.red);
        }

        // Reset and Start Async Logic
        if (_cts != null) _cts.Dispose();
        _cts = new CancellationTokenSource();

        LifeCycleRoutine(_cts.Token).Forget();
    }

    // The Main AI Loop
    private async UniTaskVoid LifeCycleRoutine(CancellationToken token)
    {
        // Loop while alive and not cancelled
        while (!token.IsCancellationRequested && _currentHealth > 0)
        {
            if (_player == null) break;

            float dist = Vector3.Distance(transform.position, _player.position);

            if (dist > _stopDistance && !_isAttacking)
            {
                // Chase State
                _agent.isStopped = false;
                _agent.SetDestination(_player.position);
            }
            else
            {
                // Attack State
                _agent.isStopped = true;
                RotateTowardsPlayer();

                if (!_isAttacking)
                {
                    // Fire attack and wait
                    await FireRayAttack(token);

                    // Cooldown between shots
                    await UniTask.Delay(_attackIntervalMS, cancellationToken: token);
                }
            }

            // Wait for next frame
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

    private async UniTask FireRayAttack(CancellationToken token)
    {
        _isAttacking = true;

        Vector3 startPos = _firePoint.position;
        Vector3 direction = (_player.position - startPos).normalized;
        Vector3 endPos;

        // Physics Raycast
        if (Physics.Raycast(startPos, direction, out RaycastHit hit, _attackRange))
        {
            endPos = hit.point;

            if (hit.collider.CompareTag("Player"))
            {
                // *** THE FIX IS HERE ***
                if (_playerHealth != null)
                {
                    // Cast float to int because PlayerHealth uses int
                    _playerHealth.TakeDamage((int)_damageToPlayer);
                }
                Debug.Log($"Zapped Player for {_damageToPlayer} damage!");
            }
        }
        else
        {
            endPos = startPos + (direction * _attackRange);
        }

        // Visuals On
        _lineRenderer.SetPosition(0, startPos);
        _lineRenderer.SetPosition(1, endPos);
        _lineRenderer.enabled = true;

        await UniTask.Delay(_laserDurationMS, cancellationToken: token);
        if (token.IsCancellationRequested) return;

        // Visuals Off
        _lineRenderer.enabled = false;
        _isAttacking = false;
    }

    // Called by WeakPoint / IDamageable
    public void TakeDamage(float baseDamage, ElementType incomingType)
    {
        if (_currentHealth <= 0) return;

        // Calculate damage multiplier from ScriptableObject
        float multiplier = _enemyType.GetDamageMultiplier(incomingType);
        float finalDamage = baseDamage * multiplier;

        _currentHealth -= finalDamage;

        // Update UI
        if (_healthUI != null)
        {
            _healthUI.UpdateHealth(_currentHealth);
        }

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        _cts.Cancel(); // Stop AI immediately

        // Ensure visuals are off
        _lineRenderer.enabled = false;

        Vector3 spawnPos = transform.position;

        // Cast a ray from the enemy's center DOWN to find the floor
        // We start 1 unit up to ensure we don't start inside the floor
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 10f))
        {
            // Found the floor! Spawn loot 0.5 units above it so it doesn't clip
            spawnPos = hit.point + (Vector3.up * 0.5f);
        }
        else
        {
            // Fallback: If we are over a hole, just drop it to y = 0.5
            spawnPos.y = 0.5f;
        }

        // Spawn Loot
        if (ObjectPooler.Instance != null)
        {
            ObjectPooler.Instance.SpawnFromPool(lootType + "Loot", spawnPos, Quaternion.identity );
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private string GetPoolTag()
    {
        // Dynamic tag generation to match your ObjectPooler keys
        return "Enemy" + _enemyType.myType.ToString();
    }

    private void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}