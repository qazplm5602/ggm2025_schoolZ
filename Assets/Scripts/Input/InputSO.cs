using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputSO", menuName = "Scriptable Objects/InputSO")]
public class InputSO : ScriptableObject, InputSystem_Actions.IPlayerActions
{
    private InputSystem_Actions input;
    public event Action<bool> OnJumpChange;
    public event Action OnInteractPress;

    private void Awake()
    {
        InitializeInput();
    }

    private void OnEnable()
    {
        InitializeInput();
    }

    private void InitializeInput()
    {
        if (input == null)
            input = new();

        input.Player.AddCallbacks(this);
        input.Player.Enable();
        input.Enable();
    }

    void OnDisable()
    {
        if (input != null)
            input.Disable();
    }

    public Vector2 GetMoveDir()
    {
        // UI가 활성화되면 이동 입력을 무시
        if (TowerPlacementSystem.Instance != null && TowerPlacementSystem.Instance.IsUIActive)
        {
            return Vector2.zero;
        }
        return input.Player.Move.ReadValue<Vector2>();
    }

    public Vector2 GetLookDir()
    {
        // UI가 활성화되면 마우스 입력을 무시
        if (TowerPlacementSystem.Instance != null && TowerPlacementSystem.Instance.IsUIActive)
        {
            return Vector2.zero;
        }
        return input.Player.Look.ReadValue<Vector2>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
    }

    public void OnLook(InputAction.CallbackContext context)
    {
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnInteractPress?.Invoke();
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        OnJumpChange?.Invoke(context.performed);
    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
    }

    public void OnNext(InputAction.CallbackContext context)
    {
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
    }
}
