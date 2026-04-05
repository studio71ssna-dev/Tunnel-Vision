// EnemyData.cs
// ScriptableObject that drives every tunable value for an enemy variant.
// Create via: Assets > Create > Enemy > EnemyData
// Each of the 3 variants gets its own asset; new types just get a new asset.

using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Enemy/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyName = "Enemy";
    public ElementType elementType = ElementType.Fire;
    public string lootPoolTag = "FireLoot"; // matched to ObjectPooler pool tag

    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 8f;

    [Header("Detection")]
    public float detectionRange = 20f;   // Idle -> Chase trigger
    public float lostRange     = 25f;    // Chase -> Idle trigger (hysteresis)

    [Header("Attack")]
    public float attackRange   = 8f;     // Chase -> Attack trigger
    public float attackDamage  = 10f;
    public float attackCooldown = 2f;    // seconds between attacks

    [Header("Elemental Resistances")]
    [Tooltip("Multiplier applied when hit by matching element (< 1 = resistant, > 1 = weak)")]
    public float sameElementMultiplier = 0.5f;

    [Header("Animation Keys")]
    // These string keys match the dictionary entries in EnemyAnimator.
    // Change here; EnemyAnimator and states never hard-code strings.
    public string animIdle   = "Idle";
    public string animWalk   = "Walk";
    public string animAttack = "Attack";
    public string animHit    = "Hit";
    public string animDead   = "Dead";
}
