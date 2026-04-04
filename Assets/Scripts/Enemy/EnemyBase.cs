// EnemyBase.cs  (v2)
// Exposes EnemyAnimator as `Anim` so states call typed helpers
// (SetIdle / SetWalking / SetAttacking / SetDead / SetHit) directly
// instead of going through the string-key IAnimatable API.

using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyAnimator))]
public abstract class EnemyBase : MonoBehaviour, IDamageable, IAnimatable
{
    [Header("Config")]
    [SerializeField] protected EnemyData _data;

    [Header("References")]
    [SerializeField] protected EnemyHealthUI _healthUI;

    // ── Public read-only refs used by states ─────────────────────
    public EnemyData Data => _data;
    public NavMeshAgent Agent { get; private set; }
    public Transform PlayerTarget { get; private set; }
    public EnemyStateMachine FSM { get; private set; }
    public EnemyAnimator Anim { get; private set; }  // typed animator access

    // ── Runtime state ────────────────────────────────────────────
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public float LastAttackTime { get; set; }

    private Animator _unityAnimator;

    // ── Unity lifecycle ──────────────────────────────────────────
    protected virtual void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Anim = GetComponent<EnemyAnimator>();
        _unityAnimator = GetComponent<Animator>();

        if (_data != null)
        {
            Agent.speed = _data.moveSpeed;
            Agent.angularSpeed = _data.rotationSpeed * 100f;
        }

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) PlayerTarget = playerObj.transform;
        else Debug.LogWarning($"[{name}] No GameObject tagged 'Player' found.");
    }

    protected virtual void OnEnable()
    {
        if (_data == null) { Debug.LogError($"[{name}] EnemyData not assigned!", this); return; }

        CurrentHealth = _data.maxHealth;
        IsDead = false;
        LastAttackTime = -999f;
        Agent.isStopped = false;

        Anim.Initialise(_unityAnimator, _data);
        _healthUI?.Initialize(_data.maxHealth, Color.red);

        FSM = new EnemyStateMachine(this);
        FSM.Initialise(CreateStates(FSM));
    }

    protected virtual void Update()
    {
        if (IsDead) return;
        FSM?.Tick();
    }

    protected virtual void OnDisable() { }

    // ── Template methods ─────────────────────────────────────────
    protected abstract IEnemyState CreateStates(EnemyStateMachine fsm);

    protected virtual void OnDeath()
    {
        SpawnLoot();
        Destroy(gameObject);
    }

    // Public so DeadState / animation events can trigger it
    public void OnDeathCleanup() => OnDeath();

    // ── IDamageable ──────────────────────────────────────────────
    public void TakeDamage(float amount, ElementType incomingType)
    {
        if (IsDead) return;

        float multiplier = (incomingType == _data.elementType)
            ? _data.sameElementMultiplier : 1f;

        CurrentHealth = Mathf.Max(CurrentHealth - amount * multiplier, 0f);
        _healthUI?.UpdateHealth(CurrentHealth);
        Anim.SetHit();  // ← typed call, no string lookup

        if (CurrentHealth <= 0f)
        {
            IsDead = true;
            FSM.ChangeState(new DeadState());
        }
    }

    // ── IAnimatable (string API kept for compatibility) ──────────
    public void PlayAnimation(string key) => Anim.PlayAnimation(key);
    public void CrossFadeAnimation(string key, float t = 0.15f) => Anim.CrossFadeAnimation(key, t);

    // ── Helpers for states ───────────────────────────────────────
    public float SqrDistToPlayer()
    {
        if (PlayerTarget == null) return float.MaxValue;
        return (PlayerTarget.position - transform.position).sqrMagnitude;
    }

    public bool PlayerInRange(float range) => SqrDistToPlayer() <= range * range;
    public bool PlayerLost(float lostRange) => SqrDistToPlayer() > lostRange * lostRange;

    private void SpawnLoot()
    {
        if (ObjectPooler.Instance == null || _data == null) return;
        Vector3 pos = transform.position;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 10f))
            pos = hit.point + Vector3.up * 0.5f;
        else
            pos.y = 0.5f;
        ObjectPooler.Instance.SpawnFromPool(_data.lootPoolTag, pos, Quaternion.identity);
    }
}