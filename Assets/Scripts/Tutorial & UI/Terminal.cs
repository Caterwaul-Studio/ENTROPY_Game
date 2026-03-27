using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class Terminal : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TerminalScreen terminalScreenScript;
    [SerializeField] private GameObject terminalScreenObj;
    [SerializeField] private GameObject ALANScreen;
    [SerializeField] private GameObject targetTransform;
    [SerializeField] private TerminalPopup popup;
    [SerializeField] private TerminalDisabled disabled;
    [SerializeField] private TerminalAudioManager terminalAudio;

    [Header("State")]
    public bool isLookedAt = false;
    public bool isActivated = false;
    public bool isUploadComplete = false;
    [SerializeField] private Light screenLight;
    private float lightIntensity;

    //this is we can assign different actions to be called by our terminals
    [Header("Screen Components")]
    [SerializeField] private Material offMaterial;
    [SerializeField] private Material onMaterial;

    public GameObject TerminalScreen { get { return terminalScreenObj; } }
    public GameObject ALANScreenUI { set { ALANScreen = value; } }
    public TerminalPopup MainScriptPopup { set { popup = value; } }
    public Material OnMaterial { set { onMaterial = value; } }
    public GameObject TargetTransform
    {
        get { return targetTransform; }
    }

    //this is how we can assign different actions to be called by our terminals
    //assign a method from a script in the inspector
    [Header("Events")]
    public UnityEvent onUploadComplete;

    private GameObject playerObj;
    private ZeroGravity playerScript;
    public ZeroGravity PlayerScript
    {
        get { return playerScript; }
    }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bootupSource;
    [SerializeField] private AudioSource completeSource;

    private void Start()
    {
        playerScript = FindFirstObjectByType<ZeroGravity>();
        terminalAudio = FindFirstObjectByType<TerminalAudioManager>();

        playerObj = playerScript.gameObject;
        lightIntensity = screenLight.intensity;
        if (isActivated == false)
        {
            disabled.StartFlashing();
        }

    }

    private void Update()
    {
        if (popup != null && popup.IsUploaded && !isUploadComplete)
        {
            isUploadComplete = true;
            playerScript.PlayerCutSceneHandler(false);
            ALANScreen.SetActive(true);

            // Play upload complete sound via TerminalAudioManager
            //terminalAudio?.PlayUploadCompleteSound(completeSource);

            onUploadComplete?.Invoke();
        }
    }

    public void Activation()
    {
        isActivated = true;
        disabled.StopFlashing();
        playerScript.PlayerCutSceneHandler(true);
        //terminalScreenObj.SetActive(true);

        // Play bootup sound via TerminalAudioManager
        terminalAudio?.PlayBootupSound(bootupSource);

        // Start typewriter effect on terminal screen
        terminalScreenScript.StartCoroutine(terminalScreenScript.TypeText());

        // Move player smoothly to the terminal
        StartCoroutine(LerpPosition(targetTransform.transform.position, 0.75f));
    }
    // starts the player at the terminal immediately after upload
    public void MediumActivation()
    {
        isActivated = true;
        disabled.StopFlashing();
        ALANScreen.SetActive(true);
        onUploadComplete?.Invoke();
    }
    // simply sets the terminal to active
    public void SoftActivation()
    {
        isActivated = true;
        disabled.StopFlashing();
        ALANScreen.SetActive(true);
    }
    private IEnumerator LerpPosition(Vector3 destination, float duration)
    {
        Vector3 start = playerObj.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            playerObj.transform.position = Vector3.Lerp(start, destination, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        playerObj.transform.position = destination;
    }

    /// <summary>
    /// Turns the terminal ON � changes material and hides the off screen overlay.
    /// </summary>
    public void TurnOn()
    {
        if (terminalScreenObj != null && onMaterial != null)
        {
            MeshRenderer renderer = terminalScreenObj.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.material = onMaterial;


        }

        screenLight.intensity = lightIntensity;

    }

    /// <summary>
    /// Turns the terminal OFF � changes material and shows the off screen overlay.
    /// </summary>
    public void TurnOff()
    {
        if (terminalScreenObj != null && offMaterial != null)
        {
            MeshRenderer renderer = terminalScreenObj.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.material = offMaterial;
        }

        screenLight.intensity = 0;
    }
}


