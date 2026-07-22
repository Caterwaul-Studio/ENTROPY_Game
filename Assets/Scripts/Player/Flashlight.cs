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
    GameObject flashlightObjectParentedToPlayer;
    [SerializeField]
    GameObject flashlightObjectInScene;

    [SerializeField]
    private bool flashlightEquipped = false;
    private bool flashlightOn = false;

    public event System.Action<bool> OnFlashlightAcquired;
    public event System.Action<bool> OnFlashlightTurnedOn;

    #region Properties
    [SerializeField]
    public bool FlashlightEquipped
    {
        get { return flashlightEquipped; }
        set
        {
            flashlightEquipped = value;
            //Debug.Log("Flashlight equipped: " + flashlightEquipped);
            flashlightObjectParentedToPlayer.SetActive(flashlightEquipped);
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
            HasFlashlight = value;
            OnFlashlightAcquired?.Invoke(HasFlashlight);
        }
    }

    #endregion

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void EquipFlashlightFromScene(InputAction.CallbackContext context)
    {
        if (lookingAtFlashlight
            && !hasFlashlight
            && !flashlightEquipped
            && context.performed)
        {
            //go through the lights of the scene object, match what ever their status is with the flashlightOn bool
            foreach (Light light in flashlightObjectInScene.GetComponentsInChildren<Light>())
            {
                flashlightOn = light.enabled;
                //Debug.Log("Light: " + flashlightOn);
            }
            hasFlashlight = true; // disables scene flashlight
            flashlightEquipped = true;   // enables player flashlight
            //set the default of the flashlight of the lights of the player flashlight from the scene flashlight
            //Debug.Log("Toggling flashlight " + flashlightOn);
            foreach (Light light in flashlightObjectParentedToPlayer.GetComponentsInChildren<Light>())
            {
                light.enabled = flashlightOn;
            }
            //Debug.Log("Equipping flashlight from scene|| HasFlashlightInScene: " + HasFlashlightInScene + " FlashlightEquipped: " + FlashlightEquipped);
            lookingAtFlashlight = false;
        }
    }

    public void ToggleFlashlightFromInventory(InputAction.CallbackContext context)
    {
        if (hasFlashlight && context.performed)
        {
            flashlightEquipped = !flashlightEquipped;

            if (flashlightEquipped == true)
            {
                flashlightOn = false;
                foreach (Light light in flashlightObjectParentedToPlayer.GetComponentsInChildren<Light>())
                {
                    //ensure the flashlight is turned off when equipping it from the inventory
                    light.enabled = flashlightOn;
                }
            }
        }
        //Debug.Log("Equipping flashlight from inventory|| HasFlashlightInScene: " + HasFlashlightInScene + " FlashlightEquipped: " + FlashlightEquipped);

    }

    public void ToggleFlashlight(InputAction.CallbackContext context)
    {
        if (hasFlashlight && flashlightEquipped && context.performed)
        {
            flashlightOn = !flashlightOn;
            //Debug.Log("Toggling flashlight" + flashlightOn);
            foreach (Light light in flashlightObjectParentedToPlayer.GetComponentsInChildren<Light>())
            {
                light.enabled = flashlightOn;
            }
        }
    }
}

