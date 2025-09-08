using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputSO", menuName = "Scriptable Objects/InputSO")]
public class InputSO : ScriptableObject, InputSystem_Actions.IPlayerActions
{
    private InputSystem_Actions input;
    public event Action<bool> OnJumpChange;
    public event Action OnInteractPress;

    void OnEnable()
    {
        if (input == null)
            input = new();

        input.Player.AddCallbacks(this);
        input.Player.Enable();

        input.Enable();
    }

    void OnDisable()
    {
        input.Disable();
    }

    public Vector2 GetMoveDir()
    {
        return input.Player.Move.ReadValue<Vector2>();
    }

    public Vector2 GetLookDir()
    {
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
