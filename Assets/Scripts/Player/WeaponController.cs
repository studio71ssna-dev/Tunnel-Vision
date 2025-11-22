using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;
using Singletons;

public class WeaponController : MonoBehaviour
{
    #region Variables
    [Header("Configuration")]
    [SerializeField] private Transform _muzzlePoint;
    [SerializeField] private MeshRenderer _gunMesh; // For changing color
    [SerializeField] private int _emissionMaterialIndex = 0; // Usually 0 or 1

    [Header("Arsenal")]
    [SerializeField] private BulletData[] _weapons; // Drag Fire, Ice, Rock Data here

    [Header("Events")]
    public UnityEvent<int, int> OnAmmoChanged; // (Current, Max)
    public UnityEvent<bool> OnReloadingState; // To show reload bar
    public UnityEvent OnWeaponSwapped; // Invoked when weapon cycles (for UI color updates)

    // State
    private int _currentIndex = 0;
    private int _currentAmmo;
    private bool _isReloading = false;
    private float _nextFireTime = 0f;
    private MaterialPropertyBlock _propBlock;
    public BulletData CurrentWeapon => (_weapons != null && _weapons.Length > 0) ? _weapons[_currentIndex] : null;

    // New firing state for hold-to-fire
    private bool _isFiringHeld = false; // true while input is held
    private bool _firingLoopActive = false; // prevents multiple concurrent loops
    #endregion

    #region General Methods
    private void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        // Initialize Ammo for the first gun
        if (CurrentWeapon != null)
        {
            _currentAmmo = CurrentWeapon.magazineSize;
            UpdateVisuals();
            OnAmmoChanged?.Invoke(_currentAmmo, CurrentWeapon.magazineSize);
            OnWeaponSwapped?.Invoke();
        }
        else
        {
            Debug.LogWarning("WeaponController: No weapons configured (\"_weapons\" is null or empty).");
        }
    }
    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnSwap += CycleWeapon;
            // Subscribe to OnShoot which provides a bool for performed state
            InputManager.Instance.OnShoot += Fire;
            // Subscribe to reload action so pressing R calls StartReload
            InputManager.Instance.OnReload += StartReload;
        }
    }
    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnSwap -= CycleWeapon;
            InputManager.Instance.OnShoot -= Fire;
            InputManager.Instance.OnReload -= StartReload;
        }
    }

    #endregion

    #region Created Method
    // LINK TO: InputManager -> OnWeaponScrollOutput
    private void CycleWeapon()
    {
        if (_weapons == null || _weapons.Length == 0) return;

        _currentIndex += 1;

        // Loop Logic
        if (_currentIndex >= _weapons.Length) _currentIndex = 0;

        // Reset ammo for new weapon
        _currentAmmo = CurrentWeapon != null ? CurrentWeapon.magazineSize : 0;

        UpdateVisuals();
        OnAmmoChanged?.Invoke(_currentAmmo, CurrentWeapon != null ? CurrentWeapon.magazineSize : 0);
        OnWeaponSwapped?.Invoke();
    }

    // LINK TO: InputManager -> OnFireOutput (now receives press state)
    private void Fire(bool isPressed)
    {
        // Update hold state
        _isFiringHeld = isPressed;

        // If pressed and we don't already have a firing loop running, start one
        if (isPressed && !_firingLoopActive)
        {
            FiringLoop().Forget();
        }
    }

    // Asynchronous firing loop that runs while the input is held
    private async UniTask FiringLoop()
    {
        _firingLoopActive = true;
        try
        {
            // Basic safety checks before starting
            if (CurrentWeapon == null)
            {
                Debug.LogWarning("WeaponController: No CurrentWeapon available. Aborting firing loop.");
                return;
            }

            if (_muzzlePoint == null)
            {
                Debug.LogWarning("WeaponController: Muzzle point is not assigned. Aborting firing loop.");
                return;
            }

            while (_isFiringHeld)
            {
                // If currently reloading, wait until reload finishes or player stops holding
                if (_isReloading)
                {
                    await UniTask.Yield();
                    continue;
                }

                // Respect fire rate
                if (Time.time < _nextFireTime)
                {
                    await UniTask.Yield();
                    continue;
                }

                // If out of ammo, DO NOT auto-reload: wait until ammo is replenished externally or the player releases
                if (_currentAmmo <= 0)
                {
                    // Wait while player holds and ammo is zero
                    while (_currentAmmo <= 0 && _isFiringHeld)
                    {
                        await UniTask.Yield();
                    }

                    // If player released while waiting, exit loop
                    if (!_isFiringHeld) break;

                    // If ammo was replenished, continue firing
                    continue;
                }

                // Fire one bullet
                _nextFireTime = Time.time + CurrentWeapon.fireRate;
                _currentAmmo--;
                OnAmmoChanged?.Invoke(_currentAmmo, CurrentWeapon.magazineSize);

                if (ObjectPooler.Instance == null)
                {
                    Debug.LogWarning("WeaponController: ObjectPooler instance is null. Cannot spawn bullets.");
                }
                else
                {
                    GameObject bulletObj = ObjectPooler.Instance.SpawnFromPool
                    (
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

                // Yield a frame before checking loop conditions again so fire rate is enforced by _nextFireTime
                await UniTask.Yield();
            }
        }
        finally
        {
            _firingLoopActive = false;
        }
    }

    // LINK TO: InputManager -> OnReloadOutput
    public void StartReload()
    {
        if (!_isReloading && CurrentWeapon != null && _currentAmmo < CurrentWeapon.magazineSize)
        {
            ReloadRoutine().Forget();
        }
    }

    // Made awaitable so firing loop can wait for reload to complete
    private async UniTask ReloadRoutine()
    {
        _isReloading = true;
        OnReloadingState?.Invoke(true);

        // Wait (converts seconds to milliseconds)
        await UniTask.Delay((int)(CurrentWeapon.reloadTime * 1000));

        _currentAmmo = CurrentWeapon.magazineSize;
        OnAmmoChanged?.Invoke(_currentAmmo, CurrentWeapon.magazineSize);

        _isReloading = false;
        OnReloadingState?.Invoke(false);
    }

    private void UpdateVisuals()
    {
        if (_gunMesh == null || CurrentWeapon == null) return;

        // Use MaterialPropertyBlock to avoid creating new Material instances
        _gunMesh.GetPropertyBlock(_propBlock, _emissionMaterialIndex);
        _propBlock.SetColor("_EmissionColor", CurrentWeapon.elementColor);
        _gunMesh.SetPropertyBlock(_propBlock, _emissionMaterialIndex);
    }

    // Expose current weapon reload time for UI
    public float GetCurrentReloadTime()
    {
        return CurrentWeapon != null ? CurrentWeapon.reloadTime : 0f;
    }

    // Expose current magazine size for UI population
    public int GetCurrentMagazineSize()
    {
        return CurrentWeapon != null ? CurrentWeapon.magazineSize : 0;
    }
    #endregion
}