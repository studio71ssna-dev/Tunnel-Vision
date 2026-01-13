using UnityEngine;
using Singletons;

public class LootPickup : MonoBehaviour
{
    [Header("Loot Settings")]
    [SerializeField] private ElementType lootType;
    [SerializeField] private int lootAmount = 50;

    private bool _playerInRange;
    private ToolController _toolController;

    private void Start()
    {
        _toolController = FindObjectOfType<ToolController>();
    }

    private void OnEnable()
    {
        InputManager.Instance.OnInteract += TryCollect;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnInteract -= TryCollect;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            _playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            _playerInRange = false;
    }

    private void TryCollect()
    {
        if (!_playerInRange) return;
        if (InputManager.Instance.IsAiming) return;

        if (!ToolRules.CanCollect(lootType, _toolController.CurrentTool))
            return;

        ScoreManager.Instance.AddScore(lootAmount);

        ObjectPooler.Instance.ReturnToPool(lootType + "Loot", gameObject);
    }
}
