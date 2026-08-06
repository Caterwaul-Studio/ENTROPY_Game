using System.Collections;
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

    [SerializeField] private GameObject toggleHeldObjCanvas;
    [SerializeField] private CanvasGroup toggleHeldObjCanvasGroup;

    [SerializeField] private GameObject throwHeldObjCanvas;
    [SerializeField] private CanvasGroup throwHeldObjCanvasGroup;

    [SerializeField] private bool heldObjToggledInv;
    [SerializeField] private bool heldObjThrown;
    [SerializeField] private bool tutorialStarted;
    [SerializeField] private bool tutorialComplete = false;
    public bool TutorialComplete => tutorialComplete;

    public event System.Action<bool> OnHeldObjAcquired;
    public event System.Action<bool> OnHeldObjInvToggled;
    public event System.Action<bool> OnHeldObjThrown;
    public void Start()
    {
        //Debug.Log($"HeldFloatingObject.Start: registering slot {slotIndex}, inventoryManager null? {inventoryManager == null}");
        inventoryManager.RegisterSlot(slotIndex, this);

        if (objInInv == false && inventoryManager.persistant.PlayerUIManager.InputIndicatorThrow.sprite != null)
        {
            inventoryManager.persistant.PlayerUIManager.ToggleThrowIndicatorVisible(false);
        }

        if(heldObj != null && !tutorialComplete)
        {
            SubscribeToHeldObjEvents();
        }
    }

    public void Update()
    {
        if(floatingObjectContainer == null)
        {
            floatingObjectContainer = inventoryManager.pickupScript.ObjectContainer.transform;
        }

        // this gate unequips the held obj if the player is in a cutscene or wrist monitor is opened
        if(heldObj != null)
        {
            if (inventoryManager.persistant.Player.InCutscene
            || inventoryManager.persistant.WristMonitor.IsActive)
            {
                if (objInInv && objInHand)
                {
                    //Debug.Log("toggling floating obj from inventory");
                    inventoryManager.DeactivateCurrent();
                    inventoryManager.persistant.PlayerUIManager.ToggleThrowIndicatorVisible(false);
                }
            }
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

        if (!tutorialComplete)
        {
            UnsubscribeFromHeldObjEvents();
            SubscribeToHeldObjEvents();
        }
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

        if(!inventoryManager.pauseMenu.activeSelf && !inventoryManager.deathMenu.activeSelf
            && !inventoryManager.persistant.Player.InCutscene
            && !inventoryManager.persistant.WristMonitor.IsActive)
        {
            if (objInInv && objInHand)
            {
                //Debug.Log("toggling floating obj from inventory");
                inventoryManager.DeactivateCurrent();
                inventoryManager.persistant.PlayerUIManager.ToggleThrowIndicatorVisible(false);
            }
            else if (objInInv && !objInHand)
            {
                //Debug.Log("toggling floating obj from inventory");
                inventoryManager.RequestActivate((int)slotIndex);
                inventoryManager.persistant.PlayerUIManager.ToggleThrowIndicatorVisible(true);
            }
        }
        if (!tutorialComplete)
            OnHeldObjInvToggled?.Invoke(true);
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

    private void SubscribeToHeldObjEvents()
    {
        OnHeldObjAcquired += HandleHeldObjAcquired;
        OnHeldObjInvToggled += HandleHeldObjToggledInv;
        OnHeldObjThrown += HandleHeldObjThrown;
    }

    private void UnsubscribeFromHeldObjEvents()
    {
        OnHeldObjAcquired -= HandleHeldObjAcquired;
        OnHeldObjInvToggled -= HandleHeldObjToggledInv;
        OnHeldObjThrown -= HandleHeldObjThrown;
    }

    public void RaiseHeldObjAcquired(bool value)
    {
        OnHeldObjAcquired?.Invoke(value);
    }

    public void RaiseHeldObjThrown(bool value)
    {
        OnHeldObjThrown?.Invoke(value);
    }

    private void HandleHeldObjAcquired(bool acquired)
    {
        if(acquired && !tutorialComplete && !tutorialStarted)
        {
            tutorialStarted = true;
            StartCoroutine(HeldObjTutorial());
        }
    }

    private void HandleHeldObjToggledInv(bool toggled)
    {
        heldObjToggledInv = true;
    }

    private void HandleHeldObjThrown(bool thrown)
    {
        heldObjThrown = true;
    }

    public IEnumerator HeldObjTutorial()
    {
        inventoryManager.InTutorial = true;

        inventoryManager.tutorialCanvases.FadeIn(
           inventoryManager.toggleHeldObjectCanvasPrefab,
           ref toggleHeldObjCanvas, ref toggleHeldObjCanvasGroup,
           inventoryManager.tutorialCanvases.tutorialCanvasesPos);

        heldObjToggledInv = false;
        yield return new WaitUntil(() => heldObjToggledInv);

        inventoryManager.tutorialCanvases.FadeOut(
            toggleHeldObjCanvas, toggleHeldObjCanvasGroup, () =>
            {
                toggleHeldObjCanvas = null;
                toggleHeldObjCanvasGroup = null;
            });

        yield return new WaitForSeconds(1f);

        inventoryManager.tutorialCanvases.FadeIn(
            inventoryManager.throwHeldObjectCanvasPrefab,
            ref throwHeldObjCanvas, ref throwHeldObjCanvasGroup,
            inventoryManager.tutorialCanvases.tutorialCanvasesPos);

        heldObjThrown = false;
        yield return new WaitUntil(() => heldObjThrown);

        inventoryManager.tutorialCanvases.FadeOut(
            throwHeldObjCanvas, throwHeldObjCanvasGroup, () =>
            {
                throwHeldObjCanvas = null;
                throwHeldObjCanvasGroup = null;
            });

        tutorialComplete = true;
        inventoryManager.InTutorial = !tutorialComplete;
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

            inventoryManager.persistant.PlayerUIManager.ToggleThrowIndicatorVisible(false);

            heldObj = null;
            objInHand = false;
            objInInv = false;
            floatingObjectContainer = null;
        }   
    }
    #endregion
}
