using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;


[System.Serializable]
public struct DamageModifier
{
    public ElementType elementType;
    [Tooltip("Damage multiplier. 1 = normal, >1 = weakness, <1 = resistance, 0 = immune")]
    [Range(0f, 5f)]
    public float multiplier;
}

public enum AttackType { Stationary, Melee, Ranged }

[CreateAssetMenu(fileName = "EnemyData", menuName = "Enemies/BaseEnemySettings")]
public class EnemyData : ScriptableObject
{
    [Header("Basic Stats")]
    public string enemyName = "New Enemy";
    public float maxHealth = 100f;
    public float moveSpeed = 3f;
    public GameObject visualPrefab; // The 3D model or visual representation of the enemy

    [Header("Elemental Properties")]
    [Tooltip("The enemy's own elemental type.")]
    public ElementType selfElementType;

    [Tooltip("Define specific resistances and weaknesses to different bullet types here.")]
    public List<DamageModifier> damageModifiers;


    [Header("Combat Behavior")]
    [Tooltip("Determines how the enemy will attack.")]
    public AttackType attackType = AttackType.Stationary;

    [Tooltip("The range within which the enemy will start its attack (e.g., start chasing or shooting).")]
    public float detectionRange = 15f;

    [Header("Melee Settings")]
    [Tooltip("Damage dealt on contact with the player. Only used if Attack Type is Melee.")]
    public float contactDamage = 10f;
    [Tooltip("The distance at which the melee enemy stops moving towards the player.")]
    public float stoppingDistance = 1.5f;
    [Tooltip("How often (in seconds) the enemy deals melee damage while player is in range.")]
    public float meleeDamageInterval = 1f;


    [Header("Ranged Settings")]
    [Tooltip("The bullet this enemy fires. Only used if Attack Type is Ranged.")]
    public BulletData projectileData;
    [Tooltip("How many seconds between each shot. Only used if Attack Type is Ranged.")]
    public float fireRate = 2f;
    public UnityEvent onReact;

    [Header("Audio")]
    public AudioClip hitSound;
    public AudioClip deathSound;

    public float GetDamageMultiplier(ElementType incomingElement)
    {
        foreach (var modifier in damageModifiers)
        {
            if (modifier.elementType == incomingElement)
            {
                return modifier.multiplier;
            }
        }
        return 1f;
    }
}