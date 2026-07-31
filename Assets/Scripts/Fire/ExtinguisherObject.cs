using UnityEngine;
using UnityEngine.InputSystem;

public class ExtinguisherObject : MonoBehaviour, IInteractable
{
    [SerializeField] private FireExtinguisher fireExtinguisher;
    [SerializeField] private GameObject extinguisherObject;
    [SerializeField] private Transform holdPos;
    [SerializeField] private PersistantManager persistantManager;

    public float remainingRetardant = 15f;

    [SerializeField] private bool isGrabbable;
    [SerializeField] private bool canGrab;

    [SerializeField] private GameObject extinguisherContainer;
    private string extinguisherContainerName = "FireExtinguishers";

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
        if(extinguisherContainer == null)
        {
            extinguisherContainer = GameObject.Find(extinguisherContainerName);
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
        if (extinguisherContainer == null)
        {
            extinguisherContainer = GameObject.Find(extinguisherContainerName);
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

    private void PickupExtinguisher()
    {
        extinguisherObject.GetComponent<Rigidbody>().isKinematic = true;
        this.transform.SetParent(fireExtinguisher.transform);
        this.transform.position = holdPos.transform.position;
        this.transform.rotation = holdPos.transform.rotation;
        extinguisherObject.transform.position = holdPos.transform.position;
        extinguisherObject.transform.rotation = holdPos.transform.rotation; ;
        fireExtinguisher.extinguisherGameObj = this.gameObject;
        fireExtinguisher.ExtinguisherEquipped = true;
        fireExtinguisher.HasExtinguisher = true;

        fireExtinguisher.AcquireExtinguisher(this.gameObject); // raises OnFireExtinguisherAcquired
    }

    private void DropExtinguisher()
    {
        Debug.Log("Dropping extinguisher");
        extinguisherObject.GetComponent<Rigidbody>().isKinematic = false;
        this.transform.SetParent(extinguisherContainer.transform);
        fireExtinguisher.ExtinguisherEquipped = false;
        fireExtinguisher.extinguisherGameObj = null;
        fireExtinguisher.HasExtinguisher = false;
    }

    private void StartFireExtinguisherTutorial(bool acquired, GameObject acquiredObj)
    {
        if (acquired && acquiredObj == this.gameObject && !fireExtinguisher.TutorialComplete)
        {
            //add logic for the tutorial of the fire extinguisher
            Debug.Log("starting tutorial");
            fireExtinguisher.TutorialComplete = true;
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (canGrab && isGrabbable && !fireExtinguisher.ExtinguisherEquipped)
        {
            isGrabbable = false;
            canGrab = false;
            PickupExtinguisher();
        }
        else if (!isGrabbable && fireExtinguisher.ExtinguisherEquipped)
        {
            isGrabbable = true;
            canGrab = false;
            DropExtinguisher();
        }

    }
}
