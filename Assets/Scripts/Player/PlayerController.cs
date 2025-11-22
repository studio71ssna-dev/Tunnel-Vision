using System.Runtime.CompilerServices;
using UnityEngine;
using Singletons;

public class PlayerController : MonoBehaviour
{
    #region General Variables
    [Header("Variables for Movement")]
    [SerializeField] private float _moveSpeed = 5f;
    private float _currentMoveSpeed = 1f;

    [Header("Variables for Looking Around")]
    [SerializeField] private float _lookSensitivity = 2f;
    #endregion

    #region General Methods

    private void OnEnable()
    {
        InputManager.Instance.OnReload += Reload;
        InputManager.Instance.OnSwap += Swap;
        InputManager.Instance.OnShoot += Shoot;
    }
    private void OnDisable()
    {
        InputManager.Instance.OnReload -= Reload;
        InputManager.Instance.OnSwap -= Swap;
        InputManager.Instance.OnShoot -= Shoot;
    }

    private void Awake()
    {
        _currentMoveSpeed = _moveSpeed;
    }

    private void Update()
    {
        MovePlayer();
        LookAround();
    }
    #endregion

    #region Created Methods
    private void MovePlayer()
    {
        Vector2 moveDirection = InputManager.Instance.MoveDirection;
        Vector3 move = new Vector3(moveDirection.x, 0, moveDirection.y);
        transform.Translate(move * _currentMoveSpeed * Time.deltaTime, Space.Self);
    }

    private void LookAround()
    {
        float horizontalLook = InputManager.Instance.HorizontalLook;
        transform.Rotate(Vector3.up * horizontalLook * _lookSensitivity * Time.deltaTime);
    }
    
    private void Reload()
    {
        // Implement reload logic here
    }
    
    private void Swap()
    {
        // Implement swap logic here
    }

    private void Shoot(bool isShooting)
    {
        // Implement shooting logic here
    }

    #endregion
}