// EnemyVariants.cs  (v2)
// BruteEnemy, SniperEnemy, SwarmEnemy.
// All CreateStates() now return the correct variant-specific idle state.

using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;
using System.Threading;

// ══════════════════════════════════════════════════════════════
//  BRUTE
// ══════════════════════════════════════════════════════════════
public class BruteEnemy : EnemyBase
{
    [Header("Brute")]
    [SerializeField] private float _chargeSpeedMultiplier = 2f;
    [SerializeField] private float _chargeRange = 12f;

    public float ChargeRange => _chargeRange;

    protected override IEnemyState CreateStates(EnemyStateMachine fsm)
    {
        return new BruteIdleState(this);   // ← was wrongly new IdleState()
    }

    public void SetCharging(bool charging)
    {
        Agent.speed = charging ? Data.moveSpeed * _chargeSpeedMultiplier : Data.moveSpeed;
    }

    public void TriggerDeathCleanup() => OnDeathCleanup();
}

public class BruteIdleState : IdleState
{
    private readonly BruteEnemy _brute;
    public BruteIdleState(BruteEnemy b) { _brute = b; }

    public override void Execute(EnemyBase enemy)
    {
        if (enemy.PlayerTarget != null && enemy.PlayerInRange(enemy.Data.detectionRange))
            enemy.FSM.ChangeState(new BruteChaseState(_brute));
    }
}

public class BruteChaseState : ChaseState
{
    private readonly BruteEnemy _brute;
    public BruteChaseState(BruteEnemy b) { _brute = b; }

    public override void Execute(EnemyBase enemy)
    {
        if (enemy.PlayerLost(enemy.Data.lostRange))
        {
            _brute.SetCharging(false);
            enemy.FSM.ChangeState(new BruteIdleState(_brute));
            return;
        }
        if (enemy.PlayerInRange(enemy.Data.attackRange))
        {
            _brute.SetCharging(false);
            enemy.FSM.ChangeState(new AttackState());
            return;
        }

        bool inChargeRange = enemy.PlayerInRange(_brute.ChargeRange);
        _brute.SetCharging(inChargeRange);

        enemy.Agent.SetDestination(enemy.PlayerTarget.position);
        // base rotation handled inline
        Vector3 dir = enemy.PlayerTarget.position - enemy.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            enemy.transform.rotation = Quaternion.Slerp(
                enemy.transform.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * enemy.Data.rotationSpeed);
    }

    public override void Exit(EnemyBase enemy)
    {
        _brute.SetCharging(false);
        base.Exit(enemy);
    }
}

// ══════════════════════════════════════════════════════════════
//  SNIPER
// ══════════════════════════════════════════════════════════════
[RequireComponent(typeof(LineRenderer))]
public class SniperEnemy : EnemyBase
{
    [Header("Sniper")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private int _laserDurationMs = 200;

    private LineRenderer _line;
    private CancellationTokenSource _cts;

    public Transform FirePoint => _firePoint;
    public int LaserDurationMs => _laserDurationMs;

    protected override void Awake()
    {
        base.Awake();
        _line = GetComponent<LineRenderer>();
        _line.enabled = false;
        _line.useWorldSpace = true;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _cts = new CancellationTokenSource();
    }

    protected override void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    protected override IEnemyState CreateStates(EnemyStateMachine fsm)
    {
        return new SniperIdleState(this);   // ← correct variant idle
    }

    public async UniTaskVoid FireLaser(CancellationToken token)
    {
        if (_firePoint == null || PlayerTarget == null) return;

        Vector3 start = _firePoint.position;
        Vector3 dir = (PlayerTarget.position - start).normalized;
        Vector3 end;

        if (Physics.Raycast(start, dir, out RaycastHit hit, Data.attackRange * 2f))
        {
            end = hit.point;
            if (hit.collider.CompareTag("Player") &&
                hit.collider.TryGetComponent(out PlayerHealth ph))
                ph.TakeDamage((int)Data.attackDamage);
        }
        else
        {
            end = start + dir * Data.attackRange * 2f;
        }

        _line.SetPosition(0, start);
        _line.SetPosition(1, end);
        _line.enabled = true;

        await UniTask.Delay(_laserDurationMs, cancellationToken: token).SuppressCancellationThrow();
        if (!token.IsCancellationRequested) _line.enabled = false;
    }

    public CancellationToken GetToken() => _cts?.Token ?? CancellationToken.None;
}

public class SniperIdleState : IdleState
{
    private readonly SniperEnemy _sniper;
    public SniperIdleState(SniperEnemy s) { _sniper = s; }

    public override void Execute(EnemyBase enemy)
    {
        if (enemy.PlayerTarget != null && enemy.PlayerInRange(enemy.Data.detectionRange))
            enemy.FSM.ChangeState(new SniperChaseState(_sniper));
    }
}

public class SniperChaseState : ChaseState
{
    private readonly SniperEnemy _sniper;
    public SniperChaseState(SniperEnemy s) { _sniper = s; }

    public override void Execute(EnemyBase enemy)
    {
        if (enemy.PlayerLost(enemy.Data.lostRange))
        {
            enemy.FSM.ChangeState(new SniperIdleState(_sniper));
            return;
        }
        if (enemy.PlayerInRange(enemy.Data.attackRange))
        {
            enemy.FSM.ChangeState(new SniperAttackState(_sniper));
            return;
        }
        base.Execute(enemy);
    }
}

public class SniperAttackState : AttackState
{
    private readonly SniperEnemy _sniper;
    private bool _isFiring;
    public SniperAttackState(SniperEnemy s) { _sniper = s; }

    protected override void PerformAttack(EnemyBase enemy)
    {
        if (_isFiring) return;
        _isFiring = true;
        _sniper.FireLaser(_sniper.GetToken()).Forget();
        ResetAfterCooldown(enemy).Forget();
    }

    private async UniTaskVoid ResetAfterCooldown(EnemyBase enemy)
    {
        await UniTask.Delay((int)(enemy.Data.attackCooldown * 1000),
            cancellationToken: _sniper.GetToken()).SuppressCancellationThrow();
        _isFiring = false;
    }

    public override void Exit(EnemyBase enemy)
    {
        _isFiring = false;
        base.Exit(enemy);
    }
}

// ══════════════════════════════════════════════════════════════
//  SWARM
// ══════════════════════════════════════════════════════════════
public class SwarmEnemy : EnemyBase
{
    [Header("Swarm")]
    [SerializeField] private float _weaveRadius = 1.5f;
    [SerializeField] private float _weaveFrequency = 2f;

    protected override IEnemyState CreateStates(EnemyStateMachine fsm)
    {
        return new SwarmIdleState(this);   // ← correct variant idle
    }

    public Vector3 GetWeaveOffset()
    {
        float t = Time.time * _weaveFrequency;
        return new Vector3(Mathf.Sin(t) * _weaveRadius, 0f, Mathf.Cos(t * 0.7f) * _weaveRadius * 0.5f);
    }
}

public class SwarmIdleState : IdleState
{
    private readonly SwarmEnemy _swarm;
    public SwarmIdleState(SwarmEnemy s) { _swarm = s; }

    public override void Execute(EnemyBase enemy)
    {
        if (enemy.PlayerTarget != null && enemy.PlayerInRange(enemy.Data.detectionRange))
            enemy.FSM.ChangeState(new SwarmChaseState(_swarm));
    }
}

public class SwarmChaseState : ChaseState
{
    private readonly SwarmEnemy _swarm;
    public SwarmChaseState(SwarmEnemy s) { _swarm = s; }

    public override void Execute(EnemyBase enemy)
    {
        if (enemy.PlayerLost(enemy.Data.lostRange))
        {
            enemy.FSM.ChangeState(new SwarmIdleState(_swarm));
            return;
        }
        if (enemy.PlayerInRange(enemy.Data.attackRange))
        {
            enemy.FSM.ChangeState(new AttackState());
            return;
        }

        Vector3 dest = enemy.PlayerTarget.position + _swarm.GetWeaveOffset();
        enemy.Agent.SetDestination(dest);

        Vector3 dir = enemy.PlayerTarget.position - enemy.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            enemy.transform.rotation = Quaternion.Slerp(
                enemy.transform.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * enemy.Data.rotationSpeed);
    }
}