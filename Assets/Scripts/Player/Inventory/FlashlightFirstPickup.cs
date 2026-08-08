using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class FlashlightFirstPickup : MonoBehaviour, IInteractable
{
    public PersistantManager persistantManager;
    [SerializeField]
    private ZeroGravity player;
    [SerializeField]
    private GameObject flashlightPickupObject;

    //is the player withing grabbing distance of the flashlight
    [SerializeField]
    private bool canGrab;
    //can the flashlight be picked up yet?
    [SerializeField]
    private bool isGrabbable;
    [SerializeField]
    private Flashlight flashlight;

    // input action reference for the interact key to pickup
    [SerializeField] private InputActionReference interactActionReference;

    //IInteractable components
    [Header("IInteractable Components")]
    [SerializeField] private Sprite promptIcon;
    public bool IsAvailableForInteraction => isGrabbable;
    public bool HideCrosshairOnLook => false;
    public Sprite PromptIcon => promptIcon;
    public Color PromptColor => Color.white;
    public Transform BillboardParent => null;
    public string PromptText => "take flashlight";
    public void OnLookEnter() => canGrab = true;
    public void OnLookExit() => canGrab = false;

    public bool CanGrab
    {
        get { return canGrab; }
        set { canGrab = value; }
    }

    public bool IsGrabbable
    {
        get { return isGrabbable; }
    }

    private void OnEnable()
    {
        if (interactActionReference)
        {
            interactActionReference.action.performed += OnInteract;
        }
    }

    private void OnDisable()
    {
        if (interactActionReference)
        {
            interactActionReference.action.performed -= OnInteract;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        canGrab = false;
        isGrabbable = true;

        //if the persistant manager is null, find it and assign it to the variable.
        if (persistantManager == null)
        {
            persistantManager = FindFirstObjectByType<PersistantManager>();
        }
        //then restore the other necessary references from the persistant manager.
        if (player == null)
        {
            player = persistantManager.Player;
        }
        if(flashlight == null)
        {
            flashlight = FindFirstObjectByType<Flashlight>();
        }

        if(flashlight != null)
        {
            flashlight.OnFlashlightAcquired += flashlight.HandleFlashlightAcquired;
            flashlight.OnFlashlightTurnedOn += flashlight.HandleFlashlightOffOnToggled;
            flashlight.OnFlashlightToggledInv += flashlight.HandleFlashlightToggledInv;
        }

        if (!flashlight.TutorialComplete)
        {
            flashlight.flashlightToggled = false;
            flashlight.flashlightToggledInv = false;
            flashlight.tutorialStarted = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //if the persistant manager is null, find it and assign it to the variable.
        if (persistantManager == null)
        {
            persistantManager = FindFirstObjectByType<PersistantManager>();
            //then restore the other necessary references from the persistant manager.
            if (player == null)
            {
                player = persistantManager.Player;
            }
            flashlight = FindFirstObjectByType<Flashlight>();

            flashlight.OnFlashlightAcquired -= flashlight.HandleFlashlightAcquired;
            flashlight.OnFlashlightAcquired += flashlight.HandleFlashlightAcquired;
            flashlight.OnFlashlightTurnedOn -= flashlight.HandleFlashlightOffOnToggled;
            flashlight.OnFlashlightTurnedOn += flashlight.HandleFlashlightOffOnToggled;
            flashlight.OnFlashlightToggledInv -= flashlight.HandleFlashlightToggledInv;
            flashlight.OnFlashlightToggledInv += flashlight.HandleFlashlightToggledInv;
        }
        //used as a guard to ensure the flashlight doesn't force drop floating object when equipping
        flashlight.LookingAtFlashlight = canGrab;
    }

    private void OnDestroy()
    {
        if(flashlight != null)
        {
            flashlight.OnFlashlightAcquired -= flashlight.HandleFlashlightAcquired;
            flashlight.OnFlashlightTurnedOn -= flashlight.HandleFlashlightOffOnToggled;
            flashlight.OnFlashlightToggledInv -= flashlight.HandleFlashlightToggledInv;
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if(canGrab && isGrabbable)
        {
            isGrabbable = false;
            canGrab = false;

            flashlight.HasFlashlight = true;
        }
    }
}
