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

    //bar group
    [SerializeField] private GameObject barsGroup;
    public Material[] barsMaterials;
    public MeshRenderer[] barsMeshes;
    private Color initBarEmissive;

    //light group
    [SerializeField] private GameObject lightsGroup;
    [SerializeField] private MeshRenderer[] lightMeshes;
    private Material[] lightMaterials;
    private Color initLightEmissive;


    public AnimationCurve powerDownCurve;
    public AnimationCurve glitchCurve;
    public Light[] lights;
    public Color endColor;
    public Color endButtonColor;
    public Color endEmissionColor;

    //hazard light group
    public GameObject hazardLightGroup;
    public HazardLight[] hazardLights;

    //wall lights
    public GameObject wallLightGroup;
    public MeshRenderer[] wallLightMeshes;
    public Material[] wallLightMaterials;
    public Color initWallLightEmissionColor;


    //doors references
    [SerializeField] private DoorScript[] doorScripts;
    [SerializeField] private GameObject doorsGroup;
    [SerializeField] private MeshRenderer[] doorMeshes;
    [SerializeField] private Material[] doorMaterials;
    Color initDoorEmissionColor;

    //connector references
    [SerializeField] private GameObject connectorGroup;
    [SerializeField] private MeshRenderer[] connectorMeshes;
    [SerializeField] private Material[] connectorMaterials;
    Color initConnectorEmissionColor;

    [SerializeField]
    private DoorScript[] doorsToOpen;
    [SerializeField]
    private DoorScript[] doorsToLock;

    //bool flags for event triggers
    public float powerDownDuration = 1f;
    public float glitchDuration = 10f; // How long one cycle of the curve takes
    public float intensityMultiplier = 1f;

    //Geist and Animator
    public Animator geistAnimator;
    public GameObject geist;
    public Light geistLight;
    public Light geistSpotlight;


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

        //connectors
        var (matsc, meshesc, initEmissivec) = GetObjectMaterials(connectorGroup);
        connectorMaterials = matsc;
        connectorMeshes = meshesc;
        initConnectorEmissionColor = initEmissivec;

    }

    // Update is called once per frame
    protected override void Update()
    {

    }


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
            //close doors

            //start rumbling of the walls

            //enable rigid bodies of floating objects so they look like they are being thrown by rumble

            //start glitching of the lights
            StartCoroutine(GlitchLights(
                lights, lightMaterials, glitchCurve, glitchDuration,
                intensityMultiplier, initLightEmissive, 1f));
            //bars glitch with this
            StartCoroutine(GlitchObjectEmissives(
                barsMaterials, glitchCurve, glitchDuration, initBarEmissive, 1f));
            //wall lights glitch oo
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
                initConnectorEmissionColor, 1f));
            //set doors broken
            StartCoroutine(SetBrokenDoors(true, false, doorScripts, 1f));
            
            yield return new WaitForSeconds(2f);
            //turn on the hazard lights
            StartCoroutine(SetActiveHazardLights(true, hazardLights, .5f));
            yield return new WaitForSeconds(8f);

            //everything shuts off, lights, power, doors etc.
            StartCoroutine(SetActiveHazardLights(false, hazardLights, .5f));
            StartCoroutine(SetBrokenDoors(false, false, doorScripts, 1f));
        }
        yield return new WaitForSeconds(1f);
    }
}
