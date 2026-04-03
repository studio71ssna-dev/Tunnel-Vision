using UnityEngine;
using UnityEngine.UI;
using SingletonManager;
using System.Collections;

namespace Singletons
{
    public class UIManager : MonoBehaviour
    {
        [Header("Player UI")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image reloadImage; // radial image (Filled, Radial360)
        [SerializeField] private string playerTag = "Player";

        [Header("Ammo UI")]
        [Tooltip("Parent object with Horizontal Layout Group where bullet images will be instantiated as children")]
        [SerializeField] private GameObject bulletContainer;
        [Tooltip("Bullet image prefab to instantiate for each magazine slot")]
        [SerializeField] private GameObject bulletPrefab;

        private PlayerHealth _playerHealth;
        private WeaponController _weapon_controller;
        private Coroutine _reloadCoroutine;

        private void Awake()
        {
            // Find WeaponController early so we can subscribe to swap events before first Start
            var playerObj = GameObject.FindWithTag(playerTag);
            if (playerObj == null) return;

            _weapon_controller = playerObj.GetComponentInChildren<WeaponController>();
            if (_weapon_controller != null)
            {
                _weapon_controller.OnBulletSwapped.AddListener(UpdateBulletColor);
            }
        }

        private void OnEnable()
        {
            if (InputManager.Instance != null)
                InputManager.Instance.OnSwap += UpdateBulletColor;
        }

        private void OnDisable()
        {
            if (InputManager.Instance != null)
                InputManager.Instance.OnSwap -= UpdateBulletColor;
        }

        private void Start()
        {
            if (healthSlider == null)
            {
                Debug.LogWarning("UIManager: Health Slider is not assigned in the inspector.");
            }

            if (reloadImage == null)
            {
                Debug.LogWarning("UIManager: Reload Image is not assigned in the inspector.");
            }
            else
            {
                // Ensure it's disabled initially
                reloadImage.enabled = false;
                reloadImage.fillAmount =0f;
            }

            if (bulletContainer == null)
            {
                Debug.LogWarning("UIManager: Bullet container is not assigned in the inspector.");
            }
            if (bulletPrefab == null)
            {
                Debug.LogWarning("UIManager: Bullet prefab is not assigned in the inspector.");
            }

            // Find player by tag and get PlayerHealth
            var playerObj = GameObject.FindWithTag(playerTag);
            if (playerObj == null)
            {
                Debug.LogWarning($"UIManager: No GameObject found with tag '{playerTag}'.");
                return;
            }

            _playerHealth = playerObj.GetComponent<PlayerHealth>();
            if (_playerHealth == null)
            {
                Debug.LogWarning("UIManager: PlayerHealth component not found on player object.");
                return;
            }

            // Hook slider max to player's max health and set initial value
            if (healthSlider != null)
            {
                healthSlider.maxValue = _playerHealth.MaxHealth;
                healthSlider.value = _playerHealth.CurrentHealth;
            }

            // If WeaponController wasn't found in Awake, try to find it now
            if (_weapon_controller == null)
            {
                _weapon_controller = playerObj.GetComponentInChildren<WeaponController>();
                if (_weapon_controller != null)
                {
                    _weapon_controller.OnBulletSwapped.AddListener(UpdateBulletColor);
                }
            }

            // Populate ammo UI initially using current weapon magazine size
            if (_weapon_controller == null)
            {
                Debug.LogWarning("UIManager: WeaponController component not found on player or its children. Ammo UI will not be initialized.");
                return;
            }

            int magSize = _weapon_controller.GetCurrentMagazineSize();
            if (magSize >0)
            {
                PopulateAmmoUI(magSize);
            }

            // Update bullet colors to match current weapon at start
            UpdateBulletColor();

            // Note: do not auto-subscribe to other weapon events here — user will hook UI methods in the editor as requested
        }

        public void OnPlayerTakeDamage(int current, int max)
        {
            if (healthSlider == null) return;
            // Ensure slider max is synced (in case max changed at runtime)
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }

        private void OnPlayerDeath()
        {
            if (healthSlider == null) return;
            healthSlider.value =0f;
        }

        public void HandleReloadingState(bool isReloading)
        {
            if (reloadImage == null) return;

            if (isReloading)
            {
                // Start progress animation
                if (_reloadCoroutine != null)
                {
                    StopCoroutine(_reloadCoroutine);
                    _reloadCoroutine = null;
                }

                float reloadTime = _weapon_controller != null ? _weapon_controller.GetCurrentReloadTime() :0f;
                reloadImage.enabled = true;
                reloadImage.fillAmount =0f;
                _reloadCoroutine = StartCoroutine(ReloadProgress(reloadTime));
            }
            else
            {
                // Stop and hide
                if (_reloadCoroutine != null)
                {
                    StopCoroutine(_reloadCoroutine);
                    _reloadCoroutine = null;
                }

                reloadImage.fillAmount =0f;
                reloadImage.enabled = false;
            }
        }

        private IEnumerator ReloadProgress(float reloadTime)
        {
            if (reloadTime <=0f)
            {
                // If reloadTime is zero or invalid, immediately show full and disable
                reloadImage.fillAmount =1f;
                yield return null;
                reloadImage.enabled = false;
                _reloadCoroutine = null;
                yield break;
            }

            float elapsed =0f;
            while (elapsed < reloadTime)
            {
                elapsed += Time.deltaTime;
                if (reloadImage != null)
                    reloadImage.fillAmount = Mathf.Clamp01(elapsed / reloadTime);
                yield return null;
            }

            if (reloadImage != null)
                reloadImage.fillAmount =1f;

            // small frame delay to ensure UI shows full
            yield return null;

            if (reloadImage != null)
                reloadImage.enabled = false;

            _reloadCoroutine = null;
        }

        // Populate the horizontal group with `count` bullet prefabs
        private void PopulateAmmoUI(int count)
        {
            if (bulletContainer == null || bulletPrefab == null) return;

            // Clear existing children
            for (int i = bulletContainer.transform.childCount -1; i >=0; i--)
            {
                var child = bulletContainer.transform.GetChild(i).gameObject;
                Destroy(child);
            }

            // Instantiate bullet images
            for (int i =0; i < count; i++)
            {
                var go = Instantiate(bulletPrefab, bulletContainer.transform, false);
                go.transform.localScale = Vector3.one;
            }
        }

        // New: method to be hooked to WeaponController.OnAmmoChanged via inspector
        // Matches the signature (current, max) like OnPlayerTakeDamage
        public void OnAmmoChanged(int current, int max)
        {
            Debug.Log($"UIManager.OnAmmoChanged called: current={current} max={max}");
            if (bulletContainer == null || bulletPrefab == null) return;

            // Ensure the UI has at least `max` children: instantiate if needed
            int childCount = bulletContainer.transform.childCount;
            if (childCount < max)
            {
                int toCreate = max - childCount;
                for (int i =0; i < toCreate; i++)
                {
                    var go = Instantiate(bulletPrefab, bulletContainer.transform, false);
                    go.transform.localScale = Vector3.one;
                }
                childCount = bulletContainer.transform.childCount;
            }

            // If there are more children than max, just deactivate the extras (do not destroy)
            if (childCount > max)
            {
                for (int i = max; i < childCount; i++)
                {
                    var extra = bulletContainer.transform.GetChild(i).gameObject;
                    extra.SetActive(false);
                }
                childCount = max;
            }

            // Now set active state for the first `max` children so that exactly `current` are enabled
            for (int i =0; i < childCount; i++)
            {
                var child = bulletContainer.transform.GetChild(i).gameObject;
                child.SetActive(i < current);
            }

            // Ensure the bullet images match the current weapon color after repopulating
            UpdateBulletColor();
        }

        // New method: update bullet images color to match current weapon color
        // Pulls color from WeaponController.CurrentWeapon and applies to children and prefab
        public void UpdateBulletColor()
        {
            if (bulletContainer == null || _weapon_controller == null) return;

            var weapon = _weapon_controller.CurrentWeapon;
            if (weapon == null)
            {
                Debug.Log("UIManager.UpdateBulletColor: CurrentWeapon is null");
                return;
            }

            Color color = weapon.elementColor;

            // Update existing children (use GetComponentInChildren to find Image if nested)
            for (int i =0; i < bulletContainer.transform.childCount; i++)
            {
                var child = bulletContainer.transform.GetChild(i).gameObject;
                var img = child.GetComponentInChildren<Image>(true);
                if (img != null)
                {
                    img.color = color;
                }
            }

            // Also update the prefab so newly instantiated bullets match the color
            if (bulletPrefab != null)
            {
                var prefabImg = bulletPrefab.GetComponentInChildren<Image>(true);
                if (prefabImg != null)
                {
                    prefabImg.color = color;
                }
            }
        }
    }
}
