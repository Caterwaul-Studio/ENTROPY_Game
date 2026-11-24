using JetBrains.Annotations;
using System.Collections;
using UnityEngine;

public class ResearchLabSequence : Event {
    [Header("Checkpoint")]
    [SerializeField] private GameObject researchLabCheckpoint;

    [Header("Research Lab Lights")]
    //Light group
    [SerializeField] private GameObject lightsGroup;
    [SerializeField] private MeshRenderer[] lightMeshes;
    public Light[] lights;
    private Material[] lightMaterials;
    private float[] initLightIntensities;
    private Color[] initLightEmissives;
    public float restoreDuration = 2f;
    public float restoreFrequency = 3f;

    [Header("Red Lights")]
    //hazard light group
    public GameObject redLightsGroup;
    [SerializeField] private MeshRenderer[] redLightMeshes;
    public Light[] redLights;
    private Material[] redLightMaterials;
    private float[] initRedLightIntensities;
    private Color[] initRedLightEmissives;
    public float redRestoreDuration = 1f;
    public float redRestoreFrequency = 1f;

    [Header("Fire Spawning")]
    //Parent fire object, groups all the fires for ease of use
    [SerializeField] private GameObject fireParent;

    //Center and spawn positions for spawning fires within each module of the carousel
    [SerializeField] private GameObject centerPos;
    [SerializeField] private GameObject spawnPos;
    [SerializeField] private GameObject centerPos1;
    [SerializeField] private GameObject spawnPos1;
    [SerializeField] private GameObject centerPos2;
    [SerializeField] private GameObject spawnPos2;
    [SerializeField] private GameObject centerPos3;
    [SerializeField] private GameObject spawnPos3;

    //Boolean for determining if a fire has been started. Used for fire shutter logic
    private bool firesStarted = false;

    [Header("Fire Shutter")]
    [SerializeField] private DoorScript door;
    private DoorScript.States originalDoorState;

    [Header("Fire Extinguisher")]
    [SerializeField] private GameObject extinguisher;

    [Header("Fire Extinguisher")]
    [SerializeField] private Sparks[] sparks;

    //Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start() {
        //Store the light data using a helper function from Event.cs
        lights = GetLights(lightsGroup);
        initLightIntensities = GetLightIntensities(lights);
        var (mats, meshes, initEmissive) = GetObjectMaterialsPerObject(lightsGroup);
        lightMaterials = mats;
        lightMeshes = meshes;
        initLightEmissives = initEmissive;

        //Red lights
        redLights = GetLights(redLightsGroup);
        initRedLightIntensities = GetLightIntensities(redLights);
        var (hazardMats, hazardMeshes, hazardInitEmissive) = GetObjectMaterialsPerObject(redLightsGroup);
        redLightMaterials = hazardMats;
        redLightMeshes = hazardMeshes;
        initRedLightEmissives = hazardInitEmissive;

        //Fire shutter
        originalDoorState = door.DoorState;
    }

    //Update is called once per frame
    protected override void Update() {
        /* Checks if the event has started the fires yet to ensure this is not fired too early, as well as if the player
           has put out all the fires. */
        if (firesStarted && fireParent.GetComponent<FireScript>().CheckFireCount() == 0) {
            //The hazard lights turn off and fire shutter opens, allowing the player to progress again
            StartCoroutine(DeactivateRedLights());
            StartCoroutine(OpenFireShutter());

            /* Exposition from Alan */

            //Ensures this only ever runs once
            firesStarted = false;

            //Saves the game and activates the research lab checkpoint once the player has cleared the sequence
            GlobalSaveManager.SaveGame(true);
            researchLabCheckpoint.SetActive(true);
        }
    }

    //Method called by the terminal when it finishes uploading (that way the main sequence can be a coroutine)
    public void StartLabSequence() {
        //Starts main sequence coroutine
        StartCoroutine(LabSequence());
    }

    //Main sequence. Called as a coroutine so pacing can be managed by yielding to the return of other coroutines
    public IEnumerator LabSequence() {
        //Power is restored back to the lab, allowing the player to fully see the state of it and Dr. Felch
        yield return StartCoroutine(RestorePower());

        /* Dialogue from Alan, says it will run a query */

        //Short delay as Alan runs its query
        yield return new WaitForSeconds(1.0f);

        /* Due to the damage sustained by the machinery, Alan's query causes a short circuit. The flying sparks cause a small fire in the
           lab's carousel, which activates the hazard lights and fire shutter. */
        StartCoroutine(StartSparks()); /* Empty function */
        StartCoroutine(CloseFireShutter());
        StartCoroutine(ActivateRedLights());
        yield return StartCoroutine(StartFires());

        //Waits for a beat so that the fires started boolean is not activated too early
        yield return new WaitForSeconds(1.0f);

        //Sets fires started to true, the player can now access the completion state of this room
        firesStarted = true;

        //Fire extinguisher in room is enabled
        EnableFireExtinguisher();

        /* Dialogue from Alan, apologizing for and explaining the short circuit
           Tells the player that fire extinguishers are back online */

        GlobalSaveManager.SaveGame(true);

        yield return null;
    }

    #region Lights

    //Function that flickers back on the lights
    private IEnumerator RestorePower() {
        //Sets the light components and material emissives to active
        enableLights(lights, lightMaterials, true);

        //Flickers the lights on based on their already existing values, obtained from them above
        yield return StartCoroutine(FlickerLightsOn(
            lights, initLightIntensities, lightMaterials, initLightEmissives, restoreDuration, restoreFrequency));
    }

    //Function that activates the red lights in the room
    private IEnumerator ActivateRedLights() {
        //Sets the light components and material emissives to active
        enableLights(redLights, redLightMaterials, true);

        //Flickers the lights on based on their already existing values, obtained from them above
        yield return StartCoroutine(FlickerLightsOn(
            redLights, initRedLightIntensities, redLightMaterials, initRedLightEmissives, redRestoreDuration, redRestoreFrequency));
    }

    //Function that deactivates the red lights in the room
    private IEnumerator DeactivateRedLights() {
        //Sets the lights and emissives back to inactive
        enableLights(redLights, redLightMaterials, false);

        yield return null;
    }

    #endregion

    #region Sparks

    //Function that creates a spark effect in the carousel area, simulating the fire start
    private IEnumerator StartSparks() {
        Debug.Log("Start Sparks Called.");

        /* Script is not filled out. I tried to use the door sparks, but it looked bad. Could just be me
           being bad at utilizing particle effects, but it this will sit empty for now */

        yield return null;
    }

    #endregion

    #region Fire Shutter

    //Function that closes the fire shutter, locking the player in the room
    private IEnumerator CloseFireShutter() {
        //Grabs the door reference and sets its state to locked
        door.SetState(DoorScript.States.Locked);

        yield return null;
    }

    //Function that opens the fire shutter, allowing the player to continue progressing
    private IEnumerator OpenFireShutter() {
        //Grabs the door state and sets it back to its original state
        door.SetState(originalDoorState);

        yield return null;
    }

    #endregion

    #region Fire

    //Function that starts fire in the research lab carousel
    private IEnumerator StartFires() {
        /* Calls the event method that instantiates fires in a circular zone for each module in the carousel */
        InstantiateFireInCircumference(centerPos, spawnPos, 1, 0, 180, fireParent);
        InstantiateFireInCircumference(centerPos1, spawnPos1, 1, 0, 180, fireParent);
        InstantiateFireInCircumference(centerPos2, spawnPos2, 1, 0, 180, fireParent);
        InstantiateFireInCircumference(centerPos3, spawnPos3, 1, 0, 180, fireParent);

        yield return null;
    }

    #endregion

    #region Fire Extinguisher
    
    //Sets the fire extinguisher object to active
    private void EnableFireExtinguisher() {
        extinguisher.SetActive(true);
    }

    #endregion
}