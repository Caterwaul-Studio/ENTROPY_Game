using UnityEngine;
using UnityEngine.InputSystem;

public class HeldFloatingObject : MonoBehaviour, IInventoryItem
{
    public bool objInHand;
    public bool objInInv;
    public GameObject heldObj;
    public InventoryManager inventoryManager;
    public int slotIndex = 3;
    //still need a Event for first grab tutorial

    public void ParentFloatingObjectToInvSlot(GameObject obj)
    {
        obj.transform.SetParent(transform, true);
        heldObj = obj;
        //set to true so we run the floating in inv loop
        objInHand = true;
        objInInv = true;
    }

    public void RemoveFloatingObjectFromInvSlot(GameObject obj, GameObject containerObj)
    {
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
        Debug.Log("toggling floating obj from inventory");
        if(!context.performed) return;

        if (heldObj == null) return;

        if(objInHand)
        {
            inventoryManager.DeactivateCurrent();
        }
        else
        {
            inventoryManager.RequestActivate((int)slotIndex);
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

}
