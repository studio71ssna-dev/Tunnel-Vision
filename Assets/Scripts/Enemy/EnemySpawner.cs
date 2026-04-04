// EnemySpawner.cs  (replaces old version)
// Unchanged public API — still uses ObjectPooler with weighted random tags.
// Now spawns EnemyBase descendants instead of the old EnemyController.
// UniTask async loops preserved exactly as before.

using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public struct EnemySpawnWeight
    {
        [Tooltip("Must match the tag in ObjectPooler (e.g. 'EnemyBrute')")]
        public string poolTag;
        [Tooltip("Higher = higher spawn chance")]
        public float weight;
    }

    [Header("Configuration")]
    [SerializeField] private List<EnemySpawnWeight> _enemies;
    [SerializeField] private Transform[] _spawnPoints;

    [Header("Spawn Timing")]
    [SerializeField] private float _startSpawnInterval  = 3f;
    [SerializeField] private float _minimumSpawnInterval = 0.5f;

    [Header("Difficulty Ramp")]
    [SerializeField] private float _rampFrequency  = 10f;
    [SerializeField] private float _rampReduction  = 0.2f;

    private float _currentInterval;
    private CancellationTokenSource _cts;

    private void Start()
    {
        _currentInterval = _startSpawnInterval;

        if (_enemies == null || _enemies.Count == 0 || _spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogError("[EnemySpawner] Missing spawn points or enemy weights.");
            return;
        }

        _cts = new CancellationTokenSource();
        SpawnLoop(_cts.Token).Forget();
        DifficultyLoop(_cts.Token).Forget();
    }

    private async UniTaskVoid SpawnLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            SpawnEnemy();
            await UniTask.Delay((int)(_currentInterval * 1000), cancellationToken: token);
        }
    }

    private async UniTaskVoid DifficultyLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await UniTask.Delay((int)(_rampFrequency * 1000), cancellationToken: token);

            if (_currentInterval > _minimumSpawnInterval)
            {
                _currentInterval = Mathf.Max(
                    _currentInterval - _rampReduction,
                    _minimumSpawnInterval);

                Debug.Log($"[EnemySpawner] Spawn interval → {_currentInterval:F2}s");
            }
        }
    }

    private void SpawnEnemy()
    {
        Transform pos = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
        string tag    = GetWeightedRandomTag();

        if (!string.IsNullOrEmpty(tag))
            ObjectPooler.Instance.SpawnFromPool(tag, pos.position, pos.rotation);
    }

    private string GetWeightedRandomTag()
    {
        float total = 0f;
        foreach (var e in _enemies) total += e.weight;

        float roll    = Random.Range(0f, total);
        float current = 0f;

        foreach (var e in _enemies)
        {
            current += e.weight;
            if (roll <= current) return e.poolTag;
        }

        return _enemies[0].poolTag;
    }

    private void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
