using UnityEngine;
using System.Collections;

/// <summary>
/// Spawns waves of enemies at specified points using the EnemyPooler.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        [Tooltip("An identifier for the wave.")]
        public string name;
        [Tooltip("The pool tag of the enemy to spawn for this wave.")]
        public string enemyTag;
        [Tooltip("The number of enemies to spawn in this wave.")]
        public int count;
        [Tooltip("The rate of spawning in enemies per second.")]
        public float rate;
    }

    [Header("Spawning Configuration")]
    [Tooltip("An array of waves to be spawned in sequence.")]
    public Wave[] waves;
    [Tooltip("An array of locations where enemies can be spawned.")]
    public Transform[] spawnPoints;
    [Tooltip("Time in seconds between the end of one wave and the start of the next.")]
    public float timeBetweenWaves = 5f;

    private int nextWave = 0;
    private float waveCountdown;
    private bool isSpawning = false;

    private void Start()
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points referenced in the EnemySpawner.");
            this.enabled = false;
            return;
        }
        waveCountdown = timeBetweenWaves;
    }

    private void Update()
    {
        // If a wave is currently spawning, do nothing.
        if (isSpawning)
        {
            return;
        }

        // Countdown to the next wave.
        if (waveCountdown <= 0f)
        {
            // Check if there are more waves to spawn.
            if (nextWave < waves.Length)
            {
                StartCoroutine(SpawnWave(waves[nextWave]));
                nextWave++;
            }
            else
            {
                Debug.Log("All waves complete!");
                this.enabled = false; // Disable spawner after all waves.
            }
        }
        else
        {
            waveCountdown -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Coroutine to spawn all enemies in a single wave.
    /// </summary>
    private IEnumerator SpawnWave(Wave wave)
    {
        isSpawning = true;
        Debug.Log($"Spawning Wave: {wave.name}");

        for (int i = 0; i < wave.count; i++)
        {
            SpawnEnemy(wave.enemyTag);
            yield return new WaitForSeconds(1f / wave.rate);
        }

        waveCountdown = timeBetweenWaves;
        isSpawning = false;
    }

    /// <summary>
    /// Spawns a single enemy at a random spawn point.
    /// </summary>
    private void SpawnEnemy(string enemyTag)
    {
        // Get an enemy from the pooler.
        GameObject enemyObject = EnemyPooler.Instance.GetFromPool(enemyTag);
        if (enemyObject != null)
        {
            // Pick a random spawn point.
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            enemyObject.transform.position = spawnPoint.position;
            enemyObject.transform.rotation = spawnPoint.rotation;

            // The enemy's OnEnable method will handle resetting its state.
        }
    }
}
