using Unity.Cinemachine;
using Cysharp.Threading.Tasks;
using Singletons;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class WeaponController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private Transform _muzzlePoint;
    [SerializeField] private MeshRenderer _gunMesh;
    [SerializeField] private int _emissionMaterialIndex = 0;

    [Header("Arsenal")]
    [SerializeField] private BulletData[] _weapons;

    [Header("Events")]
    public UnityEvent<int, int> OnAmmoChanged;
    public UnityEvent<bool> OnReloadingState;
    public UnityEvent OnBulletSwapped;

    private int _currentIndex = 0;
    private int _currentAmmo;
    private bool _isReloading = false;
    private float _nextFireTime = 0f;

    private bool _isFiringHeld = false;
    private bool _firingLoopActive = false;

    private MaterialPropertyBlock _propBlock;
    private CinemachineImpulseSource _impulseSource;
    private AimManager _aimManager;

    public BulletData CurrentWeapon =>
        (_weapons != null && _weapons.Length > 0 && _currentIndex < _weapons.Length)
            ? _weapons[_currentIndex]
            : null;

    private void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
        _impulseSource = GetComponent<CinemachineImpulseSource>();
        _aimManager = FindFirstObjectByType<AimManager>();
    }

    private void Start()
    {
        if (PlayerLoadout.Instance != null && PlayerLoadout.Instance.EquippedWeapons.Count > 0)
        {
            _weapons = PlayerLoadout.Instance.EquippedWeapons.ToArray();
            _currentIndex = 0;
        }

        if (CurrentWeapon != null)
        {
            _currentAmmo = CurrentWeapon.magazineSize;
            UpdateVisuals();
            OnAmmoChanged?.Invoke(_currentAmmo, CurrentWeapon.magazineSize);
            OnBulletSwapped?.Invoke();
        }
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnShoot += Fire;
            InputManager.Instance.OnReload += StartReload;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnShoot -= Fire;
            InputManager.Instance.OnReload -= StartReload;
        }
    }

    public void CycleWeapon()
    {
        if (_weapons == null || _weapons.Length == 0) return;

        _currentIndex++;
        if (_currentIndex >= _weapons.Length)
            _currentIndex = 0;

        UpdateVisuals();
        OnAmmoChanged?.Invoke(_currentAmmo, CurrentWeapon.magazineSize);
        OnBulletSwapped?.Invoke();
    }

    private void Fire(bool isPressed)
    {
        _isFiringHeld = isPressed;

        if (isPressed && !_firingLoopActive)
        {
            FiringLoop().Forget();
        }
    }

    private async UniTask FiringLoop()
    {
        _firingLoopActive = true;

        try
        {
            if (CurrentWeapon == null || _muzzlePoint == null) return;

            while (_isFiringHeld)
            {
                if (!InputManager.Instance.IsAiming || _isReloading)
                {
                    await UniTask.Yield();
                    continue;
                }

                if (Time.time < _nextFireTime)
                {
                    await UniTask.Yield();
                    continue;
                }

                if (_currentAmmo <= 0)
                {
                    await UniTask.Yield();
                    continue;
                }

                _nextFireTime = Time.time + CurrentWeapon.fireRate;
                _currentAmmo--;

                OnAmmoChanged?.Invoke(_currentAmmo, CurrentWeapon.magazineSize);

                // Camera shake
                _impulseSource?.GenerateImpulse();

                // 🔥 TUNNEL VISION (CORE MECHANIC)
                _aimManager?.RequestFireFOVKick();

                // Spawn bullet
                GameObject bulletObj = ObjectPooler.Instance.SpawnFromPool(
                    CurrentWeapon.poolTag,
                    _muzzlePoint.position,
                    _muzzlePoint.rotation
                );

                if (bulletObj != null && bulletObj.TryGetComponent(out Bullet bullet))
                {
                    bullet.Initialize(CurrentWeapon);
                }

                await UniTask.Yield();
            }
        }
        finally
        {
            _firingLoopActive = false;
        }
    }

    public void StartReload()
    {
        if (_isReloading || CurrentWeapon == null || _currentAmmo >= CurrentWeapon.magazineSize)
            return;

        ReloadRoutine().Forget();
    }

    private async UniTask ReloadRoutine()
    {
        _isReloading = true;
        OnReloadingState?.Invoke(true);

        // ✅ Instantly recover tunnel vision on reload
        _aimManager?.ForceRecoverFOV();

        await UniTask.Delay((int)(CurrentWeapon.reloadTime * 1000));

        _currentAmmo = CurrentWeapon.magazineSize;
        OnAmmoChanged?.Invoke(_currentAmmo, CurrentWeapon.magazineSize);

        _isReloading = false;
        OnReloadingState?.Invoke(false);
    }

    private void UpdateVisuals()
    {
        if (_gunMesh == null || CurrentWeapon == null) return;

        _gunMesh.GetPropertyBlock(_propBlock, _emissionMaterialIndex);
        _propBlock.SetColor("_EmissionColor", CurrentWeapon.elementColor);
        _gunMesh.SetPropertyBlock(_propBlock, _emissionMaterialIndex);
    }

    public float GetCurrentReloadTime()
    {
        return CurrentWeapon != null ? CurrentWeapon.reloadTime : 0f;
    }

    public int GetCurrentMagazineSize()
    {
        return CurrentWeapon != null ? CurrentWeapon.magazineSize : 0;
    }
}
