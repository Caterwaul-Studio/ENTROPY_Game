using System;
using System.Reflection;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
public class FunctionKeyTools : MonoBehaviour
{
    private InputAction f11Action;
    private ZeroGravity zeroGravity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        zeroGravity = GetComponentInChildren<ZeroGravity>();

        f11Action = new InputAction("F11", binding: "<Keyboard>/f11");
        f11Action.performed += OnF11;
        f11Action.Enable();
    }

    // Update is called once per frame
    void OnDestroy()
    {
        f11Action.performed -= OnF11;

        f11Action.Disable();
    }

    #region Input Methods

    public void OnF11(InputAction.CallbackContext context)
    {
        if(zeroGravity != null)
        {
            zeroGravity.PlayerFreeMoveNoClip = !zeroGravity.PlayerFreeMoveNoClip;
        }
    }
    #endregion
}
