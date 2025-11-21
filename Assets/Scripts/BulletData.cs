using UnityEngine;

[CreateAssetMenu(fileName = "BulletData", menuName = "Bullets/BaseBulletSettings")]
public class BulletData : ScriptableObject
{
    [Header("Pooling")]
    [Tooltip("The tag used by the ObjectPooler for this bullet type. Must match a tag in the pooler.")]
    public string poolTag;

    [Header("Basic Stats")]
    public float speed = 10f;
    public float damage = 1f;
    public float lifetime = 3f; // Time before bullet is returned to the pool

    [Header("Elemental Type")]
    [Tooltip("The elemental type of this bullet for damage calculation.")]
    public ElementType elementType = ElementType.Physical;

    [Header("Collision")]
    public LayerMask hitLayers; // What can this bullet hit?
    public bool destroyOnHit = true;

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip hitSound;
}
