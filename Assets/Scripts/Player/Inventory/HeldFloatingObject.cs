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

    [SerializeField] public bool heldObjToggledInv;
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

        if (inventoryManager.ShowIndicators && objInInv == false && inventoryManager.persistant.PlayerUIManager.InputIndicatorThrow.sprite != null)
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
                    if(inventoryManager.ShowIndicators)
                        inventoryManager.persistant.PlayerUIManager.ToggleThrowIndicatorVisible(false);
                }
            }
        }
    }

    public void ParentFloatingObjectToInvSlot(GameObject obj)
    {
        //Debug.Log("Parenting floating object to player");
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
                if(inventoryManager.ShowIndicators)
                    inventoryManager.persistant.PlayerUIManager.ToggleThrowIndicatorVisible(false);
            }
            else if (objInInv && !objInHand)
            {
                //Debug.Log("toggling floating obj from inventory");
                inventoryManager.RequestActivate((int)slotIndex);
                if (inventoryManager.ShowIndicators)
                    inventoryManager.persistant.PlayerUIManager.ToggleThrowIndicatorVisible(true);
            }
            if (!tutorialComplete)
                OnHeldObjInvToggled?.Invoke(true);
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

    private void RestartHeldObjTutorial()
    {
        StopAllCoroutines();

        UnsubscribeFromHeldObjEvents();

        if (toggleHeldObjCanvas)
            Destroy(toggleHeldObjCanvas);
        if (throwHeldObjCanvas)
            Destroy(throwHeldObjCanvas);

        toggleHeldObjCanvas = null;
        toggleHeldObjCanvasGroup = null;
        throwHeldObjCanvas = null;
        throwHeldObjCanvasGroup = null;

        heldObjToggledInv = false;
        heldObjThrown = false;
        tutorialStarted = false;

        inventoryManager.InTutorial = false;
    }

    #region ISaveableInventoryItem
    [System.Serializable]
    public class HeldFloatingObjectSaveData
    {
        public bool objInInv;
        public bool objInHand;
        public string heldObjID;
        public bool heldObjTutorialComplete;
    }

    public string GetSaveData()
    {
        string id = null;
        if (objInInv && heldObj != null)
        {
            var floatingObj = heldObj.GetComponent<FloatingObject>();
            if (floatingObj != null)
                id = floatingObj.FloatingObjectID;
        }

        var data = new HeldFloatingObjectSaveData
        {
            objInInv = objInInv,
            objInHand = objInHand,
            heldObjID = id,
            heldObjTutorialComplete = tutorialComplete
        };
        return JsonUtility.ToJson(data);
    }

    public void LoadSaveData(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        var data = JsonUtility.FromJson<HeldFloatingObjectSaveData>(json);

        tutorialComplete = data.heldObjTutorialComplete;

        if (!tutorialComplete)
        {
            RestartHeldObjTutorial();
        }

        bool eligibleForRestore = data.objInInv && GlobalSaveManager.SavedWithTerminal;

        ReleaseIfMismatched(eligibleForRestore, eligibleForRestore ? data.heldObjID : null);

        if (objInInv) return;

        if (eligibleForRestore && !string.IsNullOrEmpty(data.heldObjID))
        {
            GameObject container = inventoryManager.pickupScript.ObjectContainer;
            if (container != null)
            {
                bool found = false;
                foreach (FloatingObject floatingObj in container.GetComponentsInChildren<FloatingObject>(true))
                {
                    if (floatingObj.FloatingObjectID == data.heldObjID)
                    {
                        ParentFloatingObjectToInvSlot(floatingObj.gameObject);
                        inventoryManager.pickupScript.PickUpObject(floatingObj.gameObject);

                        objInHand = data.objInHand;
                        heldObj.SetActive(objInHand);

                        if (inventoryManager.ShowIndicators)
                            inventoryManager.persistant.PlayerUIManager.ToggleThrowIndicatorVisible(objInHand);

                        //Debug.Log($"Restored held floating object '{heldObj.name}', equipped={objInHand}");
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Debug.LogWarning($"HeldFloatingObject: saved object name '{data.heldObjID}' not found in container on load.");
                }
            }
        }
    }

    public void ClearRuntimeState()
    {
        ReleaseIfMismatched(shouldRestore: false, savedID: null); 
    }

    private void ReleaseIfMismatched(bool shouldRestore, string savedID)
    {
        var currentFloatingObj = heldObj != null ? heldObj.GetComponent<FloatingObject>() : null;

        bool currentMatchesSaved = currentFloatingObj != null
        && shouldRestore
        && currentFloatingObj.FloatingObjectID == savedID;

        if (currentMatchesSaved)
        {
            objInInv = true;
            return; // already correctly held, nothing to release
        }

        if (heldObj != null)
        {
            //Debug.Log($"[HeldFloatingObject] Releasing leftover held object '{heldObj.name}' — not eligible for retention on this reload.");

            heldObj.transform.position = originalPosition;
            Physics.IgnoreCollision(heldObj.GetComponent<Collider>(), inventoryManager.pickupScript.PlayerCollider, false);
            inventoryManager.SetChildrenToDefaultLayer(heldObj, inventoryManager.FloatingObjLayer);

            var rb = heldObj.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;

            var col = heldObj.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            Transform targetParent = floatingObjectContainer != null
                ? floatingObjectContainer
                : (inventoryManager.pickupScript.ObjectContainer != null ? inventoryManager.pickupScript.ObjectContainer.transform : null);

            heldObj.transform.SetParent(targetParent, true);
            heldObj.SetActive(true);

            inventoryManager.pickupScript.ClearHeldReference();

            if (inventoryManager.ShowIndicators)
                inventoryManager.persistant.PlayerUIManager.ToggleThrowIndicatorVisible(false);
        }

        heldObj = null;
        objInHand = false;
        objInInv = false;
        floatingObjectContainer = null;

        if (inventoryManager != null)
            inventoryManager.ReleaseSlotIfActive(this);
    }
    #endregion
}
