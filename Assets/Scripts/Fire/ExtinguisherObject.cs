using UnityEngine;
using UnityEngine.InputSystem;

public class ExtinguisherObject : MonoBehaviour, IInteractable
{
    [SerializeField] private FireExtinguisher fireExtinguisher;
    [SerializeField] private GameObject extinguisherObject;
    [SerializeField] private Transform holdPos;
    [SerializeField] private PersistantManager persistantManager;
    [SerializeField] private PickupScript pickupScript;

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

    private void OnDestroy()
    {
        if (fireExtinguisher != null)
        {
            fireExtinguisher.OnFireExtinguisherAcquired -= StartFireExtinguisherTutorial;
        }
    }

    void Start()
    {
        canGrab = false;
        isGrabbable = true;

        if (fireExtinguisher == null)
        {
            fireExtinguisher = FindFirstObjectByType<FireExtinguisher>();
        }
        if(persistantManager == null)
        {
            persistantManager = FindFirstObjectByType<PersistantManager>();
            holdPos = persistantManager.HoldPos;
        }
        if(pickupScript == null)
        {
            pickupScript = FindFirstObjectByType<PickupScript>();
        }
    }

    void Update()
    {
        if (fireExtinguisher == null)
        {
            fireExtinguisher = FindFirstObjectByType<FireExtinguisher>();
        }
        if (persistantManager == null)
        {
            persistantManager = FindFirstObjectByType<PersistantManager>();
            holdPos = persistantManager.HoldPos;
        }
        if (pickupScript == null)
        {
            pickupScript = FindFirstObjectByType<PickupScript>();
        }

        if (fireExtinguisher != null)
        {
            fireExtinguisher.OnFireExtinguisherAcquired -= StartFireExtinguisherTutorial;
            fireExtinguisher.OnFireExtinguisherAcquired += StartFireExtinguisherTutorial;
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
        if (fireExtinguisher.HasExtinguisher && fireExtinguisher.extinguisherGameObj == this.gameObject)
            return;

        //Debug.Log("Picking up extinguisher");
        extinguisherObject.GetComponent<Rigidbody>().isKinematic = true;
        this.transform.SetParent(fireExtinguisher.transform);
        this.transform.position = holdPos.transform.position;
        this.transform.rotation = holdPos.transform.rotation;
        extinguisherObject.transform.position = holdPos.transform.position;
        extinguisherObject.transform.rotation = holdPos.transform.rotation; 

        fireExtinguisher.extinguisherGameObj = this.gameObject;
        extinguisherObject.GetComponent<BoxCollider>().enabled = false;

        fireExtinguisher.ExtinguisherEquipped = true;
        fireExtinguisher.HasExtinguisher = true;

        isGrabbable = false;
        canGrab = false;

        //Debug.Log(fireExtinguisher.extinguisherGameObj);

        fireExtinguisher.AcquireExtinguisher(this.gameObject); // raises OnFireExtinguisherAcquired
        fireExtinguisher.inventoryManager.RequestActivate((int)fireExtinguisher.slotIndex);
    }

    public void DropExtinguisher()
    {
        if (fireExtinguisher.extinguisherGameObj != this.gameObject)
            return;

        //Debug.Log("Dropping extinguisher" + this.gameObject);
        extinguisherObject.GetComponent<Rigidbody>().isKinematic = false;
        this.transform.SetParent(fireExtinguisher.ExtinguisherContainer.transform);
        this.gameObject.SetActive(true);
        fireExtinguisher.ExtinguisherEquipped = false;
        fireExtinguisher.extinguisherGameObj = null;
        fireExtinguisher.HasExtinguisher = false;
        extinguisherObject.GetComponent<BoxCollider>().enabled = true;

        extinguisherObject.GetComponent<Rigidbody>().AddForce(persistantManager.MainCamera.transform.forward.normalized * 
            persistantManager.Player.RB.linearVelocity.magnitude * 1.1f, ForceMode.VelocityChange);

        isGrabbable = true;
        canGrab = false;
    }

    private void StartFireExtinguisherTutorial(bool acquired, GameObject acquiredObj)
    {
        if (acquired && acquiredObj == this.gameObject && !fireExtinguisher.TutorialComplete)
        {
            //add logic for the tutorial of the fire extinguisher
            //Debug.Log("starting tutorial");
            fireExtinguisher.TutorialComplete = true;
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (canGrab && isGrabbable && !fireExtinguisher.HasExtinguisher)
        {
            PickupExtinguisher();
            return;
        }
        else if (!canGrab && !isGrabbable && fireExtinguisher.HasExtinguisher)
        {
            if(pickupScript != null && pickupScript.current != null)
            {
                return;
            }

            DropExtinguisher();
            return;
        }
        else if (canGrab && isGrabbable && fireExtinguisher.HasExtinguisher )
        {
            //Debug.Log("Swapping extinguisher");
            fireExtinguisher.SwapExtinguisher(this.gameObject);
        }
    }
}
