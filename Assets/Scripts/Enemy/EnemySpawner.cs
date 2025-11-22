using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public struct EnemySpawnWeight
    {
        [Tooltip("Must match the tag in ObjectPooler (e.g. 'EnemyFire')")]
        public string poolTag;
        [Tooltip("Higher number = higher chance to spawn")]
        public float weight;
    }

    [Header("Configuration")]
    [SerializeField] private List<EnemySpawnWeight> _enemies;
    [SerializeField] private Transform[] _spawnPoints;

    [Header("Spawn Timing")]
    [SerializeField] private float _startSpawnInterval = 3f;
    [SerializeField] private float _minimumSpawnInterval = 0.5f;

    [Header("Difficulty Settings")]
    [SerializeField] private float _rampFrequency = 10f; // Every 10 seconds
    [SerializeField] private float _rampReduction = 0.2f; // Reduce interval by 0.2s

    private float _currentInterval;
    private CancellationTokenSource _cts;

    private void Start()
    {
        _currentInterval = _startSpawnInterval;

        if (_enemies == null || _enemies.Count == 0 || _spawnPoints.Length == 0)
        {
            Debug.LogError("EnemySpawner: Missing Spawn Points or Enemy Weights.");
            return;
        }

        // Initialize Token for clean Async cleanup
        _cts = new CancellationTokenSource();

        // Fire and Forget the loops
        SpawnLoop(_cts.Token).Forget();
        DifficultyLoop(_cts.Token).Forget();
    }

    private async UniTaskVoid SpawnLoop(CancellationToken token)
    {
        // We use a while loop that checks for cancellation automatically
        while (!token.IsCancellationRequested)
        {
            SpawnEnemy();

            // Wait for the current interval
            // Note: Converting float seconds to milliseconds for Delay
            await UniTask.Delay((int)(_currentInterval * 1000), cancellationToken: token);
        }
    }

    private async UniTaskVoid DifficultyLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            // Wait for the ramp frequency duration
            await UniTask.Delay((int)(_rampFrequency * 1000), cancellationToken: token);

            // Make game harder
            if (_currentInterval > _minimumSpawnInterval)
            {
                _currentInterval -= _rampReduction;
                _currentInterval = Mathf.Max(_currentInterval, _minimumSpawnInterval);

                Debug.Log($"<color=red>Alert:</color> Spawn Rate increased! New Interval: {_currentInterval}");
            }
        }
    }

    private void SpawnEnemy()
    {
        // 1. Pick Random Position
        Transform pos = _spawnPoints[Random.Range(0, _spawnPoints.Length)];

        // 2. Pick Random Enemy based on Weight
        string tagToSpawn = GetWeightedRandomTag();

        if (!string.IsNullOrEmpty(tagToSpawn))
        {
            // Using your existing ObjectPooler logic
            ObjectPooler.Instance.SpawnFromPool(tagToSpawn, pos.position, pos.rotation);
        }
    }

    private string GetWeightedRandomTag()
    {
        float totalWeight = 0f;
        foreach (var enemy in _enemies) totalWeight += enemy.weight;

        float randomValue = Random.Range(0, totalWeight);
        float currentWeight = 0f;

        foreach (var enemy in _enemies)
        {
            currentWeight += enemy.weight;
            if (randomValue <= currentWeight)
            {
                return enemy.poolTag;
            }
        }

        // Fallback (should theoretically not reach here)
        return _enemies[0].poolTag;
    }

    private void OnDisable()
    {
        // IMPORTANT: Cancel the tasks so they don't run in the background or error out
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}