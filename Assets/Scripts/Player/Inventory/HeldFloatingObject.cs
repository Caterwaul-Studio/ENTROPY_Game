using UnityEngine;
using UnityEngine.InputSystem;

public class HeldFloatingObject : MonoBehaviour, IInventoryItem, ISaveableInventoryItem
{
    public bool objInHand;
    public bool objInInv;
    public GameObject heldObj;
    public InventoryManager inventoryManager;
    public int slotIndex = 3;
    //still need a Event for first grab tutorial

    public Transform floatingObjectContainer;
    public Vector3 originalPosition;

    public void Start()
    {
        //Debug.Log($"HeldFloatingObject.Start: registering slot {slotIndex}, inventoryManager null? {inventoryManager == null}");
        inventoryManager.RegisterSlot(slotIndex, this);
    }

    public void Update()
    {
        if(floatingObjectContainer == null)
        {
            floatingObjectContainer = inventoryManager.pickupScript.ObjectContainer.transform;
        }
    }

    public void ParentFloatingObjectToInvSlot(GameObject obj)
    {
        Debug.Log("Parenting floating object to player");
        originalPosition = obj.transform.position;
        obj.transform.SetParent(transform, true);
        heldObj = obj;
        inventoryManager.SetChildrenToHoldLayer(heldObj); //set all children of the held object to the hold layer
        //set to true so we run the floating in inv loop
        objInHand = true;
        objInInv = true;
    }

    public void RemoveFloatingObjectFromInvSlot(GameObject obj, GameObject containerObj)
    {
        originalPosition = Vector3.zero;
        inventoryManager.SetChildrenToDefaultLayer(obj, inventoryManager.FloatingObjLayer); //set all children of the held object to the default layer
        obj.transform.SetParent(containerObj.transform, true);
        heldObj = null;
        objInHand = false;
        objInInv = false;
    }

    public void SwapFloatingObjectsInInv(GameObject obj1, GameObject obj2, GameObject containerObj)
    {
        RemoveFloatingObjectFromInvSlot(obj1, containerObj);
        ParentFloatingObjectToInvSlot(obj2);
    }

    public void ToggleFloatingObjInv(InputAction.CallbackContext context)
    {
        if(!context.performed) return;

        if (heldObj == null) return;

        if(objInInv && objInHand)
        {
            //Debug.Log("toggling floating obj from inventory");
            inventoryManager.DeactivateCurrent();
            inventoryManager.playerUIManager.ToggleThrowIndicatorVisible(false);
        }
        else if(objInInv && !objInHand)
        {
            //Debug.Log("toggling floating obj from inventory");
            inventoryManager.RequestActivate((int)slotIndex);
            inventoryManager.playerUIManager.ToggleThrowIndicatorVisible(true);
        }
    }

    public void Equip()
    {
        if (heldObj == null) return;
        heldObj.SetActive(true);
        objInHand = true;
    }

    public void Unequip()
    {
        if (heldObj == null) return;
        heldObj.SetActive(false);
        objInHand = false;
    }

    #region ISaveableInventoryItem
    [System.Serializable]
    public class HeldfloatingObjectSaveData
    {
        public bool objInInv;
        public string heldObjName;
    }

    public string GetSaveData()
    {
        var data = new HeldfloatingObjectSaveData
        {
            objInInv = objInInv,
            heldObjName = (objInInv && heldObj != null) ? heldObj.name : null
        };
        return JsonUtility.ToJson(data);
    }

    public void LoadSaveData(string json)
    {
        Debug.Log("loading held obj save data");
        ClearRuntimeState();
    }

    public void ClearRuntimeState()
    {
        if(heldObj == null) return;

        if (!GlobalSaveManager.SavedWithTerminal)
        {
            Debug.Log("Clearing held obj");
            heldObj.transform.position = originalPosition;
            Physics.IgnoreCollision(heldObj.GetComponent<Collider>(), inventoryManager.pickupScript.PlayerCollider, false);
            inventoryManager.SetChildrenToDefaultLayer(heldObj, inventoryManager.FloatingObjLayer);
            heldObj.GetComponent<Rigidbody>().isKinematic = false;
            heldObj.GetComponent<Collider>().enabled = true;
            heldObj.transform.SetParent(floatingObjectContainer, true);
            heldObj.SetActive(true);

            inventoryManager.pickupScript.ClearHeldReference();

            inventoryManager.playerUIManager.ToggleThrowIndicatorVisible(false);

            heldObj = null;
            objInHand = false;
            objInInv = false;
            floatingObjectContainer = null;
        }   
    }
    #endregion
}
