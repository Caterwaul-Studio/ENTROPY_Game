using UnityEngine;
using UnityEngine.InputSystem;

public class DeactivateLockdownInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private LockdownEvent lockdownEvent;
    [SerializeField] private InputActionReference interactActionReference;
    [SerializeField] private Sprite promptIcon;

    private bool canLook;

    public bool IsAvailableForInteraction => 
        lockdownEvent != null && lockdownEvent.IsActive && !lockdownEvent.IsComplete && lockdownEvent.LeverPulled;
    public bool HideCrosshairOnLook => false;
    public Sprite PromptIcon => promptIcon;
    public Color PromptColor => Color.white;
    public Transform BillboardParent => null;
    public string PromptText => "deactivate manual lockdown";

    public void OnLookEnter() => canLook = true;
    public void OnLookExit() => canLook = false;

    private void OnEnable()
    {
        if (interactActionReference)
        {
            interactActionReference.action.performed += OnInteract;
        }
    }

    private void OnDisable()
    {
        if(interactActionReference)
        {
            interactActionReference.action.performed -= OnInteract;
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed && canLook)
        {
            lockdownEvent.TryDeactivateLockdown();
        }
    }
}
