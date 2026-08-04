using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FireExtinguisher : MonoBehaviour, IInventoryItem, ISaveableInventoryItem
{
    public int slotIndex = 2;
    public InventoryManager inventoryManager;
    public bool ExtinguisherInRaycast;

    [SerializeField] private GameObject extinguisherContainer;
    private string extinguisherContainerName = "FireExtinguishers";

    [SerializeField] private bool canPuff = true;
    [SerializeField] private bool hasExtinguisher = true;
    [SerializeField] private List<GameObject> puffObjects;
    [SerializeField] private int puffCycle = 0;
    [SerializeField] private Transform fireExtinguisherPosition;
    [SerializeField] private ParticleSystem sys;
    [SerializeField] private ParticleSystem.MainModule sysMain;
    [SerializeField] private ParticleSystem.EmissionModule sysEmission;
    [SerializeField] private GameObject player;
    [SerializeField] private ZeroGravity zeroGravity;
    private float initialEmission;
    private bool holding;

    [SerializeField] private bool tutorialComplete = false;

    public bool extinguisherEquipped = false;
    public GameObject extinguisherGameObj;

    public event System.Action<bool, GameObject> OnFireExtinguisherAcquired;


    public bool HasExtinguisher
    {
        get { return hasExtinguisher; }
        set { hasExtinguisher = value; }
    }

    public bool ExtinguisherEquipped
    {
        get { return extinguisherEquipped; }
        set { extinguisherEquipped = value; }
    }

    public bool TutorialComplete
    {
        get { return tutorialComplete; }
        set { tutorialComplete = value; }
    }

    public GameObject ExtinguisherContainer
    {
        get { return extinguisherContainer; }
    }

    public Transform FireExtinguisherPosition
    {
        get { return fireExtinguisherPosition; }
    }

    private void OnEnable()
    {
        sysMain = sys.main;
        sysEmission = sys.emission;
    }

    private void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<ZeroGravity>().gameObject;
            zeroGravity = player.GetComponent<ZeroGravity>();
        }
        if (extinguisherContainer == null)
        {
            extinguisherContainer = GameObject.Find(extinguisherContainerName);
        }
        initialEmission = sysEmission.rateOverTimeMultiplier;
        sysEmission.rateOverTime = 0;

        inventoryManager.RegisterSlot((int)slotIndex, this);

        if (hasExtinguisher == false && inventoryManager.playerUIManager.InputIndicatorThrow.sprite != null)
        {
            inventoryManager.playerUIManager.ToggleThrowIndicatorVisible(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (holding && canPuff && hasExtinguisher && !inventoryManager.pauseMenu.activeSelf && !inventoryManager.deathMenu.activeSelf && !zeroGravity.IsDead)
            StartCoroutine(MakePuff());

        if (extinguisherContainer == null)
        {
            extinguisherContainer = GameObject.Find(extinguisherContainerName);
        }

        //if (!zeroGravity.IsDead)
        //    if (pickupScript.current != null)
        //        if (pickupScript.current.GetComponent<ExtinguisherObject>() != null && !pickupScript.CanPickUp && (pauseMenu == null || !pauseMenu.activeInHierarchy))
        //            if (pickupScript.current.GetComponent<ExtinguisherObject>().remainingRetardant > 0)
        //                hasExtinguisher = true;
        //            else
        //                hasExtinguisher = false;
        //        else
        //            hasExtinguisher = false;
        //    else
        //        hasExtinguisher = false;
        //else
        //    hasExtinguisher = false;


        //this logic creates a gate bool to ensure the floating objects are not dropped when picking up a new extinguisher
        ExtinguisherInRaycast = false;

        if (extinguisherContainer != null)
        {
            foreach (ExtinguisherObject extinguisherObj in extinguisherContainer.GetComponentsInChildren<ExtinguisherObject>())
            {
                if (extinguisherObj.CanGrab)
                {
                    ExtinguisherInRaycast = extinguisherObj.CanGrab;
                    break;
                }
            }
        }  
    }
    System.Collections.IEnumerator MakePuff()
    { //instead of creating nodes, just moving around existing nodes is used again in the hopes it will help with optimization
        if(this.GetComponentInChildren<ExtinguisherObject>() != null &&
            this.GetComponentInChildren<ExtinguisherObject>().remainingRetardant > 0)
        {
            puffObjects[puffCycle].GetComponent<PuffMovement>().Shoot(fireExtinguisherPosition);
            puffCycle++;
            if (puffCycle >= puffObjects.Count)
                puffCycle = 0;
            canPuff = false;
            this.GetComponentInChildren<ExtinguisherObject>().remainingRetardant -= 0.15f;
            sysEmission.rateOverTimeMultiplier = initialEmission;
            yield return new WaitForSeconds(0.15f);
            sysEmission.rateOverTime = 0;
            canPuff = true;
        }
    }

    public void AcquireExtinguisher(GameObject acquiredObj)
    {
        hasExtinguisher = true;
        extinguisherGameObj = acquiredObj;
        OnFireExtinguisherAcquired?.Invoke(true, acquiredObj);
    }

    public void SwapExtinguisher(GameObject obj2)
    {
        extinguisherGameObj.GetComponent<ExtinguisherObject>().DropExtinguisher();
        obj2.GetComponent<ExtinguisherObject>().PickupExtinguisher();
    }

    public void ToggleExtinguisherFromInventory(InputAction.CallbackContext context)
    {
        if(hasExtinguisher && context.performed && !inventoryManager.pauseMenu.activeSelf && !inventoryManager.deathMenu.activeSelf)
        {
            if (extinguisherEquipped)
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
    }

    public void OnLeftClick(InputAction.CallbackContext context)
    {
        if (hasExtinguisher && !inventoryManager.pauseMenu.activeSelf && !inventoryManager.deathMenu.activeSelf)
        {
            if (context.phase == InputActionPhase.Started)
            {
                holding = true;
                //Debug.Log("Holding started");
            }
            else if (context.phase == InputActionPhase.Canceled)
            {
                holding = false;
                //Debug.Log("Holding ended");
            }
        }
    }

    public void Equip()
    {
        extinguisherEquipped = true;
        if(extinguisherGameObj != null)
        {
            extinguisherGameObj.SetActive(true);
        }
        //inventoryManager.SetChildrenToHoldLayer(extinguisherGameObj);
    }

    public void Unequip()
    {
        extinguisherEquipped = false;
        if (extinguisherGameObj != null)
        {
            extinguisherGameObj.SetActive(false);
        }
    }

    #region ISaveableInventoryItem

    [System.Serializable]
    public class FireExtinguisherSaveData
    {
        public bool hasExtinguisher;
        public bool extinguisherEquipped;
        public string extinguisherID;
        public float remainingRetardent;
    }

    public string GetSaveData()
    {
        string id = null;
        float retardent = 0f;
        if(hasExtinguisher && extinguisherGameObj != null)
        {
            var obj = extinguisherGameObj.GetComponentInChildren<ExtinguisherObject>();
            if (obj != null)
            {
                id = obj.ExtinguisherID;
                retardent = obj.remainingRetardant;
            }
        }

        var data = new FireExtinguisherSaveData 
        { 
            hasExtinguisher = hasExtinguisher,
            extinguisherEquipped = extinguisherEquipped,
            extinguisherID = id,
            remainingRetardent = retardent,
        };
        return JsonUtility.ToJson(data);
    }

    public void LoadSaveData(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        var data = JsonUtility.FromJson<FireExtinguisherSaveData>(json);

        // only a permanent/terminal save should let the extinguisher persist across a scene reload
        bool eligibleForRestore = data.hasExtinguisher && GlobalSaveManager.SavedWithTerminal;

        ReleaseIfMismatched(eligibleForRestore, eligibleForRestore ? data.extinguisherID : null);

        if (hasExtinguisher) return;

        if (eligibleForRestore && !string.IsNullOrEmpty(data.extinguisherID) && extinguisherContainer != null)
        {
            bool found = false;
            foreach(var obj in ExtinguisherContainer.GetComponentsInChildren<ExtinguisherObject>())
            {
                if(obj.ExtinguisherID == data.extinguisherID)
                {
                    obj.PickupExtinguisher();

                    var heldObj = extinguisherGameObj != null
                    ? extinguisherGameObj.GetComponent<ExtinguisherObject>()
                    : null;


                    if (heldObj != null)
                    {
                        heldObj.remainingRetardant = data.remainingRetardent;
                        inventoryManager.fireExtinguisher.extinguisherEquipped = data.extinguisherEquipped;
                        inventoryManager.fireExtinguisher.extinguisherGameObj.SetActive(data.extinguisherEquipped);
                        inventoryManager.playerUIManager.ToggleThrowIndicatorVisible(data.extinguisherEquipped);
                        Debug.Log($"Restored remainingRetardant={data.remainingRetardent} on clone {heldObj.name}");
                    }
                    else
                    {
                        Debug.LogWarning("Could not find clone to restore remainingRetardant onto.");
                    }

                    found = true;
                    return;
                }
                Debug.LogWarning($"FireExtinguisher: saved extinguisherId '{data.extinguisherID}' not found in container on load.");
            }

            if(!found)
            {
                Debug.LogWarning($"FireExtinguisher: saved extinguisherId '{data.extinguisherID}' not found in container on load.");
            }
        }
    }

    public void ClearRuntimeState()
    {
        ReleaseIfMismatched(shouldHaveExtinguisher: false, savedID: null);
    }

    private void ReleaseIfMismatched(bool shouldHaveExtinguisher, string savedID)
    {
        ExtinguisherObject currentlyHeld = (extinguisherGameObj != null)
            ? extinguisherGameObj.GetComponent<ExtinguisherObject>()
            : null;

        bool currentMatchesSaved = currentlyHeld != null
            && shouldHaveExtinguisher
            && currentlyHeld.ExtinguisherID == savedID;

        if (currentMatchesSaved)
        {
            hasExtinguisher = true;
            return; // already correctly held, nothing to release
        }

        if (currentlyHeld != null)
        {
            Debug.Log($"[FireExtinguisher] Discarding leftover held extinguisher (id={currentlyHeld.ExtinguisherID}) — not eligible for retention on this reload.");
            Destroy(currentlyHeld.gameObject); // it's a runtime clone with no valid "original" scene position, so destroy rather than ForceReturn
        }

        hasExtinguisher = false;
        extinguisherGameObj = null;

        if (inventoryManager != null)
        {
            inventoryManager.ReleaseSlotIfActive(this);
        }
    }
    #endregion
}
