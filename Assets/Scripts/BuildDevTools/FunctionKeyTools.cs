using System;
using System.Reflection;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
public class FunctionKeyTools : MonoBehaviour
{
    [SerializeField]
    private ZeroGravity zeroGravity;

    [SerializeField]
    private PlayerInput playerInput;

    [SerializeField]
    private bool allowFunctionkeys = false;

    void Awake()
    {
        if (allowFunctionkeys)
        {
            if (playerInput == null)
                playerInput = GetComponent<PlayerInput>();

            // enable the functionkeys map alongsie the other ones
            playerInput.actions.FindActionMap("FunctionKeyShortCuts").Enable();
        }
    }

    #region Input Methods

    public void OnF11(InputAction.CallbackContext context)
    {
        if(zeroGravity != null && context.performed)
        {
            zeroGravity.PlayerFreeMoveNoClip = !zeroGravity.PlayerFreeMoveNoClip;
        }
    }
    #endregion
}
