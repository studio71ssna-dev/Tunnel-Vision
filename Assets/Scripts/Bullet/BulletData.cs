using UnityEngine;

[CreateAssetMenu(fileName = "BulletData", menuName = "Bullets/BulletData")]
public class BulletData : ScriptableObject
{
    [Header("Pooling")]
    public string poolTag; // Make sure this matches BulletPooler config!

    [Header("Visuals")]
    [ColorUsage(true, true)] // Allows HDR intensity selection
    public Color elementColor = Color.white;

    [Header("Basic Stats")]
    public float speed = 50f;
    public float damage = 10f;
    public float lifetime = 3f;
    public float fireRate = 0.15f; // Added per-weapon fire rate

    [Header("Reloading")]
    public int magazineSize = 30;
    public float reloadTime = 1.5f;

    [Header("Elemental Type")]
    public ElementType elementType;

    [Header("Collision")]
    public LayerMask hitLayers;
    public bool destroyOnHit = true;

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip hitSound;
}