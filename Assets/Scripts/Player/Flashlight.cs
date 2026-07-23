using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.InputSystem;

public class Flashlight : MonoBehaviour
{
    [SerializeField]
    private GameObject player;
    [SerializeField]
    ZeroGravity zeroGPlayer;
    [SerializeField]
    private Transform holdPos;
    [SerializeField]
    private Camera cam;
    [SerializeField]
    private PlayerUIManager uiManager;

    private bool lookingAtFlashlight = false;
    [SerializeField]
    private bool hasFlashlight = false;

    [SerializeField]
    GameObject flashlightInHand;
    [SerializeField]
    private GameObject flashlightOutHand;

    [SerializeField]
    private bool flashlightEquipped = false;
    private bool flashlightOn = false;
    private bool flashlightClippedToBelt = false;

    public event System.Action<bool> OnFlashlightAcquired;
    public event System.Action<bool> OnFlashlightTurnedOn;

    #region Properties
    [SerializeField]
    public bool FlashlightEquipped
    {
        get { return flashlightEquipped; }
        set
        {
            //Debug.Log("Flashlight equipped: " + flashlightEquipped);
            flashlightInHand.SetActive(flashlightEquipped);
            //Debug.Log("Flashlight object parented to player active: " + flashlightObjectParentedToPlayer.activeSelf);
        }
    }
    // bool to check wether or not the player is looking at the flashlight in the scene,
    // if true the player can pick up the flashlight and use it.
    public bool LookingAtFlashlight
    {
        get { return lookingAtFlashlight; }
        set { lookingAtFlashlight = value; }
    }
    // property for the flashlight in the scene,
    //if true the flashlight will be destroyed in scene. 
    // the player instead will use the flashlight parented to ZeroGPlayer
    public bool HasFlashlight
    {
        get
        {
            return hasFlashlight;
        }
        set { if (hasFlashlight == value) return;
            hasFlashlight = value;
            OnFlashlightAcquired?.Invoke(hasFlashlight);
        }
    }

    #endregion

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        flashlightEquipped = false;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void EquipFlashlightFromScene()
    {
        Debug.Log("equip flashlight called");
        if (hasFlashlight
            && !flashlightEquipped)
        {
            flashlightEquipped = true;   // enables player flashlight
            flashlightInHand.SetActive(flashlightEquipped);
            //set the default of the flashlight of the lights of the player flashlight from the scene flashlight
            //Debug.Log("Toggling flashlight " + flashlightOn);
            foreach (Light light in flashlightInHand.GetComponentsInChildren<Light>())
            {
                light.enabled = flashlightOn;
            }
            Debug.Log("Equipping flashlight from scene|| HasFlashlightInScene: " + hasFlashlight + " FlashlightEquipped: " + FlashlightEquipped);
            lookingAtFlashlight = false;
        }
    }

    public void ToggleFlashlightFromInventory(InputAction.CallbackContext context)
    {
        //if the player has picked up the flashlight and performs key click of 1
        if (hasFlashlight && context.performed)
        {
            //toggle the flashlight equipped bool
            flashlightEquipped = !flashlightEquipped;
            //set the visibility of he flashlight based on it
            flashlightInHand.SetActive(flashlightEquipped);
            //if the player currently has equipped the flashlight and its currently on
            if (flashlightOn)
            {
                flashlightClippedToBelt = !flashlightEquipped;
                //set the config joint flashlight true (clipped to belt)
                flashlightOutHand.SetActive(flashlightClippedToBelt);
                flashlightInHand.SetActive(!flashlightClippedToBelt);
            }
        }
        //Debug.Log("Equipping flashlight from inventory|| HasFlashlightInScene: " + HasFlashlightInScene + " FlashlightEquipped: " + FlashlightEquipped);

    }

    public void ToggleFlashlight(InputAction.CallbackContext context)
    {
        if (hasFlashlight && flashlightEquipped == true && context.performed)
        {
            flashlightOn = !flashlightOn;
            //Debug.Log("Toggling flashlight" + flashlightOn);
            foreach (Light light in flashlightInHand.GetComponentsInChildren<Light>())
            {
                Debug.Log("light found");
                light.enabled = flashlightOn;
            }
        }
    }
}

