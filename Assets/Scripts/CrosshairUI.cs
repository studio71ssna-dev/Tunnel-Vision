using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CrosshairUI : MonoBehaviour
{
    private RectTransform crosshairRectTransform;
    private PlayerControls controls;

    void Awake()
    {
        crosshairRectTransform = GetComponent<RectTransform>();
        controls = new PlayerControls();
    }

    void OnEnable()
    {
        controls.Enable();
    }

    void OnDisable()
    {
        controls.Disable();
    }

    void Update()
    {
        Vector2 currentMousePosition = controls.Player.MousePosition.ReadValue<Vector2>();
        crosshairRectTransform.position = currentMousePosition;
    }
}
