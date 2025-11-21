using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewEnemyType", menuName = "EnemyData/Enemy Type")]
public class EnemyType : ScriptableObject
{
    [Header("Identity")]
    public ElementType myType; // The Enum (Fire, Water, Poison)

    [Header("Relationships")]
    [Tooltip("Define how much damage I take from other elements.")]
    public List<ElementMultiplier> damageMultipliers;

    // Runtime optimization: Dictionary for fast lookup
    private Dictionary<ElementType, float> _multiplierMap;

    private void OnEnable()
    {
        // Convert List to Dictionary for O(1) access speed
        _multiplierMap = new Dictionary<ElementType, float>();
        foreach (var item in damageMultipliers)
        {
            if (!_multiplierMap.ContainsKey(item.incomingElement))
            {
                _multiplierMap.Add(item.incomingElement, item.multiplier);
            }
        }
    }

    public float GetDamageMultiplier(ElementType incomingType)
    {
        if (_multiplierMap == null) OnEnable(); // Safety check

        // 1. Immunity Check (Self vs Self)
        if (incomingType == myType) return 0f;

        // 2. Lookup Table
        if (_multiplierMap.ContainsKey(incomingType))
        {
            return _multiplierMap[incomingType];
        }

        // 3. Default (Neutral damage)
        return 1f;
    }

    [System.Serializable]
    public struct ElementMultiplier
    {
        public ElementType incomingElement;
        [Range(0f, 5f)] public float multiplier; // e.g., 2.0 for Weakness, 0.5 for Resistance
    }
}