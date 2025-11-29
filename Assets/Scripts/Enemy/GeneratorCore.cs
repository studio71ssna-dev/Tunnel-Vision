using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

public class GeneratorCore : MonoBehaviour, IDamageable
{
    [System.Serializable]
    public struct ElementSpawnConfig
    {
        public ElementType type;
        public string enemyPoolTag;
        public Color visualColor;
    }

    [Header("Core Stats")]
    [SerializeField] private float _maxHealth = 500f;
    [SerializeField] private float _spawnInterval = 3f;
    [SerializeField] private float _elementSwitchInterval = 10f; // Changes bias every 10s

    [Header("Configuration")]
    [SerializeField] private List<ElementSpawnConfig> _configs;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private MeshRenderer _coreRenderer; // To show current color

    // Events
    public static System.Action OnCoreDestroyed;

    private float _currentHealth;
    private int _currentConfigIndex;
    private CancellationTokenSource _cts;
    private bool _isDestroyed = false;

    private void Start()
    {
        _currentHealth = _maxHealth;
        _cts = new CancellationTokenSource();

        // Start Logic
        SpawnLoop(_cts.Token).Forget();
        ElementSwitchLoop(_cts.Token).Forget();

        UpdateVisuals();
    }

    // 1. Spawning Logic
    private async UniTaskVoid SpawnLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && !_isDestroyed)
        {
            SpawnEnemy();
            await UniTask.Delay((int)(_spawnInterval * 1000), cancellationToken: token);
        }
    }

    // 2. Element Switching Logic
    private async UniTaskVoid ElementSwitchLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && !_isDestroyed)
        {
            await UniTask.Delay((int)(_elementSwitchInterval * 1000), cancellationToken: token);

            // Pick a new element (Simple Round Robin or Random)
            _currentConfigIndex = (_currentConfigIndex + 1) % _configs.Count;
            UpdateVisuals();
        }
    }

    private void SpawnEnemy()
    {
        if (_spawnPoints.Length == 0) return;

        // Pick random point
        Transform pos = _spawnPoints[Random.Range(0, _spawnPoints.Length)];

        // Spawn enemy based on CURRENT Element Config
        string tag = _configs[_currentConfigIndex].enemyPoolTag;

        if (ObjectPooler.Instance != null)
        {
            ObjectPooler.Instance.SpawnFromPool(tag, pos.position, pos.rotation);
        }
    }

    private void UpdateVisuals()
    {
        if (_coreRenderer != null && _configs.Count > 0)
        {
            _coreRenderer.material.color = _configs[_currentConfigIndex].visualColor;
        }
    }

    // IDamageable Implementation
    public void TakeDamage(float amount, ElementType incomingType)
    {
        if (_isDestroyed) return;


        if (incomingType == _configs[_currentConfigIndex].type)
        {
            amount *= 0.5f;
        }

        _currentHealth -= amount;

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        _isDestroyed = true;
        _cts.Cancel();
        OnCoreDestroyed?.Invoke();

        Destroy(gameObject);
    }

    private void OnDisable()
    {
        _cts?.Cancel();
    }

    // Helper for the Forecast System
    public ElementType GetStartingElement()
    {
        if (_configs.Count > 0) return _configs[0].type;
        return ElementType.Fire; // Default
    }
}