using UnityEngine;

public class HeldFloatingObject : MonoBehaviour
{
    public bool objInHand;
    //still need a Event for first grab tutorial

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (objInHand)
        {
            ToggleFloatingObjInv();
        }
    }

    public void ParentFloatingObjectToInvSlot(GameObject obj)
    {
        //obj.transform.SetParent(transform, true);
        //set to true so we run the floating in inv loop
        objInHand = true;
    }

    public void RemoveFloatingObjectFromInvSlot(GameObject obj)
    {
        objInHand = false;
    }

    private void ToggleFloatingObjInv()
    {
        Debug.Log("toggling floating obj from inventory");
    }

}
