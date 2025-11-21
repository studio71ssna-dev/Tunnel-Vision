using UnityEngine;

public class LootPickup : MonoBehaviour
{
    [SerializeField] private int _currencyAmount = 5;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // GameManager.Instance.AddCurrency(_currencyAmount);
            ObjectPooler.Instance.ReturnToPool("LootOrb", gameObject);
        }
    }
}