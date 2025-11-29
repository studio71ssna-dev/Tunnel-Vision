using UnityEngine;
using System.Collections.Generic;
using SingletonManager;

namespace Singletons
{
    public class PlayerLoadout : SingletonPersistent
    {
        public static PlayerLoadout Instance => GetInstance<PlayerLoadout>();

        [Header("Inventory")]
        // The weapons the player currently owns and will take to the next level
        public List<BulletData> EquippedWeapons = new List<BulletData>();

        // Default weapon (fallback if player buys nothing)
        [SerializeField] private BulletData _defaultWeapon;

        protected override void OnAwake()
        {
            // Ensure we always have at least one gun
            if (EquippedWeapons.Count == 0 && _defaultWeapon != null)
            {
                EquippedWeapons.Add(_defaultWeapon);
            }
        }

        public void AddWeapon(BulletData newWeapon)
        {
            if (!EquippedWeapons.Contains(newWeapon))
            {
                EquippedWeapons.Add(newWeapon);
            }
        }

        public void ClearLoadout()
        {
            EquippedWeapons.Clear();
            if (_defaultWeapon != null) EquippedWeapons.Add(_defaultWeapon);
        }
    }
}