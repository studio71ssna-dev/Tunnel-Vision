using UnityEngine;
using System.Collections.Generic;


public class BulletPooler : MonoBehaviour
{
    public static BulletPooler Instance;

    [System.Serializable]
    public class Pool
    {
        [Tooltip("A unique tag to identify the pool (e.g., 'PlayerBullet', 'EnemyGrunt').")]
        public string tag;
        [Tooltip("The prefab that this pool will manage.")]
        public GameObject prefab;
        [Tooltip("The initial number of objects to create in the pool.")]
        public int size;
    }

    [Header("Object Pools")]
    [Tooltip("List of all object pools to be created on start.")]
    public List<Pool> pools;

    // The dictionary holds the queues of GameObjects for each pool tag.
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

        // Create and pre-warm all the defined pools.
        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false); // Start with the object disabled.
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    /// <param name="tag">The tag of the pool to get an object from.</param>

    public GameObject GetFromPool(string tag)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag '{tag}' doesn't exist.");
            return null;
        }
        if (poolDictionary[tag].Count == 0)
        {
            Pool pool = pools.Find(p => p.tag == tag);
            if (pool != null)
            {
                Debug.LogWarning($"Pool with tag '{tag}' was empty. Expanding pool size.");
                GameObject newObj = Instantiate(pool.prefab);
                return newObj; // The new object is returned directly. It will be added to the pool on return.
            }
            return null;
        }

        // Get an object from the pool.
        GameObject objectToSpawn = poolDictionary[tag].Dequeue();

        // The object is returned active and ready to use.
        objectToSpawn.SetActive(true);

        return objectToSpawn;
    }

    /// <param name="tag">The tag of the pool the object belongs to.</param>
    /// <param name="objectToReturn">The GameObject instance to return.</param>
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
