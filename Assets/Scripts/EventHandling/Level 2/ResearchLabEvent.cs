using JetBrains.Annotations;
using System.Collections;
using UnityEngine;

public class ResearchLabSequence : Event {
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
    }

    //Update is called once per frame
    protected override void Update() {
        /* The player has successfully extinguished all the fires */
        if (firesStarted && fireParent.transform.childCount == 0) {
            /* The hazard lights turn off and the fire shutter opens again */
            StartCoroutine(DeactivateRedLights());
            StartCoroutine(OpenFireShutter());

            /* Exposition from Alan */

            //Ensures this only ever runs once
            firesStarted = false;

            /* Update the player's objectives and save state here */
        }
    }

    //Method called by the terminal when it finishes uploading (that way the main sequence can be a coroutine)
    public void StartLabSequence() {
        //Starts main sequence coroutine
        StartCoroutine(LabSequence());
    }

    //Main sequence. Called as a coroutine so pacing can be managed by yielding to the return of other coroutines
    public IEnumerator LabSequence() {
        Debug.Log("Start Lab Sequence Called.");

        /* The player has successfully rebooted the terminal
           Power is restored to the lab, but they now see the poor state of it and Dr. Felch */
        yield return StartCoroutine(RestorePower());

        /* Dialogue from Alan, says it will run a query */

        /* Short delay as Alan starts the query
           Alan's query has started a short circuit
           Sparks fly off of the machinery and a fire starts in the carousel
           This activates the fire shutter, closing the player in the room, and turns on the hazard lights */
        yield return new WaitForSeconds(1.0f);
        StartCoroutine(StartSparks());
        StartCoroutine(CloseFireShutter());
        StartCoroutine(ActivateRedLights());
        yield return StartCoroutine(StartFires());

        //Sets fires started to true, the player can now access the completion state of this room
        firesStarted = true;

        /* Fire extinguisher in room is enabled */
        EnableFireExtinguisher();

        /* Dialogue from Alan, apologizing for and explaining the short circuit
           Tells the player that fire extinguishers are back online */

        yield return null;
    }

    #region Lights

    //Function that flickers back on the lights
    private IEnumerator RestorePower() {
        Debug.Log("Lights Sequence Called.");

        lightsGroup.SetActive(true);

        yield return StartCoroutine(FlickerLightsOn(
            lights, initLightIntensities, lightMaterials, initLightEmissives, restoreDuration, restoreFrequency));
    }

    //Function that activates the red lights in the room
    private IEnumerator ActivateRedLights() {
        Debug.Log("Activate Red Lights Called.");

        redLightsGroup.SetActive(true);

        yield return StartCoroutine(FlickerLightsOn(
            redLights, initRedLightIntensities, redLightMaterials, initRedLightEmissives, redRestoreDuration, redRestoreFrequency));

        yield return null;
    }

    //Function that deactivates the hazard lights in the room
    private IEnumerator DeactivateRedLights() {
        Debug.Log("Deactivate Red Lights Called.");

        yield return null;
    }

    #endregion

    #region Sparks

    //Function that creates a spark effect in the carousel area, simulating the fire start
    private IEnumerator StartSparks() {
        Debug.Log("Start Sparks Called.");

        yield return null;
    }

    #endregion

    #region Fire Shutter

    //Function that closes the fire shutter, locking the player in the room
    private IEnumerator CloseFireShutter() {
        Debug.Log("Close Fire Shutter Called.");

        yield return null;
    }

    //Function that opens the fire shutter, allowing the player to continue progressing
    private IEnumerator OpenFireShutter() {
        Debug.Log("Open Fire Shutter Called.");

        yield return null;
    }

    #endregion

    #region Fire

    //Function that starts fire in the research lab carousel
    private IEnumerator StartFires() {
        /* Calls the event method that instantiates fires in a circular zone for each module in the carousel */
        InstantiateFireInCircumference(centerPos, spawnPos, 4, 0, 5, fireParent);
        InstantiateFireInCircumference(centerPos1, spawnPos1, 4, 45, 15, fireParent);
        InstantiateFireInCircumference(centerPos2, spawnPos2, 4, 0, 5, fireParent);
        InstantiateFireInCircumference(centerPos3, spawnPos3, 4, 45, 15, fireParent);

        yield return null;
    }

    #endregion

    #region Fire Extinguisher

    private void EnableFireExtinguisher() {

    }

    #endregion
}