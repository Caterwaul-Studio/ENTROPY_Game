using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;
using UnityEngine.SceneManagement;

public class PickupScript : MonoBehaviour
{
    [SerializeField]
    private InventoryManager inventoryManager;

    [SerializeField]
    private GameObject player;
    [SerializeField]
    ZeroGravity zeroGPlayer;
    [SerializeField]
    PlayerUIManager uiManager;
    //[SerializeField]
    //private Transform holdPos;
    [SerializeField]
    private Camera cam;

    [SerializeField]
    public GameObject ObjectContainer;
    //string to help find the object container in the scene, if it is not assigned in the inspector
    private string objectContainerName = "FloatingObjects";

    [SerializeField]
    private bool canPickUp = false;
    [SerializeField]
    private float coolDownMax = 3;
    [SerializeField]
    private float coolDown;


    [SerializeField]
    private LayerMask objectLayer;
    [SerializeField]
    private float pickUpRange = 1.3f; //how far the player can pickup the object from
    public GameObject heldObj; //object which we pick up
    private Rigidbody heldObjRb; //rigidbody of object we pick up
    private Collider heldObjCollider;

    [SerializeField]
    private Collider playerCollider;
    public GameObject current;

    [Header ("Throw Charge")]
    [SerializeField] private float minThrowForce = 3f; //force at which the object is thrown at
    [SerializeField] private float maxThrowForce = 15f; //force at which the object is thrown at
    [SerializeField] private float maxChargeTime = 3f; //time it takes to reach max throw force

    private bool isChargingThrow = false;
    private float chargeStartTime;
    private float currentThrowForce;

    public float ChargeRatio => isChargingThrow
        ? Mathf.Clamp01((Time.time - chargeStartTime) / maxChargeTime)
        : 0f;

    [SerializeField] private PlayerAudio playerAudio;

    [Header("Audio")]
    public ItemAudioHandler itemAudioHandler;

    private bool hasThrownObject = false; //for tutorial section for detecting throwing

    private Color indicatorColor = new Color(1f, 1f, 1f, 0.5f);
    private Color emptyColor = new Color(0, 0, 0, 0f);

    public float PickUpRange
    {
        get { return pickUpRange; }
    }

    public LayerMask ObjectLayer
    {
        get { return objectLayer; } 
    }

    public bool HasThrownObject
    {
        get { return hasThrownObject; }
        set { hasThrownObject = value;  }
    }

    public bool CanPickUp
    {
        get { return canPickUp; }
        set { canPickUp = value; }
    }

    public GameObject HeldObject
    {
        get { return heldObj; }
    }

    public Collider PlayerCollider => playerCollider;


    // onenable and ondisable called on scene load
    private void OnEnable() { 
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    // Start is called before the first frame update
    private void Start()
    {
         coolDown = 0;

        if(ObjectContainer == null)
        {
            ObjectContainer = GameObject.Find(objectContainerName);
        }
    }


    // Update is called once per frame
    private void Update()
    {
        if (zeroGPlayer.CanGrab) //if currently not holding anything and is allowed to grab things
        {
            //perform raycast to check if player is looking at object within pickuprange
            RaycastHit hit;
            float sphereRadius = 0.3f; // adjust for how wide you want the grab to be
            if (Physics.SphereCast(cam.transform.position, sphereRadius, cam.transform.forward, out hit, pickUpRange))
            {
                GameObject hitObj = hit.collider.gameObject;
                if (hitObj.layer == LayerMask.NameToLayer("FloatingObject"))
                {
                    current = hitObj;
                    canPickUp = true;
                    uiManager.interactBillboardObjectInScene = true;
                    uiManager.ShowBillboardUI(uiManager.KeyFIndicator, hitObj.transform);
                }
                else
                {
                    current = null;
                    canPickUp = false;

                    uiManager.HideBillboardUI();
                }
            }
            else
            {
                // NEW: Check if something is extremely close to the camera
                Collider[] nearby = Physics.OverlapSphere(cam.transform.position + cam.transform.forward * 0.2f, 0.4f, objectLayer);
                bool found = false;
                foreach (Collider col in nearby)
                {
                    if (col.gameObject.layer == LayerMask.NameToLayer("FloatingObject"))
                    {
                        current = col.gameObject;
                        canPickUp = true;

                        uiManager.ShowBillboardUI(uiManager.KeyFIndicator, col.transform);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    current = null;
                    canPickUp = false;

                    uiManager.HideBillboardUI();
                }
            }
        }
        else
        {
            canPickUp = false;
        }

        //Debug.DrawRay(cam.transform.position, cam.transform.forward * pickUpRange, Color.blue);

        if (coolDown > 0)
        {
            coolDown -= Time.deltaTime;
        }


    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        //if (buttonPressed)
        //{
        //    buttonPressed = false;
        //}
        //else
        //{
        //    buttonPressed = true;
        //}
     

        if (!context.performed || inventoryManager.pauseMenu.activeSelf || inventoryManager.deathMenu.activeSelf) return;

        if (canPickUp && heldObj == null)
        {
            PickUpObject(current);
            inventoryManager.heldFloatingObject.ParentFloatingObjectToInvSlot(current);
            inventoryManager.heldFloatingObject.inventoryManager.RequestActivate((int)inventoryManager.heldFloatingObject.slotIndex);
            //Debug.Log("Picked up object");
        }
        else if (canPickUp && heldObj != null && current != null)
        {
            inventoryManager.heldFloatingObject.SwapFloatingObjectsInInv(heldObj, current, ObjectContainer);
            DropObject();
            PickUpObject(current);
            inventoryManager.heldFloatingObject.inventoryManager.RequestActivate((int)inventoryManager.heldFloatingObject.slotIndex);
        }
        else if (heldObj != null && !inventoryManager.persistant.PlayerUIManager.interactBillboardObjectInScene && inventoryManager.heldFloatingObject.objInHand)
        {
            //Debug.Log("Dropped object");
            inventoryManager.heldFloatingObject.RemoveFloatingObjectFromInvSlot(heldObj, ObjectContainer);
            DropObject();
            inventoryManager.heldFloatingObject.inventoryManager.DeactivateCurrent();
        }
        if (!inventoryManager.heldFloatingObject.TutorialComplete)
            inventoryManager.heldFloatingObject.RaiseHeldObjAcquired(true);
    }

    public void OnThrow(InputAction.CallbackContext context)
    {
        //if (heldObj != null && inventoryManager.heldFloatingObject.objInHand && !pauseMenu.activeSelf) //if player is holding object
        //{
        //    if (heldObj.GetComponent<FloatingImpactAudio>()) //only do this if the object has an audio source
        //    {
        //        //unmute the thrown object
        //        StartCoroutine(heldObj.GetComponent<FloatingImpactAudio>().unmuteAfterTime());
        //    }
        //    inventoryManager.heldFloatingObject.RemoveFloatingObjectFromInvSlot(heldObj, ObjectContainer);
        //    MoveObject(); //keep object position at holdPos
        //    ThrowObject();
        //}

        if (context.started)
        {
            if(heldObj != null && inventoryManager.heldFloatingObject.objInHand && !inventoryManager.pauseMenu.activeSelf && !inventoryManager.deathMenu.activeSelf)
            {
                isChargingThrow = true;
                chargeStartTime = Time.time;
            }
        }
        else if (context.canceled)
        {
            if(!isChargingThrow) return;
            isChargingThrow = false;

            if(heldObj != null && inventoryManager.heldFloatingObject.objInHand && !inventoryManager.pauseMenu.activeSelf && !inventoryManager.deathMenu.activeSelf)
            {
                float heldTime = Mathf.Clamp(Time.time - chargeStartTime, 0f, maxChargeTime);
                float chargeRatio = heldTime / maxChargeTime;
                currentThrowForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargeRatio);

                if(heldObj.GetComponent<FloatingImpactAudio>()) //only do this if the object has an audio source
                {
                    //unmute the thrown object
                    StartCoroutine(heldObj.GetComponent<FloatingImpactAudio>().unmuteAfterTime());
                }
                inventoryManager.heldFloatingObject.RemoveFloatingObjectFromInvSlot(heldObj, ObjectContainer);
                MoveObject();
                ThrowObject();
            }
        }
    }

    void PickUpObject(GameObject pickUpObj)
    {
        if (pickUpObj.GetComponent<AudioSource>()) //only do this if the object has an audio source
        {
            //mute the picked up object's audio source if we are holding it
            pickUpObj.GetComponent<AudioSource>().mute = true;
        }

        if (pickUpObj.GetComponent<Rigidbody>()) //make sure the object has a RigidBody
        {
            canPickUp = false;
            heldObj = pickUpObj; //assign heldObj to the object that was hit by the raycast (no longer == null)
            uiManager.HideBillboardUI();
            heldObjRb = pickUpObj.GetComponent<Rigidbody>(); //assign Rigidbody
            heldObjCollider = pickUpObj.GetComponent<Collider>();
            heldObjCollider.enabled = false;
            heldObjRb.isKinematic = true;
            //heldObjRb.transform.parent = holdPos.transform; //parent object to holdposition

            if (heldObj != null)

            //make sure object doesnt collide with player, it can cause weird bugs
            Physics.IgnoreCollision(heldObj.GetComponent<Collider>(), playerCollider, true);
            //heldObj.GetComponent<Collider>().enabled = false;

            uiManager.ToggleThrowIndicatorVisible(true);

            MoveObject();
            //zeroGPlayer.MoveHandsTo(holdPos.GetChild(0).transform, null);

            AudioSource itemSource = heldObj.GetComponentInChildren<AudioSource>();

            if (itemAudioHandler != null)
                itemAudioHandler.PlayPickUpSound(inventoryManager.heldFloatingObject.transform.position);
        }
    }
    void DropObject()
    {
        //re-enable collision with player
        Physics.IgnoreCollision(heldObj.GetComponent<Collider>(), playerCollider, false);
        //heldObj.GetComponent<Collider>().enabled = true;
        heldObjCollider.enabled = true;
        heldObjRb.isKinematic = false;
        heldObj.SetActive(true);
        heldObjRb.AddForce(cam.transform.forward.normalized * zeroGPlayer.RB.linearVelocity.magnitude, ForceMode.VelocityChange);
        heldObj.transform.parent = ObjectContainer.transform; //unparent object
        heldObj = null; //undefine game object

        uiManager.ToggleThrowIndicatorVisible(false);

        //current = null;
        //zeroGPlayer.MoveHandsTo(null, null);
    }
    void MoveObject()
    {
        //keep object position the same as the holdPosition position
        heldObj.transform.position = inventoryManager.heldFloatingObject.transform.position;
    }

    void ThrowObject()
    {
        if (!inventoryManager.heldFloatingObject.TutorialComplete)
            inventoryManager.heldFloatingObject.RaiseHeldObjThrown(true);

        if (heldObj.GetComponent<ExtinguisherObject>() != null)
        {
            return; //this makes it so the fire extinguisher cant be thrown, necessary because the throw input is used for the fire extinguisher behavior.
        }
        //same as drop function, but add force to object before undefining it
        Physics.IgnoreCollision(heldObj.GetComponent<Collider>(), playerCollider, false);
        //heldObj.GetComponent<Collider>().enabled = true;
        heldObjCollider.enabled = true;
        heldObjRb.isKinematic = false;
        heldObj.transform.parent = ObjectContainer.transform;
        heldObjRb.AddForce(cam.transform.forward.normalized * currentThrowForce, ForceMode.VelocityChange);

        AudioSource itemSource = heldObj.GetComponentInChildren<AudioSource>();

        if (itemAudioHandler != null)
            itemAudioHandler.PlayThrowSound(inventoryManager.heldFloatingObject.transform.position);

        heldObj = null;
        hasThrownObject = true;
        StartCoroutine(ResetThrowFlag());

        transform.GetComponent<Rigidbody>().AddForce(-cam.transform.forward.normalized * (currentThrowForce * (heldObjRb.mass / transform.GetComponent<Rigidbody>().mass) * 1.25f), ForceMode.VelocityChange);
        //Debug.Log("Thrown at velocity: " + heldObjRb.linearVelocity.magnitude);

        uiManager.ToggleThrowIndicatorVisible(false);

        // initiate pick up cd
        canPickUp = false;
        coolDown = coolDownMax;
        //zeroGPlayer.MoveHandsTo(null, null);
    }

    public void ClearHeldReference()
    {
        heldObj = null;
        heldObjRb = null;
        heldObjCollider = null;
        canPickUp = false;
    }

    IEnumerator ResetThrowFlag()
    {
        yield return null; // Wait one frame
        hasThrownObject = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ObjectContainer = GameObject.Find("FloatingObjects");
    }

}
