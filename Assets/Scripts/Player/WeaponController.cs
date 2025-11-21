using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;

public class WeaponController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private Transform _muzzlePoint;
    [SerializeField] private MeshRenderer _gunMesh; // For changing color
    [SerializeField] private int _emissionMaterialIndex = 0; // Usually 0 or 1

    [Header("Arsenal")]
    [SerializeField] private BulletData[] _weapons; // Drag Fire, Ice, Rock Data here

    [Header("Events")]
    public UnityEvent<int, int> OnAmmoChanged; // (Current, Max)
    public UnityEvent<bool> OnReloadingState; // To show reload bar

    // State
    private int _currentIndex = 0;
    private int _currentAmmo;
    private bool _isReloading = false;
    private float _nextFireTime = 0f;
    private MaterialPropertyBlock _propBlock;

    private BulletData CurrentWeapon => _weapons[_currentIndex];

    private void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        // Initialize Ammo for the first gun
        _currentAmmo = CurrentWeapon.magazineSize;
        UpdateVisuals();
        OnAmmoChanged.Invoke(_currentAmmo, CurrentWeapon.magazineSize);
    }

    // LINK TO: InputManager -> OnWeaponScrollOutput
    public void CycleWeapon(float scrollDirection)
    {
        if (_isReloading) return; // Block swap during reload

        int direction = (int)Mathf.Sign(scrollDirection);
        _currentIndex += direction;

        // Loop Logic
        if (_currentIndex >= _weapons.Length) _currentIndex = 0;
        if (_currentIndex < 0) _currentIndex = _weapons.Length - 1;

        // Reset stats for new weapon
        _currentAmmo = CurrentWeapon.magazineSize;
        OnAmmoChanged.Invoke(_currentAmmo, CurrentWeapon.magazineSize);

        UpdateVisuals();
    }

    // LINK TO: InputManager -> OnFireOutput
    public void Fire()
    {
        if (_isReloading || Time.time < _nextFireTime) return;

        if (_currentAmmo <= 0)
        {
            ReloadRoutine().Forget();
            return;
        }

        _nextFireTime = Time.time + CurrentWeapon.fireRate;
        _currentAmmo--;
        OnAmmoChanged.Invoke(_currentAmmo, CurrentWeapon.magazineSize);

        GameObject bulletObj = ObjectPooler.Instance.SpawnFromPool(
        CurrentWeapon.poolTag,
        _muzzlePoint.position,
        _muzzlePoint.rotation
    );

        if (bulletObj != null)
        {
            if (bulletObj.TryGetComponent<Bullet>(out var bulletScript))
            {
                bulletScript.Initialize(CurrentWeapon);
            }
        }
    }

    // LINK TO: InputManager -> OnReloadOutput
    public void StartReload()
    {
        if (!_isReloading && _currentAmmo < CurrentWeapon.magazineSize)
        {
            ReloadRoutine().Forget();
        }
    }

    private async UniTaskVoid ReloadRoutine()
    {
        _isReloading = true;
        OnReloadingState.Invoke(true);

        // Wait (converts seconds to milliseconds)
        await UniTask.Delay((int)(CurrentWeapon.reloadTime * 1000));

        _currentAmmo = CurrentWeapon.magazineSize;
        OnAmmoChanged.Invoke(_currentAmmo, CurrentWeapon.magazineSize);

        _isReloading = false;
        OnReloadingState.Invoke(false);
    }

    private void UpdateVisuals()
    {
        // Use MaterialPropertyBlock to avoid creating new Material instances
        _gunMesh.GetPropertyBlock(_propBlock, _emissionMaterialIndex);
        _propBlock.SetColor("_EmissionColor", CurrentWeapon.elementColor);
        _gunMesh.SetPropertyBlock(_propBlock, _emissionMaterialIndex);
    }
}