using UnityEngine;
using Singletons;

public class PlayerSwapManager : MonoBehaviour
{
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private ToolController toolController;

    private void OnEnable()
    {
        InputManager.Instance.OnSwap += HandleSwap;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnSwap -= HandleSwap;
    }

    private void HandleSwap()
    {
        if (InputManager.Instance.IsAiming)
        {
            weaponController.SendMessage("CycleWeapon", SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            toolController.CycleTool();
        }
    }
}
