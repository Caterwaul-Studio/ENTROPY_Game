using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFusesTest : MonoBehaviour, IInteractable
{
    //temporary script that updates inventory of player's fuses as they're collected and used
    [SerializeField]
    private Sprite promptIcon;
    [SerializeField] 
    private InputActionReference interactActionReference;
    private bool canGrab = false;

    [SerializeField]
    private GameObject fusebox;
    private FuseboxTest fuseboxScript;

    [SerializeField]
    private Transform billboardParent;

    public void OnLookEnter() { canGrab = true; Debug.Log("Looked at fuse"); }
    public void OnLookExit() { canGrab = false; Debug.Log("Looked away from fuse"); }
    public bool CanGrab
    {
        get { return canGrab; }
        set { canGrab = value; }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fuseboxScript = fusebox.GetComponent<FuseboxTest>();
    }

    // Update is called once per frame
    void Update()
    {
    }
    // is the interactable available for interaction
   public bool IsAvailableForInteraction { get { return true; } }
    // does hovering on this object hide the crosshair
    public bool HideCrosshairOnLook { get { return false; } }
    // the prompt icon for interacting, usually F key indicator sprite
    public Sprite PromptIcon { get { return promptIcon; } }
    // what color is the prompt, usually white
    public Color PromptColor { get { return Color.white; } }
    // what message goes on this interactable
    public string PromptText { get { return "Pick Up Fuse"; } }
    //where does the billboard object appear on screen
    public Transform BillboardParent { get { return billboardParent; } }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (canGrab)
        {
            fuseboxScript.storedFuses++;
            this.gameObject.SetActive(false);
        }
    }
    private void OnInteract()
    {
        if (canGrab)
        {
            fuseboxScript.storedFuses ++;
            this.gameObject.SetActive(false);
        }
    }

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
}
