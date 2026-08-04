using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.InputSystem;

public class Flashlight : MonoBehaviour, IInventoryItem, ISaveableInventoryItem
{
    [Header("Flashlight Variables")]
    public int slotIndex = 1;
    public InventoryManager inventoryManager;

    public float intensityMax = 1500f;
    public float intensityMin = 5f;

    public float spotRangeMin = .5f;
    public float spotRangeMax = 30f;

    [SerializeField]
    private LayerMask barrierLayer; //set layer for barriers

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

    public bool lookingAtFlashlight = false;
    [SerializeField]
    private bool hasFlashlight = false;

    [SerializeField]
    GameObject flashlightInHand;
    [SerializeField]
    private GameObject flashlightOutHand;
    [SerializeField]
    private GameObject flashlightOutHandPrefab;
    private string flashlightOutHandObjectName = "FlashlightOffhand(Clone)";
    [SerializeField]
    private GameObject outHandPos;

    [SerializeField]
    private bool flashlightEquipped = false;
    private bool flashlightOn = false;
    private bool flashlightClippedToBelt = false;

    public event System.Action<bool> OnFlashlightAcquired;
    public event System.Action<bool> OnFlashlightTurnedOn;

    Ray ray;

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
        inventoryManager.RegisterSlot((int)slotIndex, this);
    }

    // Update is called once per frame
    void Update()
    {
        //this determines if the value scaling should be running on the flashlight in hand or out of hand
        if(flashlightInHand && flashlightInHand.activeSelf && flashlightOn)
        {
            ScaleFlashlightValues(flashlightInHand);
        }
        else if (flashlightOutHand != null && flashlightOutHand.activeSelf && flashlightOn)
        {
            ScaleFlashlightValues(flashlightOutHand);
        }
    }

    /// <summary>
    /// This method is created to dynamically scale the flashlight variables to feel more realistic at shorter range.
    /// </summary>
    /// <param name="flashlight"></param>
    public void ScaleFlashlightValues(GameObject flashlight)
    {
        //get the distance to the nearest object infront of the flashlight
        float distance = RayCastDistance(flashlight);

        foreach (Light light in flashlight.GetComponentsInChildren<Light>())
        {
            //if it's a spotlight
            if(light.type == UnityEngine.LightType.Spot)
            {
                //Debug.Log("scaling light intensity");
                //set the intensity to a ratio scaled by the distance
                light.intensity = intensityMax / spotRangeMax * distance;
            }
        }
    }

    /// <summary>
    /// This method is a helper function for ScaleFlashlightValues
    /// This creates a ray cast out from the flashlight bulb to find the distance between it and the nearest wall. 
    /// If the wall is beyond the spotlight range, it returns the spotlight range
    /// </summary>
    /// <param name="flashlight"></param>
    /// <returns></returns>
    public float RayCastDistance(GameObject flashlight)
    {
        //performa simple single ray cast to establish the flashlight's distance to the closest object in front of it
        RaycastHit hit;

        //create the ray
        ray = new Ray(flashlight.transform.position, flashlight.transform.up);

        //create the raycast sending its info to hit, and with a max range of the max range of the spotlight
        if(Physics.Raycast(ray, out hit, spotRangeMax, barrierLayer))
        {
            //Debug.Log("ray distance: " + hit.distance);
           // Debug.Log("hit tag" + hit.rigidbody);
            if (hit.distance <= spotRangeMin)
            {

                return spotRangeMin;
            }
            else
            {
                //Debug.Log("ray distance: " + hit.distance);
                return hit.distance;
            }
        }
        //the raycast goes beyond the max range
        else
        {
            Debug.Log("Ray hit nothing, walls are out of range");
            return spotRangeMax;
        }


    }

    public void EquipFlashlightFromScene()
    {
        //Debug.Log("equip flashlight called");
        if (hasFlashlight
            && !flashlightEquipped)
        {
            inventoryManager.RequestActivate(slotIndex);
            inventoryManager.playerUIManager.ToggleThrowIndicatorVisible(true);
        }
    }

    public void ToggleFlashlightFromInventory(InputAction.CallbackContext context)
    {
        //if the player has picked up the flashlight and performs key click of 1
        if (hasFlashlight && context.performed)
        {
            if(flashlightEquipped)
            {
                inventoryManager.DeactivateCurrent();
                inventoryManager.playerUIManager.ToggleThrowIndicatorVisible(false);
            }
            else
            {
                inventoryManager.RequestActivate((int)slotIndex);
                inventoryManager.playerUIManager.ToggleThrowIndicatorVisible(true);
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

    public void Equip()
    {
        flashlightEquipped = true;
        flashlightInHand.SetActive(true);
        inventoryManager.SetChildrenToHoldLayer(flashlightInHand);

        foreach (Light light in flashlightInHand.GetComponentsInChildren<Light>())
        {
            light.enabled = flashlightOn;
        }

        if(flashlightOutHand != null)
        {
            Destroy(flashlightOutHand);
        }
        flashlightOutHand = null;
        flashlightClippedToBelt = false;
    }

    public void Unequip()
    {
        flashlightEquipped = false;
        flashlightInHand.SetActive(false);
        if (flashlightOn)
        {
            flashlightClippedToBelt = true;
            //set the config joint flashlight true (clipped to belt)
            if (flashlightClippedToBelt)
            {
                //instantiate a new outhand flashlight
                GameObject outHand = Instantiate(flashlightOutHandPrefab, outHandPos.transform.position, outHandPos.transform.rotation);
                //set the parent to the flashlight inventory slot
                outHand.transform.SetParent(outHandPos.transform, true);
                inventoryManager.SetChildrenToHoldLayer(outHand);
                //set config joint connected body
                //find the rigid body of this gameobject
                Rigidbody connectedBody = outHandPos.GetComponent<Rigidbody>();
                ConfigurableJoint joint = outHand.GetComponent<ConfigurableJoint>();
                joint.connectedBody = connectedBody;
                //set the new outhand to the outhand flashlight object
                flashlightOutHand = outHand;
                //set the up to the outHandPos up
                flashlightOutHand.transform.up = outHandPos.transform.up;
                //finally set it true now that everything is set
                flashlightOutHand.SetActive(true);

                RayCastDistance(flashlightInHand);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawRay(ray);
    }

    #region ISaveableInventoryItem
    public class FlashlightSaveData
    {
        public bool hasFlashlight;
    }

    public string GetSaveData()
    {
        return JsonUtility.ToJson(new FlashlightSaveData { hasFlashlight = hasFlashlight });
    }

    public void LoadSaveData(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        var data = JsonUtility.FromJson<FlashlightSaveData>(json);
        HasFlashlight = data.hasFlashlight;
    }

    public void ClearRuntimeState()
    {
        if (!hasFlashlight)
        {
            flashlightEquipped = false;
            if (flashlightInHand != null) flashlightInHand.SetActive(false);
        }
    }

    #endregion
}

