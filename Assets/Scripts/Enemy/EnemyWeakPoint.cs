using UnityEngine;

public class EnemyWeakPoint : MonoBehaviour,IDamageable
{
    [SerializeField] private EnemyController _controller;

    public void TakeDamage(float amount, ElementType incomingType)
    {
        _controller.TakeDamage(amount, incomingType);
    }
}