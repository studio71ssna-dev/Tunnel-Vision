using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Image _fillImage; // Optional: To change color based on element

    [Header("Settings")]
    [SerializeField] private bool _hideWhenFull = true;

    private Transform _camTransform;

    private void Awake()
    {
        // Cache the camera for performance
        if (Camera.main != null)
            _camTransform = Camera.main.transform;

        // Find components if not assigned
        if (_healthSlider == null) _healthSlider = GetComponentInChildren<Slider>();
    }

    private void OnEnable()
    {
        // Reset logic when spawned from pool
        if (_camTransform == null && Camera.main != null)
            _camTransform = Camera.main.transform;
    }

    // Called by EnemyController
    public void Initialize(float maxHealth, Color elementColor)
    {
        _healthSlider.maxValue = maxHealth;
        _healthSlider.value = maxHealth;

        if (_fillImage != null)
            _fillImage.color = elementColor;

        if (_hideWhenFull)
            _healthSlider.gameObject.SetActive(false);
        else
            _healthSlider.gameObject.SetActive(true);
    }

    // Called by EnemyController
    public void UpdateHealth(float currentHealth)
    {
        _healthSlider.value = currentHealth;

        if (_hideWhenFull)
        {
            // Show only if damaged
            bool shouldShow = currentHealth < _healthSlider.maxValue && currentHealth > 0;
            _healthSlider.gameObject.SetActive(shouldShow);
        }
    }

    // Make the UI always face the camera
    private void LateUpdate()
    {
        if (_camTransform != null)
        {
            // Align rotation with camera so it's always flat to the screen
            transform.rotation = _camTransform.rotation;
        }
    }
}