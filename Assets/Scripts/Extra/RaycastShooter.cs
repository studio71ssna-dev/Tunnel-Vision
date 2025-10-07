using UnityEngine;
using UnityEngine.InputSystem;

public class RaycastShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private RectTransform crosshair;

    [Header("Settings")]
    [SerializeField] private float shootDistance = 100f;
    [SerializeField] private float damage = 25f;
    [SerializeField] private LayerMask shootableLayer;

    private PlayerInput playerInput;
    private InputAction shootAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        shootAction = playerInput.actions["Shoot"];

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        shootAction.performed += _ => OnShoot();
    }

    private void OnDisable()
    {
        shootAction.performed -= _ => OnShoot();
    }

    private void Update()
    {
        // Handle aiming with mouse position
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);

        // Optional: Visualize the aiming ray in editor
        Debug.DrawRay(ray.origin, ray.direction * shootDistance, Color.red);

        if (crosshair != null)
        {
            crosshair.position = Mouse.current.position.ReadValue();
        }
    }

    private void OnShoot()
    {
      

        // Get mouse position from input system
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);

        // Perform the raycast
        if (Physics.Raycast(ray, out RaycastHit hit, shootDistance, shootableLayer))
        {

            TargetHealth targetHealth = hit.collider.GetComponent<TargetHealth>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage);
            }
        }
    }

}