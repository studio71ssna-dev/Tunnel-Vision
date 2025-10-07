using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages pools of reusable GameObjects to improve performance by avoiding
/// frequent instantiation and destruction.
/// </summary>
public class EnemyPooler : MonoBehaviour
{
    /// <summary>
    /// Singleton instance to allow easy access from other scripts.
    /// </summary>
    public static EnemyPooler Instance;

    /// <summary>
    /// A class to define the properties of a single object pool.
    /// You can define these in the Inspector.
    /// </summary>
    [System.Serializable]
    public class Pool
    {
        [Tooltip("A unique tag to identify the pool.")]
        public string tag;
        [Tooltip("The prefab that this pool will manage.")]
        public GameObject prefab;
        [Tooltip("The initial number of objects to create in the pool.")]
        public int size;
    }

    [Header("Object Pools")]
    [Tooltip("List of all object pools to be created on start.")]
    public List<Pool> pools;

    // The dictionary will hold the queues of GameObjects for each pool tag.
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        // Set up the singleton instance.
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        // Iterate through all the defined pools.
        foreach (Pool pool in pools)
        {
            // Create a new queue for the current pool.
            Queue<GameObject> objectPool = new Queue<GameObject>();

            // Pre-instantiate the objects for the pool (pre-warming).
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false); // Start with the object disabled.
                objectPool.Enqueue(obj);
            }

            // Add the filled queue to the dictionary with its tag.
            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    /// <summary>
    /// Gets a GameObject from the specified pool.
    /// </summary>
    /// <param name="tag">The tag of the pool to get an object from.</param>
    /// <returns>A disabled GameObject from the pool, or a new one if the pool is empty and can expand.</returns>
    public GameObject GetFromPool(string tag)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag '{tag}' doesn't exist.");
            return null;
        }

        // If the pool is empty, we can optionally expand it.
        if (poolDictionary[tag].Count == 0)
        {
            Pool pool = pools.Find(p => p.tag == tag);
            if (pool != null)
            {
                Debug.LogWarning($"Pool with tag '{tag}' is empty. Expanding pool size.");
                GameObject newObj = Instantiate(pool.prefab);
                // The new object is returned directly, active, and will be added to the pool on return.
                return newObj;
            }
            return null;
        }

        // Get an object from the front of the queue.
        GameObject objectToSpawn = poolDictionary[tag].Dequeue();
        objectToSpawn.SetActive(true);

        return objectToSpawn;
    }

    /// <summary>
    /// Returns a GameObject to its pool.
    /// </summary>
    /// <param name="tag">The tag of the pool the object belongs to.</param>
    /// <param name="objectToReturn">The GameObject to return.</param>
    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag '{tag}' doesn't exist. Destroying object instead.");
            Destroy(objectToReturn);
            return;
        }

        objectToReturn.SetActive(false);
        poolDictionary[tag].Enqueue(objectToReturn);
    }
}
