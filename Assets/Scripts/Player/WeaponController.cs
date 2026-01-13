using Unity.Cinemachine;
using Cysharp.Threading.Tasks;
using Singletons; // Required for InputManager and PlayerLoadout
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class WeaponController : MonoBehaviour
{
    #region Variables
    [Header("Configuration")]
    [SerializeField] private Transform _muzzlePoint;
    [SerializeField] private MeshRenderer _gunMesh; // For changing color
    [SerializeField] private int _emissionMaterialIndex = 0; // Usually 0 or 1

    [Header("Arsenal")]
    // This will be overwritten by PlayerLoadout if it exists
    [SerializeField] private BulletData[] _weapons;

    [Header("Events")]
    public UnityEvent<int, int> OnAmmoChanged; // (Current, Max)
    public UnityEvent<bool> OnReloadingState; // To show reload bar
    public UnityEvent OnBulletSwapped; // Invoked when weapon cycles (for UI color updates)

    // State
    private int _currentIndex = 0;
    private int _currentAmmo;
    private bool _isReloading = false;
    private float _nextFireTime = 0f;
    private MaterialPropertyBlock _propBlock;

    // Cached Component
    private CinemachineImpulseSource _impulseSource;

    public BulletData CurrentWeapon => (_weapons != null && _weapons.Length > 0 && _currentIndex < _weapons.Length)
        ? _weapons[_currentIndex]
        : null;

    // New firing state for hold-to-fire
    private bool _isFiringHeld = false; // true while input is held
    private bool _firingLoopActive = false; // prevents multiple concurrent loops
    #endregion

    #region General Methods
    private void Awake()
    {
        // Force lock cursor whenever this level loads
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _propBlock = new MaterialPropertyBlock();
        _impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void Start()
    {
        // ---------------------------------------------------------
        // *** CRITICAL INTEGRATION: CONNECT TO SHOP LOADOUT ***
        // ---------------------------------------------------------
        // Check if the Singleton exists and has weapons (from the Shop scene)
        if (PlayerLoadout.Instance != null && PlayerLoadout.Instance.EquippedWeapons.Count > 0)
        {
            // Overwrite local array with the persistent loadout
            _weapons = PlayerLoadout.Instance.EquippedWeapons.ToArray();
            // Reset index to be safe
            _currentIndex = 0;
        }
        // ---------------------------------------------------------

        // Initialize Ammo for the first gun (shared magazine across weapons)
        if (CurrentWeapon != null)
        {
            _currentAmmo = CurrentWeapon.magazineSize;
            UpdateVisuals();
            OnAmmoChanged?.Invoke(_currentAmmo, CurrentWeapon.magazineSize);
            OnBulletSwapped?.Invoke();
        }
        else
        {
            Debug.LogWarning("WeaponController: No weapons configured. Check Inspector or PlayerLoadout.");
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

        // Visual / UI Updates
        UpdateVisuals();
        OnAmmoChanged?.Invoke(_currentAmmo, CurrentWeapon != null ? CurrentWeapon.magazineSize : 0);
        OnBulletSwapped?.Invoke();
    }

    // LINK TO: InputManager -> OnFireOutput (receives press state)
    private void Fire(bool isPressed)
    {
        // Update hold state
        _isFiringHeld = isPressed;

        // If pressed and we don't already have a firing loop running, start one.
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
            // Basic safety checks
            if (CurrentWeapon == null || _muzzlePoint == null) return;

            while (_isFiringHeld)
            {
                // *** AIM CHECK ***
                // Pause loop if not aiming (must aim to shoot)
                if (InputManager.Instance == null || !InputManager.Instance.IsAiming)
                {
                    await UniTask.Yield();
                    continue;
                }

                // Pause if reloading
                if (_isReloading)
                {
                    await UniTask.Yield();
                    continue;
                }

                // Fire Rate Check
                if (Time.time < _nextFireTime)
                {
                    await UniTask.Yield();
                    continue;
                }

                // Ammo Check (Pause if empty, wait for refill or release)
                if (_currentAmmo <= 0)
                {
                    while (_currentAmmo <= 0 && _isFiringHeld)
                    {
                        await UniTask.Yield();
                    }
                    if (!_isFiringHeld) break;
                    continue;
                }

                // ---------------- FIRE LOGIC ----------------
                _nextFireTime = Time.time + CurrentWeapon.fireRate;
                _currentAmmo--;

                OnAmmoChanged?.Invoke(_currentAmmo, CurrentWeapon.magazineSize);

                // Camera Shake
                if (_impulseSource != null)
                {
                    _impulseSource.GenerateImpulse();
                }

                // Spawn Bullet
                if (ObjectPooler.Instance != null)
                {
                    GameObject bulletObj = ObjectPooler.Instance.SpawnFromPool(
                        CurrentWeapon.poolTag,
                        _muzzlePoint.position,
                        _muzzlePoint.rotation
                    );

                    if (bulletObj != null && bulletObj.TryGetComponent<Bullet>(out var bulletScript))
                    {
                        bulletScript.Initialize(CurrentWeapon);
                    }
                }

                // Yield frame
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

    private async UniTask ReloadRoutine()
    {
        _isReloading = true;
        OnReloadingState?.Invoke(true);

        // Convert seconds to milliseconds for UniTask
        await UniTask.Delay((int)(CurrentWeapon.reloadTime * 1000));

        // Refill
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

    public float GetCurrentReloadTime()
    {
        return CurrentWeapon != null ? CurrentWeapon.reloadTime : 0f;
    }

    public int GetCurrentMagazineSize()
    {
        return CurrentWeapon != null ? CurrentWeapon.magazineSize : 0;
    }
    #endregion
}