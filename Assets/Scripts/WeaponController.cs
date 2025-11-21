using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[System.Serializable]
public class OnBulletSwitchedEvent : UnityEvent<BulletData> { }

public class WeaponController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform gunPoint;
    // [SerializeField] private Transform playerBody; // REMOVE THIS - Player body won't rotate with aim
    [SerializeField] private Camera mainCamera; // NEW: Reference to the main camera for raycasting

    [Header("Aiming Settings")]
    // [SerializeField] private float mouseSensitivity = 100f; // No longer directly used for camera rotation
    [Tooltip("The layer mask for what the aim cursor can hit to determine shooting direction.")]
    [SerializeField] private LayerMask aimHitLayers; // NEW: Layers for raycasting aim

    [Header("Weapon Settings")]
    [SerializeField] private BulletData[] bulletTypes;
    [SerializeField] private float reloadTime = 1.5f;
    [SerializeField] private int maxAmmo = 10;
    [SerializeField] private float fireCooldown = 0.1f;

    [Header("Events")]
    public OnBulletSwitchedEvent onBulletSwitched;
    public UnityEvent onShoot, onReloadStart, onReloadComplete;

    // Runtime variables
    private int currentAmmo;
    private int currentBulletIndex = 0;
    private bool isReloading = false;
    private bool canShoot = true;
    // private float xRotation = 0f; // REMOVE THIS - No more direct camera rotation
    private PlayerControls controls;
    private Vector2 currentMousePosition; // NEW: Store current mouse position for aiming

    private void Awake()
    {
        controls = new PlayerControls();
        currentAmmo = maxAmmo;
        // Cursor.lockState = CursorLockMode.Locked; // REMOVE OR CHANGE: We want the cursor visible and free
        // Cursor.visible = false; // REMOVE OR CHANGE: We want the cursor visible

        // Ensure the cursor is visible and unlocked for this aiming style
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("WeaponController: Main Camera not found! Assign it in the Inspector or ensure your camera is tagged 'MainCamera'.");
                enabled = false; // Disable script if no camera
                return;
            }
        }
    }

    private void OnEnable()
    {
        controls.Enable();
        controls.Player.Shoot.performed += _ => OnShoot();
        controls.Player.Reload.performed += _ => OnReload();
        controls.Player.SwitchBullet.performed += ctx => OnSwitchBullet(ctx.ReadValue<Vector2>());
        // controls.Player.MousePosition.performed += ctx => Aim(ctx.ReadValue<Vector2>()); // MousePosition now just updates currentMousePosition
        controls.Player.MousePosition.performed += ctx => currentMousePosition = ctx.ReadValue<Vector2>();
        controls.Player.MousePosition.canceled += ctx => currentMousePosition = ctx.ReadValue<Vector2>(); // Also update on release for continuous tracking
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    // LateUpdate is a good place for camera/aiming logic after all other updates
    private void LateUpdate()
    {
        // No direct camera or player body rotation needed here anymore
        // The aiming logic will be in the Shoot method
    }

    // The Aim method is no longer directly manipulating camera/player rotation.
    // Instead, it's about determining the target point in the world from the cursor.
    // We'll integrate this logic directly into Shoot for clarity.
    /*
    private void Aim(Vector2 mouseDelta)
    {
        // This method is now effectively absorbed into how we calculate the bullet direction
        // based on the currentMousePosition.
    }
    */

    private async void OnShoot()
    {
        if (!canShoot || isReloading || currentAmmo <= 0) return;

        Shoot();
        currentAmmo--;
        onShoot?.Invoke();
        await FireCooldownAsync();

        // Optional: Trigger reload if ammo is now zero
        if (currentAmmo <= 0)
        {
            OnReload();
        }
    }

    private void Shoot()
    {
        BulletData bulletData = bulletTypes[currentBulletIndex];

        // Get a bullet from the object pooler instead of instantiating.
        GameObject bulletObject = BulletPooler.Instance.GetFromPool(bulletData.poolTag);

        if (bulletObject != null)
        {
            Vector3 targetPoint;

            // NEW AIMING LOGIC: Raycast from camera through mouse position
            Ray ray = mainCamera.ScreenPointToRay(currentMousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, aimHitLayers))
            {
                // If the ray hits something on the defined layers, aim at that point
                targetPoint = hit.point;
            }
            else
            {
                // If nothing is hit, aim at a point far away along the ray direction
                // This simulates shooting into the distance when not targeting a specific object.
                targetPoint = ray.GetPoint(100f); // Aim 100 units away
            }

            // Set bullet position to gunPoint
            bulletObject.transform.position = gunPoint.position;

            // Calculate rotation to look from gunPoint towards the targetPoint
            // Ensure the bullet's forward direction is towards the target.
            bulletObject.transform.LookAt(targetPoint);


            // The bullet is already active, so we just need to initialize it.
            bulletObject.GetComponent<Bullet>().Initialize(bulletData);

            // Play shoot sound if available
            if (bulletData.shootSound != null)
            {
                AudioSource.PlayClipAtPoint(bulletData.shootSound, gunPoint.position);
            }
        }
    }

    private async UniTask FireCooldownAsync()
    {
        canShoot = false;
        await UniTask.Delay((int)(fireCooldown * 1000));
        canShoot = true;
    }

    private async void OnReload()
    {
        if (!isReloading && currentAmmo < maxAmmo)
        {
            await ReloadAsync();
        }
    }

    private async UniTask ReloadAsync()
    {
        isReloading = true;
        onReloadStart?.Invoke();
        await UniTask.Delay((int)(reloadTime * 1000));
        currentAmmo = maxAmmo;
        isReloading = false;
        onReloadComplete?.Invoke();
    }

    private void OnSwitchBullet(Vector2 scrollDelta)
    {
        if (scrollDelta.y == 0) return;

        int direction = scrollDelta.y > 0 ? 1 : -1;
        currentBulletIndex = (currentBulletIndex + direction + bulletTypes.Length) % bulletTypes.Length;
        onBulletSwitched?.Invoke(bulletTypes[currentBulletIndex]);
    }

    // Optional: Draw a debug ray in the editor to visualize the aim
    private void OnDrawGizmos()
    {
        if (Application.isPlaying && mainCamera != null && gunPoint != null)
        {
            Vector3 targetPoint;
            Ray ray = mainCamera.ScreenPointToRay(currentMousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, aimHitLayers))
            {
                targetPoint = hit.point;
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(targetPoint, 0.1f); // Draw a small sphere at the hit point
            }
            else
            {
                targetPoint = ray.GetPoint(100f);
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(gunPoint.position, targetPoint);
        }
    }
}