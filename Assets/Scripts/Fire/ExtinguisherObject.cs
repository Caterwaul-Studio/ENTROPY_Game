using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class ExtinguisherObject : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject extinguisherObject;
    [SerializeField] private Transform holdPos;
    [SerializeField] private PersistantManager persistantManager;
    [SerializeField] private InventoryManager inventoryManager;

    public float remainingRetardant = 15f;

    [SerializeField] private bool isGrabbable;
    [SerializeField] private bool canGrab;

    // input action reference for the interact key to pickup
    [SerializeField] private InputActionReference interactActionReference;

    //IInteractable components
    [Header("IInteractable Components")]
    [SerializeField] private Sprite promptIcon;
    public bool IsAvailableForInteraction => isGrabbable;
    public bool HideCrosshairOnLook => false;
    public Sprite PromptIcon => promptIcon;
    public Color PromptColor => Color.white;
    public Transform BillboardParent => null;
    public string PromptText => "take fire extinguisher";
    public void OnLookEnter() => canGrab = true;
    public void OnLookExit() => canGrab = false;

    [Header("Persistence")]
    //must have unique ids for exxtinguishers to validate them being stored in save data
    [SerializeField] private string extinguisherID;
    public string ExtinguisherID => extinguisherID;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(extinguisherID))
        {
            // auto assign a unique id for this specific fire extinguisher
            extinguisherID = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    public bool CanGrab => canGrab;

    private void OnEnable()
    {
        if (interactActionReference)
        {
            interactActionReference.action.performed += OnInteract;
        }
    }

    private void OnDisable()
    {
        if (interactActionReference)
        {
            interactActionReference.action.performed -= OnInteract;
        }
    }

    public void Awake()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalParent = transform.parent;
        canGrab = false;
        isGrabbable = true;
    }

    void Start()
    {
        if(inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
            if (inventoryManager.fireExtinguisher == null)
            {
                inventoryManager.fireExtinguisher = FindFirstObjectByType<FireExtinguisher>();
            }
        }
        
        if(persistantManager == null)
        {
            persistantManager = FindFirstObjectByType<PersistantManager>();
            holdPos = inventoryManager.fireExtinguisher.FireExtinguisherPosition;
        }

        // if the persisted FireExtinguisher already holds an instance with this same ID,
        // this is a stale duplicate spawned by the scene reload - remove it immediately
        var held = inventoryManager.fireExtinguisher.extinguisherGameObj;
        if (GlobalSaveManager.SavedWithTerminal &&
            inventoryManager.fireExtinguisher.HasExtinguisher && 
            held != null && held != this.gameObject)
        {
            var heldScript = held.GetComponent<ExtinguisherObject>();
            if (heldScript != null && heldScript.ExtinguisherID == extinguisherID)
            {
                //Debug.Log($"[ExtinguisherObject] {name} is a stale scene duplicate of already-held id {extinguisherID}, destroying.");
                Destroy(held);
                return;
            }
        }
    }

    void Update()
    {
        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
            if (inventoryManager.fireExtinguisher == null)
            {
                inventoryManager.fireExtinguisher = FindFirstObjectByType<FireExtinguisher>();
            }
        }
        if (persistantManager == null)
        {
            persistantManager = FindFirstObjectByType<PersistantManager>();
            holdPos = inventoryManager.fireExtinguisher.FireExtinguisherPosition;
        }
    }

    //private void HandleFireExtinguisherAcquired(bool acquired, GameObject acquiredObj)
    //{
    //    if (acquired && acquiredObj == this.gameObject)
    //    {
    //        //Debug.Log("extinguisher acquired for first time");
    //        extinguisherObject.GetComponent<Rigidbody>().isKinematic = true;
    //        this.transform.SetParent(fireExtinguisher.transform);
    //        this.transform.position = holdPos.transform.position;
    //        this.transform.rotation = holdPos.transform.rotation;
    //        extinguisherObject.transform.position = holdPos.transform.position;
    //        extinguisherObject.transform.rotation = holdPos.transform.rotation;;
    //        fireExtinguisher.extinguisherGameObj = this.gameObject;
    //        fireExtinguisher.ExtinguisherEquipped = true;
    //    }
    //}

    public void PickupExtinguisher()
    {
        persistantManager.PlayerObject.GetComponent<PlayerUIManager>().ForceDetachBillboard();

        if (inventoryManager.fireExtinguisher.HasExtinguisher && inventoryManager.fireExtinguisher.extinguisherGameObj == this.gameObject)
            return;
        //Debug.Log($"[ExtinguisherObject] PickupExtinguisher on {name}[{GetInstanceID()}] id={extinguisherID}, remainingRetardant BEFORE clone={remainingRetardant}");
        GameObject clone = Instantiate(gameObject, holdPos.position, holdPos.rotation);
        ExtinguisherObject cloneScript = clone.GetComponent<ExtinguisherObject>();
        //Debug.Log($"[ExtinguisherObject] clone[{clone.GetInstanceID()}] remainingRetardant AFTER Instantiate={cloneScript.remainingRetardant}");
        GameObject cloneExtObj = clone.GetComponentInChildren<Rigidbody>()?.gameObject ?? clone;

        //Debug.Log("Picking up extinguisher");
        cloneExtObj.GetComponent<Rigidbody>().isKinematic = true;
        clone.transform.SetParent(inventoryManager.fireExtinguisher.transform);
        clone.transform.position = holdPos.transform.position;
        clone.transform.rotation = holdPos.transform.rotation;
        cloneExtObj.transform.position = holdPos.transform.position;
        cloneExtObj.transform.rotation = holdPos.transform.rotation;

        inventoryManager.fireExtinguisher.extinguisherGameObj = clone;
        cloneExtObj.GetComponent<BoxCollider>().enabled = false;
        inventoryManager.SetChildrenToHoldLayer(cloneExtObj);

        inventoryManager.fireExtinguisher.ExtinguisherEquipped = true;
        inventoryManager.fireExtinguisher.HasExtinguisher = true;

        cloneScript.isGrabbable = false;
        cloneScript.canGrab = false;
        //Debug.Log(inventoryManager.fireExtinguisher.extinguisherGameObj);

        inventoryManager.fireExtinguisher.AcquireExtinguisher(clone); // raises OnFireExtinguisherAcquired
        inventoryManager.fireExtinguisher.inventoryManager.RequestActivate((int)inventoryManager.fireExtinguisher.slotIndex);
        if (inventoryManager.ShowIndicators)
            inventoryManager.persistant.PlayerUIManager.ToggleThrowIndicatorVisible(true);
        Destroy(gameObject);
    }

    public void DropExtinguisher()
    {
        if (inventoryManager.fireExtinguisher.extinguisherGameObj != this.gameObject)
            return;

        //Debug.Log("Dropping extinguisher" + this.gameObject);
        inventoryManager.SetChildrenToDefaultLayer(extinguisherObject, inventoryManager.IInteractableLayer);
        extinguisherObject.GetComponent<Rigidbody>().isKinematic = false;
        this.transform.SetParent(inventoryManager.fireExtinguisher.ExtinguisherContainer.transform);
        this.gameObject.SetActive(true);
        inventoryManager.fireExtinguisher.ExtinguisherEquipped = false;
        inventoryManager.fireExtinguisher.extinguisherGameObj = null;
        inventoryManager.fireExtinguisher.HasExtinguisher = false;
        extinguisherObject.GetComponent<BoxCollider>().enabled = true;

        extinguisherObject.GetComponent<Rigidbody>().AddForce(persistantManager.MainCamera.transform.forward.normalized * 
        persistantManager.Player.RB.linearVelocity.magnitude * 1.1f, ForceMode.VelocityChange);

        isGrabbable = true;
        canGrab = false;
    }

    public void ForceReturnToOriginalState()
    {
        //Debug.Log("Force-returning extinguisher to original state: " + this.gameObject);

        inventoryManager.SetChildrenToDefaultLayer(extinguisherObject, inventoryManager.IInteractableLayer);

        Rigidbody rb = extinguisherObject.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        this.transform.SetParent(originalParent, true);
        this.transform.position = originalPosition;
        this.transform.rotation = originalRotation;
        this.gameObject.SetActive(true);

        BoxCollider col = extinguisherObject.GetComponent<BoxCollider>();
        if (col != null) col.enabled = true;

        //Destroy(extinguisherObject.transform.parent.gameObject);

        if (inventoryManager.fireExtinguisher.extinguisherGameObj == this.gameObject)
        {
            inventoryManager.fireExtinguisher.ExtinguisherEquipped = false;
            inventoryManager.fireExtinguisher.extinguisherGameObj = null;
            inventoryManager.fireExtinguisher.HasExtinguisher = false;
        }

        isGrabbable = true;
        canGrab = false;
    }


    private void OnInteract(InputAction.CallbackContext context)
    {
        if( !inventoryManager.pauseMenu.activeSelf && !inventoryManager.deathMenu.activeSelf
            && !inventoryManager.persistant.Player.InCutscene
            && !inventoryManager.persistant.WristMonitor.IsActive)
        {
            if (canGrab && isGrabbable && !inventoryManager.fireExtinguisher.HasExtinguisher)
            {
                PickupExtinguisher();
                return;
            }
            else if (!canGrab && !isGrabbable && inventoryManager.fireExtinguisher.HasExtinguisher
                && inventoryManager.pickupScript.current == null
                && inventoryManager.fireExtinguisher.TutorialComplete)
            {
                DropExtinguisher();
                return;
            }
            else if (canGrab && isGrabbable && inventoryManager.fireExtinguisher.HasExtinguisher)
            {
                //Debug.Log("Swapping extinguisher");
                if (this.gameObject.GetComponentInChildren<InteractableProxy>().PromptText != "take fire extinguisher")
                {
                    return;
                }

                //Debug.Log("Swapping extinguisher");
                inventoryManager.fireExtinguisher.SwapExtinguisher(this.gameObject);
            }
        }   
    }
}
