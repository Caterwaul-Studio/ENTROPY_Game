using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class LockdownEvent : MonoBehaviour, IInteractable
{
    [SerializeField]
    private GameObject playerObject;
    private ZeroGravity player;

    [SerializeField]
    private BoxCollider DoorTrigger;
    [SerializeField]
    private BoxCollider MusicTrigger;
    [SerializeField]
    private BoxCollider cuttingOutTrigger;

    //[SerializeField]
    //private GameObject lever;
    [SerializeField]
    private GameObject wrist;
    [SerializeField]
    private DoorScript[] doorsToOpen;
    [SerializeField]
    private DoorScript[] doorsToLock;

    [SerializeField]
    private DoorScript medDoor;
    [SerializeField]
    private DoorScript brokenDoor;
    [SerializeField]
    private DoorScript bodyDoor;
    [SerializeField]
    private DoorScript escapeDoor;

    private DialogueManager dialogueManager;

    public HazardLight[] hazardsToDisable;
    public AmbientController ambientController;

    [SerializeField]
    private GameObject auxLightObj;
    [SerializeField]
    private Light auxLight;

    [SerializeField]
    private Material leverMaterial;

    // lockdown bools
    private bool leverPulled;
    private bool lockdownDeactivated;
    private bool isActive;
    private bool canPull;
    private bool isComplete;

    // wrist monitor
    private bool canGrab;
    private bool isGrabbable;
    [SerializeField]
    private WristMonitor wristMonitor;

    public GameplayBeatAudio audioManager;
    public AnimationCurve powerDownCurve;
    public AnimationCurve glitchCurve;
    public Light[] lights;
    public Light[] softLights;
    public Color endColor;
    public Color endButtonColor;
    public Color endEmissionColor;

    private bool glitchLights = false;
    private bool poweringDown = false;
    public float powerDownDuration = 1f;
    public float glitchDuration = 1f; // How long one cycle of the curve takes
    public float intensityMultiplier = 1f;

    private float powerDownElapsedTime = 0f;
    private float glitchElapsedTime = 0f;

    [SerializeField]
    private GameObject grateToMove;
    [SerializeField]
    private GameObject grateMoveLocation;

    private Vector3 gratePos;
    private Vector3 grateMovePos;


    [SerializeField] private ServerProgressBars serverProgress;
    //[SerializeField] private Terminal serverTerminal;
    [SerializeField] private LockdownPanel lockdownPanel;

    [SerializeField]
    private MeshRenderer[] emissiveMeshes;
    [SerializeField]
    private Material[] serverEmissives;
    private Color[] initialEmissionColor;

    [SerializeField] private GameObject BarsGroup;
    [SerializeField] private MeshRenderer TerminalBars;
    private Material[] barsMaterials;
    private Color initBarEmissive;

    [SerializeField] private MeshRenderer[] lightMeshes = new MeshRenderer[12];
    private Material[] lightMaterials = new Material[12];
    [SerializeField] private Color initLightEmissiveColor;
    [SerializeField] private float initLightEmissiveMultiplier;
    [SerializeField] private Color endLightEmissiveColor;
    [SerializeField] private float endLightEmissiveMultiplier;

    public GameObject alienBody;
    public Animator alienAnimator;
    public Light alienLight;
    public Light alienSpotLight;

    public Transform panelMovePos;

    [SerializeField] private InputActionReference interactActionReference;

    //IInteractable components
    [Header("IInteractable Components")]
    [SerializeField] private Sprite promptIcon;
    public bool IsAvailableForInteraction => !lockdownDeactivated;
    public bool HideCrosshairOnLook => false;
    public Sprite PromptIcon => promptIcon;
    public Color PromptColor => Color.white;
    public Transform BillboardParent => null;
    public string PromptText => isActive ? "deactivate manual lockdown" : "initiate lever release";
    public void OnLookEnter() => canPull = true;
    public void OnLookExit() => canPull = false;

    public bool LeverPulled
    {
        get { return leverPulled; }
    }
    public bool CanPull
    {
        get { return canPull; }
        set { canPull = value; }
    }

    public bool IsActive
    {
        get { return isActive; }
    }

    public bool IsComplete
    {
        get { return isComplete; }
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(grateToMove != null)
        {
            gratePos = grateToMove.transform.position;
        }

        if (grateMoveLocation != null)
        {
            grateMovePos = grateMoveLocation.transform.position;
        }

        playerObject = PersistantManager.Instance.PlayerObject;
        player = playerObject.GetComponent<ZeroGravity>();
        wristMonitor = PersistantManager.Instance.WristMonitor;

        DoorTrigger.enabled = false;
        //MusicTrigger.enabled = false;
        cuttingOutTrigger.enabled = false;
        // checks if player is currently hovering over lever
        canPull = false;
        // checks if the first sequence is complete
        leverPulled = false;
        // checks if system is able to be turned on
        isActive = false;
        // all player action is completed for this sequence.
        isComplete = false;

        canGrab = false;
        isGrabbable = true;

        dialogueManager = FindFirstObjectByType<DialogueManager>();
        ambientController = FindFirstObjectByType<AmbientController>();


        // Renderer rend = serverObj.GetComponent<Renderer>();
        //serverMaterialInstance = rend.materials[1]; // this clones the material at runtime

        // manual setup for getting material references (annoying)
        serverEmissives = new Material[3];
       
        serverEmissives[0] = emissiveMeshes[0].materials[1]; // Server material
        serverEmissives[1] = emissiveMeshes[0].materials[2]; // Wire Material
        serverEmissives[2] = emissiveMeshes[1].materials[2]; // Floor material

        initialEmissionColor = new Color[serverEmissives.Length];
        for (int i = 0; i < serverEmissives.Length; i++)
        {
            initialEmissionColor[i] = serverEmissives[i].GetColor("_EmissionColor");
        }


        GetBarMaterials();
        GetLightMaterials();
    }

    // Update is called once per frame
    void Update()
    {
        if(poweringDown)
        {
            powerDownElapsedTime += Time.deltaTime;
            float t = powerDownElapsedTime / powerDownDuration;

            // Only apply intensity if still within duration
            if (t <= 1f)
            {
                float curveValue = powerDownCurve.Evaluate(t);
                foreach (Light lightSource in lights)
                {
                    lightSource.intensity = curveValue * intensityMultiplier;
                }

            }
            else
            {
                // Glitch complete, turn off or reset as needed
                poweringDown = false;
            }
        }

        if(glitchLights)
        {

            glitchElapsedTime += Time.deltaTime;
            float t = glitchElapsedTime / glitchDuration;

            // Only apply intensity if still within duration
            if (t <= 1f)
            {
                float curveValue = glitchCurve.Evaluate(t);
                foreach(Light lightSource in lights)
                {
                    lightSource.intensity = curveValue * intensityMultiplier;
                }
                
            }
            else
            {
                // Glitch complete, turn off or reset as needed
                glitchLights = false;

                // Updates wrist monitor objective 
                wristMonitor.CompleteObjective();
            }
        }
    }

    public void LockDoors()
    {
        foreach(DoorScript door in doorsToLock)
        {
            door.SetState(DoorScript.States.Locked);
        }
    }

    public void OpenDoors()
    {
        
        foreach (DoorScript door in doorsToOpen)
        {
            StartCoroutine(OpenDoorWithDelay(door));
        }

        //deadBody.AddTorque(deadBody.transform.right * 15);
        //deadBody.AddForce(new Vector3(0, -1, 0) * 30);

        //StartCoroutine(WaitForBodyVisible());

        
    }

    //adding slight delay to door to prevent phasing.
    private IEnumerator OpenDoorWithDelay(DoorScript door)
    {
        float randomDelay = Random.Range(0f, 0.2f); // Adjust range if needed
        yield return new WaitForSeconds(randomDelay);
        door.SetState(DoorScript.States.Open);
    }

    private IEnumerator WaitForBodyVisible()
    {
        yield return new WaitForSeconds(2f);
        //audioManager.playBodyStinger();

    }

    public void OnInteract(InputAction.CallbackContext context)
    {

        if (context.performed)
        {
            //Debug.Log(isActive);
            //Handle lockdown lever
            if (canPull && isActive && !isComplete)
            {
                audioManager.PlayButtonClick();
                StartCoroutine(LerpPosition(panelMovePos.position, 1f));
 
                // open the broken door first
                //brokenDoor.SetState(DoorScript.States.Open);

                DoorTrigger.enabled = true;
                //isActive = false;
                isComplete = true;

                // begin lighting and audio queues
                player.PlayerCutSceneHandler(true);
                StartCoroutine(PlayLockdownFX());

            }
            else if (canPull && !isActive)
            {
                // lever must be pulled first
                StartCoroutine(ActivateLever());
                audioManager.playLeverSFX();
            }
        }
        
    }
    private IEnumerator ActivateLever()
    {
        leverPulled = true;
        yield return StartCoroutine(lockdownPanel.PlayLeverAnimation());

        lockdownPanel.SwitchToDeactivate();

        //genuinely idk if this sequence is used??? it doesnt play so is it just a empty line idk???
        //dialogueManager.StartDialogueSequence(9, 0.5f);

        // I need isactive to be true when dialogue concludes
        yield return new WaitForSeconds(1f);
        isActive = true;
    }

    private IEnumerator PlayLockdownFX()
    {
        //Wait for button Press
        yield return new WaitForSeconds(0.5f);

        //Lock the Entrance Door;
        StartCoroutine(LockServerEntrance());
        MusicTrigger.enabled = true;

        foreach(HazardLight hazard in hazardsToDisable)
        {
            hazard.IsHazard = false;
        }

        bodyDoor.SetState(DoorScript.States.BrokenShort);
        LockDoors();

        audioManager.FadeServers(false);
        audioManager.playPowerCut();
        serverProgress.Shutdown();

        for (int i = 0; i < serverEmissives.Length; i++)
        {
            StartCoroutine(FadeEmission(serverEmissives, initialEmissionColor[i], initialEmissionColor[i], 4f, 0, 6.5f, 0f));
        }

        StartCoroutine(FadeEmission(barsMaterials, initBarEmissive, initBarEmissive, 1f, -10, 8f, 0f));

        StartCoroutine(FadeEmission(lightMaterials, initLightEmissiveColor, endLightEmissiveColor, initLightEmissiveMultiplier, -10, 6.5f, 0f));

        poweringDown = true;
        
        yield return new WaitForSeconds(13f);

        //audioManager.playAlienRunAway();
        StartCoroutine(PlayAlienAnimation());

        yield return new WaitForSeconds(26f);

        audioManager.playPowerOn();
        serverProgress.Reboot();

        for (int i = 0; i < serverEmissives.Length; i++)
        {
            StartCoroutine(FadeEmission(serverEmissives, initialEmissionColor[i], endEmissionColor, 0, 4f, 4f, 2f));
        }

        StartCoroutine(FadeEmission(barsMaterials, initBarEmissive, initBarEmissive, -10, 1, 8.5f, 0f));
        StartCoroutine(FadeEmission(lightMaterials, endLightEmissiveColor, endLightEmissiveColor, -10, endLightEmissiveMultiplier, 6.5f, 0f));

        glitchLights = true;
        foreach(Light light in lights)
        {
            StartCoroutine(FadeLightColor(light, light.color, endColor, 5f));
        }
        foreach (Light lightSource in softLights)
        {
            StartCoroutine(FadeLightIntensity(lightSource, 0.6f, 5f));
        }

        //adjust intensity to be lower onreboot
        intensityMultiplier = 0.2f;

        yield return new WaitUntil(() => !glitchLights);
        foreach(Light lightSource in lights)
        {
            lightSource.intensity = intensityMultiplier;
        }
        
        yield return new WaitForSeconds(4f);
        player.PlayerCutSceneHandler(false);
        StartCoroutine(MoveDoor(gratePos, grateMovePos, 1f, null));
        
        //Open doors in the doors to open array, this is the dining hall to facilities door.

        OpenDoors();
        escapeDoor.SetState(DoorScript.States.Closed);
        audioManager.FadeServers(true);
        ambientController.Progress();
        
    }

    private IEnumerator LockServerEntrance()
    {
        brokenDoor.SetState(DoorScript.States.Closed);

        yield return null;

        // needs to be updated with new auxiliary lights to function base on emission color
        //auxLightObj.GetComponent<Renderer>().material = leverMaterial;
        //auxLight.color = endButtonColor;

        //brokenDoor.SetState(DoorScript.States.Open);
    }

    public IEnumerator FadeLightColor(Light lightSource, Color fromColor, Color toColor, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            lightSource.color = Color.Lerp(fromColor, toColor, t);
            yield return null;
        }

        lightSource.color = toColor;
    }

    public IEnumerator FadeLightIntensity(Light lightSource, float targetIntensity, float duration)
    {
        float startIntensity = lightSource.intensity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            lightSource.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            yield return null;
        }

        lightSource.intensity = targetIntensity;
    }

    // Coroutine to fade the CanvasGroup over time
    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha)
    {
        float timeElapsed = 0f;
        float fadeDuration = 1f;

        while (timeElapsed < fadeDuration)
        {
            // Lerp alpha from start to end
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timeElapsed / fadeDuration);
            timeElapsed += Time.deltaTime;
            yield return null; // Wait until the next frame
        }

        canvasGroup.alpha = endAlpha; // Ensure it's set to the final alpha
    }

    public IEnumerator FadeEmission(Material[] materials, Color fromColor, Color toColor, float fromIntensity, float toIntensity, float duration, float delayTime)
    {
        yield return new WaitForSeconds(delayTime);


        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;

            // Interpolate color and intensity separately
            Color currentColor = Color.Lerp(fromColor, toColor, t);
            float currentIntensity = Mathf.Lerp(fromIntensity, toIntensity, t);

            // Apply combined color * intensity as emission
            foreach (Material mat in materials)
            {
                mat.SetColor("_EmissionColor", currentColor * currentIntensity);
            }


            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Final value at end of fade
        foreach (Material mat in materials)
        {
            mat.SetColor("_EmissionColor", toColor * toIntensity);
        }
    }



    private IEnumerator MoveDoor(Vector3 fromPos, Vector3 toPos, float duration, System.Action onComplete)
    {
        audioManager.PlayMoveGrate();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = 0.5f * Mathf.Sin((elapsed / duration) * Mathf.PI - Mathf.PI / 2f) + 0.5f;
            grateToMove.transform.position = Vector3.Lerp(fromPos, toPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        grateToMove.transform.position = toPos;
        onComplete?.Invoke();
    }



    private IEnumerator PlayAlienAnimation()
    {
        alienBody.SetActive(true);

        StartCoroutine(FadeAlienLight());

        alienAnimator.SetTrigger("PlayLockdown");

        audioManager.playAlienRunAway();

        yield return new WaitForSeconds(29f);

        
        alienBody.SetActive(false);
        alienAnimator.SetTrigger("ReturnToIdle");
    }

    IEnumerator FadeAlienLight()
    {
        float duration = 2f;
        float elapsed = 0f;

        // Fade from 0 to 10
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            alienLight.intensity = Mathf.Lerp(0f, 30f, elapsed / (duration / 2));
            alienSpotLight.intensity = Mathf.Lerp(0f, 30f, elapsed / (duration / 2));
            yield return null;
        }

        elapsed = 0f;

        // Fade from 10 back to 0
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            alienLight.intensity = Mathf.Lerp(30f, 0f, elapsed / (duration / 2));
            alienSpotLight.intensity = Mathf.Lerp(30f, 0f, elapsed / (duration / 2));
            yield return null;
        }

        // Ensure final value is exactly 0
        alienLight.intensity = 0f;
        alienSpotLight.intensity = 0f;
    }

    public void TerminalActivated()
    {
        //change the lockdown panel and make it interactable
        lockdownPanel.SwitchToDeactivate();
        isActive = true;
        
    }

    /// <summary>
    /// Method for moving the player to the panel position;
    /// </summary>
    /// <param name="destination"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    private IEnumerator LerpPosition(Vector3 destination, float duration)
    {
        lockdownDeactivated = true;
        Vector3 start = player.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            player.transform.position = Vector3.Lerp(start, destination, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        player.transform.position = destination;
    }

    private void GetBarMaterials()
    {
        MeshRenderer[] barMeshes = BarsGroup.GetComponentsInChildren<MeshRenderer>();

        barsMaterials = new Material[barMeshes.Length + 1];


        for (int i = 0; i < barMeshes.Length; i++)
        {
            barsMaterials[i] = barMeshes[i].material;
        }

        barsMaterials[barsMaterials.Length-1] = TerminalBars.materials[0];

        initBarEmissive = barsMaterials[0].GetColor("_EmissionColor");
    }

    private void GetLightMaterials()
    {
        for (int i = 0; i < lightMeshes.Length; i++)
        {
            lightMaterials[i] = lightMeshes[i].material;
        }

    }

}
