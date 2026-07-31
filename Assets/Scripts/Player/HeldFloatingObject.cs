using UnityEngine;
using UnityEngine.InputSystem;

public class HeldFloatingObject : MonoBehaviour
{
    public bool objInHand;
    public bool objInInv;
    public GameObject heldObj;
    //still need a Event for first grab tutorial

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

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
        if (context.performed && heldObj != null)
        {
            heldObj.SetActive(!objInHand);
            objInHand = !objInHand;
        }
    }

}
