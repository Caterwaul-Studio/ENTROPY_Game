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
            fireExtinguisher.OnFireExtinguisherAcquired -= HandleFireExtinguisherAcquired;
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

        if(fireExtinguisher != null)
        {
            fireExtinguisher.OnFireExtinguisherAcquired -= HandleFireExtinguisherAcquired;
            fireExtinguisher.OnFireExtinguisherAcquired += HandleFireExtinguisherAcquired;
            fireExtinguisher.OnFireExtinguisherAcquired -= StartFireExtinguisherTutorial;
            fireExtinguisher.OnFireExtinguisherAcquired += StartFireExtinguisherTutorial;
        }
    }

    private void HandleFireExtinguisherAcquired(bool acquired)
    {
        if (acquired)
        {
            Debug.Log("extinguisher acquired");
            extinguisherObject.GetComponent<Rigidbody>().isKinematic = true;
            this.transform.SetParent(fireExtinguisher.transform);
            this.transform.position = holdPos.transform.position;
            this.transform.rotation = holdPos.transform.rotation;
            extinguisherObject.transform.position = holdPos.transform.position;
            extinguisherObject.transform.rotation = holdPos.transform.rotation;
            fireExtinguisher.extinguisherGameObj = this;
        }
    }

    private void StartFireExtinguisherTutorial(bool tutorial)
    {
        //add logic for the tutorial of the fire extinguisher
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (canGrab && isGrabbable && !fireExtinguisher.ExtinguisherEquipped)
        {
            isGrabbable = false;
            canGrab = false;

            fireExtinguisher.HasExtinguisher = true;
            fireExtinguisher.ExtinguisherEquipped = true;
        }
    }
}
