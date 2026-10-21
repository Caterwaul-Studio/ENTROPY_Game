using UnityEngine;
using System.Collections;

public class Lvl2IntroSequenceEvents : Event
{
    private PersistantManager persistantManager;
    private GameObject player;
    private ZeroGravity zeroGravity;
    private InventoryManager inventoryManager;

    //flashlight references
    private Flashlight flashlight;

    //event trigger references
    private BoxCollider GeistHallRunTrigger;
    private BoxCollider GeistHallAppearTrigger;

    //bar group
    [Header("Bars")]
    [SerializeField] private GameObject barsGroup;
    public Material[] barsMaterials;
    public MeshRenderer[] barsMeshes;
    private Color initBarEmissive;
    

    [Header("Light Glitch")]
    public AnimationCurve glitchCurve;
    //bool flags for event triggers
    public float powerDownDuration = 1f;
    public float glitchDuration = 10f; // How long one cycle of the curve takes
    public float intensityMultiplier = 1f;
    [Header("Light Restore")]
    public float restoreDuration = 3f;
    public float restoreFrequency = 15f;

    [Header("Auxilliary Lights")]
    //light group
    [SerializeField] private GameObject lightsGroup;
    [SerializeField] private MeshRenderer[] lightMeshes;
    public Light[] lights;
    private Material[] lightMaterials;
    private float[] initLightIntensities;
    private Color initLightEmissive;

    [Header("Hazard Lights")]
    //hazard light group
    public GameObject hazardLightGroup;
    public HazardLight[] hazardLights;

    [Header("Wall Lights")]
    //wall lights
    public GameObject wallLightGroup;
    public MeshRenderer[] wallLightMeshes;
    public Material[] wallLightMaterials;
    public Color initWallLightEmissionColor;

    [Header("Doors")]
    //doors references
    [SerializeField] private DoorScript[] doorScripts;
    [SerializeField] private GameObject doorsGroup;
    [SerializeField] private MeshRenderer[] doorMeshes;
    [SerializeField] private Material[] doorMaterials;
    Color initDoorEmissionColor;
    [SerializeField] private DoorScript exitFromStorage;

    [Header("Connectors")]
    //connector references
    [SerializeField] private GameObject connectorGroup;
    [SerializeField] private MeshRenderer[] connectorMeshes;
    [SerializeField] private Material[] connectorMaterials;
    Color initConnectorEmissionColor;

    [Header("Floating Objects Static Before Event")]
    [SerializeField] private GameObject floatingObjectsGroup;
    [SerializeField] private FloatingObject[] floatingObjects;

    [Header("Cubby Doors Static Before Event")]
    [SerializeField] private GameObject cubbyDoorsGroup;
    [SerializeField] private HingeJoint[] cubbyDoors;

    [Header("Walls to shake")]
    [SerializeField] private GameObject[] wallsArray;
    [SerializeField] private Vector3[] wallOrigins;
    [SerializeField] private AnimationCurve wallShakeCurve;
    [SerializeField] float wallShakeDuration = 10f;
    [SerializeField] float wallShakeMagnitude = 0.05f;
    [SerializeField] float wallShakeFrequency = 25f;

    [Header("Geist Cutscene")]
    //Geist and Animator
    [SerializeField] private SpawnGeist geistSpawner;
    [SerializeField] private GeistCutsceneRoute hallRunRoute;
    private bool hallRunPlayed;

    [SerializeField] private GeistCutsceneRoute hallAppearRoute;
    private bool hallAppearPlayed;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        persistantManager = PersistantManager.Instance;
        if (persistantManager != null)
        {
            player = persistantManager.PlayerObject;
            zeroGravity = persistantManager.Player;
            inventoryManager = persistantManager.InventoryManager;
            flashlight = inventoryManager.flashlight;
        }
        else
        {
            Debug.LogError("PersistantManager not found in the scene.");
        }
        //store the light data using a helper function from Event.cs
        lights = GetLights(lightsGroup);
        initLightIntensities = GetLightIntensities(lights);
        var (mats, meshes, initEmissive) = GetLightMaterials(lightsGroup);
        lightMaterials = mats;
        lightMeshes = meshes;
        initLightEmissive = initEmissive;

        //hazardlights
        hazardLights = GetHazardLights(hazardLightGroup);

        //bars
        var (matsb, meshesb, initEmissiveb) = GetObjectMaterials(barsGroup);
        barsMaterials = matsb;
        barsMeshes = meshesb;
        initBarEmissive = initEmissiveb;

        //wall lights
        var (matsw, meshsw, initEmissivew) = GetObjectMaterials(wallLightGroup);
        wallLightMaterials = matsw;
        wallLightMeshes = meshsw;
        initWallLightEmissionColor = initEmissivew;

        //doors
        var (matsd, meshesd, initEmissived) = GetObjectMaterials(connectorGroup);
        doorMaterials = matsd;
        doorMeshes = meshesd;
        initDoorEmissionColor = initEmissived;
        doorScripts = GetDoors(doorsGroup);
        StoreDoorStates(doorScripts);

        //connectors
        var (matsc, meshesc, initEmissivec) = GetObjectMaterials(connectorGroup);
        connectorMaterials = matsc;
        connectorMeshes = meshesc;
        initConnectorEmissionColor = initEmissivec;

        //floating objects
        floatingObjects = GetFloatingObjects(floatingObjectsGroup);
        SetFloatingObjsStaticState(floatingObjects);

        //cubby doors
        cubbyDoors = GetCubbyDoors(cubbyDoorsGroup);
        SetCubbyDoorStaticState(true, cubbyDoors);

        //walls
        wallOrigins = GetWallsOrigins(wallsArray);
    }

    // Update is called once per frame
    protected override void Update()
    {

    }

    #region storage room impact tremor
    public void TriggerExplosion()
    {
        StartCoroutine(ExplosionRocksTheShip());
    }

    public IEnumerator ExplosionRocksTheShip()
    {
        Debug.Log("ExplosionRocksTheShip event started.");
        //flag that guards if th flashlight has been aquired
        if (flashlight.HasFlashlight)
        {
            yield return new WaitForSeconds(3f);

            //start rumbling of the walls
            StartCoroutine(ShakeWalls(wallsArray, wallOrigins, wallShakeCurve, wallShakeDuration, wallShakeMagnitude, wallShakeFrequency));

            //floating objects spill out of cubbys
            SetCubbyDoorStaticState(false, cubbyDoors);

            yield return new WaitForSeconds(1f);
            //start glitching of the lights
            StartCoroutine(GlitchLights(
                lights, lightMaterials, glitchCurve, glitchDuration,
                intensityMultiplier, initLightEmissive, 1f));
            //bars glitch with this
            StartCoroutine(GlitchObjectEmissives(
                barsMaterials, glitchCurve, glitchDuration, initBarEmissive, 1f));
            //wall lights glitch too
            StartCoroutine(GlitchObjectEmissives(
                wallLightMaterials, glitchCurve, glitchDuration, 
                initWallLightEmissionColor, 1f));
            //connector lights
            StartCoroutine(GlitchObjectEmissives(
                connectorMaterials, glitchCurve, glitchDuration,
                initConnectorEmissionColor, 1f));
            //door lights
            StartCoroutine(GlitchObjectEmissives(
                doorMaterials, glitchCurve, glitchDuration,
                initDoorEmissionColor, 1f));
            //set doors broken
            StartCoroutine(SetBrokenDoors(true, false, doorScripts, 1f));
            
            yield return new WaitForSeconds(3f);
            //turn on the hazard lights
            StartCoroutine(SetActiveHazardLights(true, hazardLights, .5f));
            yield return new WaitForSeconds(8f);

            //everything shuts off, lights, power, doors etc.
            StartCoroutine(SetActiveHazardLights(false, hazardLights, .5f));
            StartCoroutine(SetBrokenDoors(false, false, doorScripts, 1f));
            exitFromStorage.SetState(DoorScript.States.Closed);
        }
        yield return new WaitForSeconds(1f);
    }
    #endregion

    #region power restored
    public void TriggerPowerRestored()
    {
        StartCoroutine(PowerRestored());
    }

    private IEnumerator PowerRestored()
    {
        Debug.Log("Power restored called");

        yield return StartCoroutine(FlickerLightsOn(
            lights, initLightIntensities, lightMaterials, initLightEmissive, restoreDuration, restoreFrequency));

        StartCoroutine(FlickerLightsOn(null, null, barsMaterials, initBarEmissive, restoreDuration, restoreFrequency));
        StartCoroutine(FlickerLightsOn(null, null, wallLightMaterials, initWallLightEmissionColor, restoreDuration, restoreFrequency));
        StartCoroutine(FlickerLightsOn(null, null, connectorMaterials, initConnectorEmissionColor, restoreDuration, restoreFrequency));
        StartCoroutine(FlickerLightsOn(null, null, doorMaterials, initDoorEmissionColor, restoreDuration, restoreFrequency));

        StartCoroutine(SetBrokenDoors(false, true, doorScripts, 1f));
    }

    #endregion

    #region Geist spooks
    public void TriggerGeistHallRun()
    {
        TriggerGeistCutscene(ref hallRunPlayed, geistSpawner, hallRunRoute);
    }

    public void TriggerGeistAppear()
    {
        TriggerGeistCutscene(ref hallAppearPlayed, geistSpawner, hallAppearRoute);
    }

    #endregion
}
