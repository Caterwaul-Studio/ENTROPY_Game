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
    private GameObject ObjectContainer;
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
    private float throwForce = 5f; //force at which the object is thrown at
    [SerializeField]
    private float pickUpRange = 1.3f; //how far the player can pickup the object from
    public GameObject heldObj; //object which we pick up
    private Rigidbody heldObjRb; //rigidbody of object we pick up
    private Collider heldObjCollider;

    [SerializeField]
    private Collider playerCollider;
    public GameObject current;

    [SerializeField] private PlayerAudio playerAudio;

    [SerializeField] private GameObject pauseMenu;

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

        inventoryManager.heldFloatingObject.inventoryManager.RegisterSlot((int)inventoryManager.heldFloatingObject.slotIndex, inventoryManager.heldFloatingObject);
    }


    // Update is called once per frame
    private void Update()
    {
        if (pauseMenu == null)
            pauseMenu = GameObject.Find("PauseMenu");
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
     

        if (!context.performed) return;

        if (canPickUp && heldObj == null)
        {
            PickUpObject(current);
            inventoryManager.heldFloatingObject.ParentFloatingObjectToInvSlot(current);
            inventoryManager.heldFloatingObject.inventoryManager.RequestActivate((int)inventoryManager.heldFloatingObject.slotIndex);
            //Debug.Log("Picked up object");
        }
        else if (canPickUp && heldObj != null && !inventoryManager.fireExtinguisher.ExtinguisherInRaycast && !inventoryManager.flashlight.LookingAtFlashlight)
        {
            inventoryManager.heldFloatingObject.SwapFloatingObjectsInInv(heldObj, current, ObjectContainer);
            DropObject();
            PickUpObject(current);
            inventoryManager.heldFloatingObject.inventoryManager.RequestActivate((int)inventoryManager.heldFloatingObject.slotIndex);
        }
        else if (heldObj != null && !inventoryManager.fireExtinguisher.ExtinguisherInRaycast && !inventoryManager.flashlight.LookingAtFlashlight)
        {
            //Debug.Log("Dropped object");
            inventoryManager.heldFloatingObject.RemoveFloatingObjectFromInvSlot(heldObj, ObjectContainer);
            DropObject();
            inventoryManager.heldFloatingObject.inventoryManager.DeactivateCurrent();
        }

    }

    public void OnThrow(InputAction.CallbackContext context)
    {
        if (heldObj != null && inventoryManager.heldFloatingObject.objInHand && !pauseMenu.activeSelf) //if player is holding object
        {
            if (heldObj.GetComponent<FloatingImpactAudio>()) //only do this if the object has an audio source
            {
                //unmute the thrown object
                StartCoroutine(heldObj.GetComponent<FloatingImpactAudio>().unmuteAfterTime());
            }
            inventoryManager.heldFloatingObject.RemoveFloatingObjectFromInvSlot(heldObj, ObjectContainer);
            MoveObject(); //keep object position at holdPos
            ThrowObject();
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
            inventoryManager.SetChildrenToHoldLayer(heldObj); //set all children of the held object to the hold layer

            //make sure object doesnt collide with player, it can cause weird bugs
            Physics.IgnoreCollision(heldObj.GetComponent<Collider>(), playerCollider, true);
            //heldObj.GetComponent<Collider>().enabled = false;

            //easy fix for playtest, ensure good later
            //>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
            if (uiManager.InputIndicatorThrow.sprite == null)
            {
                uiManager.InputIndicatorThrow.sprite = uiManager.LeftClickIndicator; 
                uiManager.InputIndicatorThrow.color = new Color(1, 1, 1, 1);
                //uiManager.InputIndicatorThrow.transform.position = zeroGPlayer.cam.WorldToScreenPoint(holdPos.GetChild(0).transform.position);
            }

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

        inventoryManager.SetChildrenToDefaultLayer(heldObj, inventoryManager.FloatingObjLayer); //set all children of the held object to the default layer

        heldObjCollider.enabled = true;
        heldObjRb.isKinematic = false;
        heldObj.SetActive(true);
        heldObjRb.AddForce(cam.transform.forward.normalized * zeroGPlayer.RB.linearVelocity.magnitude, ForceMode.VelocityChange);
        heldObj.transform.parent = ObjectContainer.transform; //unparent object
        heldObj = null; //undefine game object

        //easy fix for playtest, ensure good later
        //>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        if (uiManager.InputIndicatorThrow.sprite != null)
        {
            uiManager.InputIndicatorThrow.sprite = null;
            uiManager.InputIndicatorThrow.color = new Color(0, 0, 0, 0);
        }

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
        if (heldObj.GetComponent<ExtinguisherObject>() != null)
        {
            return; //this makes it so the fire extinguisher cant be thrown, necessary because the throw input is used for the fire extinguisher behavior.
        }
        //same as drop function, but add force to object before undefining it
        Physics.IgnoreCollision(heldObj.GetComponent<Collider>(), playerCollider, false);
        //heldObj.GetComponent<Collider>().enabled = true;
        
        inventoryManager.SetChildrenToDefaultLayer(heldObj, inventoryManager.FloatingObjLayer); //set all children of the held object to the default layer

        heldObjCollider.enabled = true;
        heldObjRb.isKinematic = false;
        heldObj.transform.parent = ObjectContainer.transform;
        heldObjRb.AddForce(cam.transform.forward.normalized * throwForce, ForceMode.VelocityChange);

        AudioSource itemSource = heldObj.GetComponentInChildren<AudioSource>();

        if (itemAudioHandler != null)
            itemAudioHandler.PlayThrowSound(inventoryManager.heldFloatingObject.transform.position);

        heldObj = null;
        hasThrownObject = true;
        StartCoroutine(ResetThrowFlag());

        transform.GetComponent<Rigidbody>().AddForce(-cam.transform.forward.normalized * (throwForce * (heldObjRb.mass / transform.GetComponent<Rigidbody>().mass) * 1.5f), ForceMode.VelocityChange);
        //Debug.Log("Thrown at velocity: " + heldObjRb.linearVelocity.magnitude);


        //easy fix for playtest, ensure good later
        //>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        if(uiManager.InputIndicatorThrow.sprite != null)
        {
            uiManager.InputIndicatorThrow.sprite = null;
            uiManager.InputIndicatorThrow.color = new Color(0, 0, 0, 0);
        }

        // initiate pick up cd
        canPickUp = false;
        coolDown = coolDownMax;

        //zeroGPlayer.MoveHandsTo(null, null);
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
