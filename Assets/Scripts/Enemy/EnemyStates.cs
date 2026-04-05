// EnemyStates.cs  (v2)
// States now call enemy.Anim.SetIdle() / SetWalking() / SetAttacking() / SetDead()
// instead of CrossFadeAnimation("Walk") etc.
// The Animator Controller handles all blending via bool-parameter transitions.

using UnityEngine;

// ═══════════════════════════════════════════
//  IDLE
// ═══════════════════════════════════════════
public class IdleState : IEnemyState
{
    public virtual void Enter(EnemyBase enemy)
    {
        enemy.Agent.isStopped = true;
        enemy.Anim.SetIdle();
    }

    public virtual void Execute(EnemyBase enemy)
    {
        if (enemy.PlayerTarget == null) return;
        if (enemy.PlayerInRange(enemy.Data.detectionRange))
            enemy.FSM.ChangeState(new ChaseState());
    }

    public virtual void Exit(EnemyBase enemy) { }
}

// ═══════════════════════════════════════════
//  CHASE
// ═══════════════════════════════════════════
public class ChaseState : IEnemyState
{
    private const float DEST_UPDATE_INTERVAL = 0.2f;
    private float _nextUpdate;

    public virtual void Enter(EnemyBase enemy)
    {
        enemy.Agent.isStopped = false;
        enemy.Anim.SetWalking();
        _nextUpdate = 0f;
    }

    public virtual void Execute(EnemyBase enemy)
    {
        if (enemy.PlayerTarget == null) return;

        if (enemy.PlayerLost(enemy.Data.lostRange))
        {
            enemy.FSM.ChangeState(new IdleState());
            return;
        }

        if (enemy.PlayerInRange(enemy.Data.attackRange))
        {
            enemy.FSM.ChangeState(new AttackState());
            return;
        }

        if (Time.time >= _nextUpdate)
        {
            enemy.Agent.SetDestination(enemy.PlayerTarget.position);
            _nextUpdate = Time.time + DEST_UPDATE_INTERVAL;
        }

        RotateTowards(enemy);
    }

    public virtual void Exit(EnemyBase enemy)
    {
        enemy.Agent.isStopped = true;
    }

    protected void RotateTowards(EnemyBase enemy)
    {
        Vector3 dir = enemy.PlayerTarget.position - enemy.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        enemy.transform.rotation = Quaternion.Slerp(
            enemy.transform.rotation,
            Quaternion.LookRotation(dir),
            Time.deltaTime * enemy.Data.rotationSpeed);
    }
}

// ═══════════════════════════════════════════
//  ATTACK
// ═══════════════════════════════════════════
public class AttackState : IEnemyState
{
    public virtual void Enter(EnemyBase enemy)
    {
        enemy.Agent.isStopped = true;
        enemy.Anim.SetAttacking();
    }

    public virtual void Execute(EnemyBase enemy)
    {
        if (enemy.PlayerTarget == null) return;

        if (!enemy.PlayerInRange(enemy.Data.attackRange))
        {
            enemy.FSM.ChangeState(new ChaseState());
            return;
        }

        RotateTowards(enemy);

        if (Time.time >= enemy.LastAttackTime + enemy.Data.attackCooldown)
        {
            enemy.LastAttackTime = Time.time;
            PerformAttack(enemy);
        }
    }

    public virtual void Exit(EnemyBase enemy)
    {
        enemy.Anim.SetIdle();
    }

    protected virtual void PerformAttack(EnemyBase enemy)
    {
        // Re-trigger the attack animation for each hit
        // (IsAttacking stays true throughout; the Controller loops the clip)
        if (enemy.PlayerTarget.TryGetComponent(out PlayerHealth ph))
            ph.TakeDamage((int)enemy.Data.attackDamage);
    }

    protected void RotateTowards(EnemyBase enemy)
    {
        Vector3 dir = enemy.PlayerTarget.position - enemy.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        enemy.transform.rotation = Quaternion.Slerp(
            enemy.transform.rotation,
            Quaternion.LookRotation(dir),
            Time.deltaTime * enemy.Data.rotationSpeed);
    }
}

// ═══════════════════════════════════════════
//  DEAD
// ═══════════════════════════════════════════
public class DeadState : IEnemyState
{
    public void Enter(EnemyBase enemy)
    {
        enemy.Agent.isStopped = true;
        enemy.Agent.enabled = false;
        enemy.Anim.SetDead();
        // Cleanup happens either via:
        //   a) Animation Event on the Dead clip calling OnDeathCleanup()
        //   b) A timer — see EnemyBase.OnDeathCleanup()
    }

    public void Execute(EnemyBase enemy) { }
    public void Exit(EnemyBase enemy) { }
}