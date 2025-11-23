using UnityEngine;
using UnityEngine.SceneManagement;

public class LootPickup : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _scoreAmount = 50;
    [SerializeField] private string _pickupSoundName = "CoinPickup"; // Optional: If you have audio

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. Add Score
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(_scoreAmount);
            }

            // 2. Play Sound (Optional - purely for "juice")
            // AudioSource.PlayClipAtPoint(_pickupClip, transform.position);

            // 3. Recycle
            // Ensure "LootOrb" matches your ObjectPool tag exactly
            if (ObjectPooler.Instance != null)
                ObjectPooler.Instance.ReturnToPool("LootOrb", gameObject);
            else
                Destroy(gameObject);
        }
    }
}