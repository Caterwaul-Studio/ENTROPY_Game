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

    [SerializeField] private GameObject useFlashlightCanvas;
    [SerializeField] private CanvasGroup useFlashlightCanvasGroup;

    [SerializeField] private GameObject toggleFlashlightCanvas;
    [SerializeField] private CanvasGroup toggleFlashlightCanvasGroup;

    // add all additional events based stuff here
    public bool flashlightToggled;
    public bool flashlightToggledInv;
    public bool tutorialStarted;

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
            flashlight.OnFlashlightAcquired += HandleFlashlightAcquired;
            flashlight.OnFlashlightTurnedOn += HandleFlashlightOffOnToggled;
            flashlight.OnFlashlightToggledInv += HandleFlashlightToggledInv;
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

            flashlight.OnFlashlightAcquired -= HandleFlashlightAcquired;
            flashlight.OnFlashlightAcquired += HandleFlashlightAcquired;
            flashlight.OnFlashlightTurnedOn -= HandleFlashlightOffOnToggled;
            flashlight.OnFlashlightTurnedOn += HandleFlashlightOffOnToggled;
            flashlight.OnFlashlightToggledInv -= HandleFlashlightToggledInv;
            flashlight.OnFlashlightToggledInv += HandleFlashlightToggledInv;
        }
        //used as a guard to ensure the flashlight doesn't force drop floating object when equipping
        flashlight.LookingAtFlashlight = canGrab;
    }

    private void OnDestroy()
    {
        if(flashlight != null)
        {
            flashlight.OnFlashlightAcquired -= HandleFlashlightAcquired;
            flashlight.OnFlashlightTurnedOn -= HandleFlashlightOffOnToggled;
            flashlight.OnFlashlightToggledInv -= HandleFlashlightToggledInv;


        }
    }

    private void HandleFlashlightAcquired(bool acquired)
    {
        if (acquired)
        {
            flashlightPickupObject.SetActive(false);
            flashlight.EquipFlashlightFromScene();
        }

        // only run the tutorial the first time the flashlight is picked up
        // (e.g. skip it if TutorialComplete was already true from a save file)
        if (!flashlight.TutorialComplete && !tutorialStarted)
        {
            tutorialStarted = true;
            StartCoroutine(FlashlightTutorial());
        }
    }

    private void HandleFlashlightOffOnToggled(bool turnedOn)
    {
        //add logic here for the tutorial of the flashlight
        flashlightToggled = true;
    }

    private void HandleFlashlightToggledInv(bool toggledInv)
    {
        flashlightToggledInv = true;
    }

    public IEnumerator FlashlightTutorial()
    {
        persistantManager.inventoryManager.InTutorial = true;
        //fade in the use flashlight panel
        persistantManager.inventoryManager.tutorialCanvases.FadeIn(
            persistantManager.inventoryManager.useFlashlightCanvasPrefab,
            ref useFlashlightCanvas, ref useFlashlightCanvasGroup,
            persistantManager.inventoryManager.tutorialCanvases.tutorialCanvasesPos);

        //wait until the player uses left click to turn on/off the flashlight
        flashlightToggled = false;
        yield return new WaitUntil(() => flashlightToggled);

        //fade out the use flashlight panel
        persistantManager.inventoryManager.tutorialCanvases.FadeOut(
            useFlashlightCanvas, useFlashlightCanvasGroup, () =>
            {
                useFlashlightCanvas = null;
                useFlashlightCanvasGroup = null;
            });

        yield return new WaitForSeconds(1f);

        //fade in the toggle flashlight panel
        persistantManager.inventoryManager.tutorialCanvases.FadeIn(
            persistantManager.inventoryManager.toggleFlashlightCanvasPrefab,
            ref toggleFlashlightCanvas, ref toggleFlashlightCanvasGroup, 
            persistantManager.inventoryManager.tutorialCanvases.tutorialCanvasesPos);

        //wait until the player uses left click to toggle the flashlight
        flashlightToggledInv = false; 
        yield return new WaitUntil(() => flashlightToggledInv);

        //fade out the toggle flashlight panel
        persistantManager.inventoryManager.tutorialCanvases.FadeOut(
            toggleFlashlightCanvas, toggleFlashlightCanvasGroup, () =>
            {
                toggleFlashlightCanvas = null;
                toggleFlashlightCanvasGroup = null;
            });
        flashlight.lookingAtFlashlight = false;
        flashlight.TutorialComplete = true;
        persistantManager.inventoryManager.InTutorial = !flashlight.TutorialComplete;
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
