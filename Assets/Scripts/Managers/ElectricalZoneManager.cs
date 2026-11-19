using UnityEngine;
using System.Collections;

public class ElectricalZoneManager : Event, PowerObject
{
    [SerializeField]
    private GameObject doors;
    [SerializeField]
    private GameObject connectors;
    [SerializeField]
    private GameObject lightsGroup;
    [SerializeField]
    private GameObject wires;
    [SerializeField]
    private GameObject elecBarCover;
    private Transform[] elecBars;

    private DoorScript[] doorScripts;
    private DoorScript[] connectorScripts;
    //door materials
    [SerializeField] private MeshRenderer[] doorMeshes;
    [SerializeField] private Material[] doorMaterials;
    Color[] initDoorEmissionColors;
    //connector materials
    [SerializeField] private MeshRenderer[] connectorMeshes;
    [SerializeField] private Material[] connectorMaterials;
    Color[] initConnectorEmissionColors;

    private LiveWire[] wireScripts;
    //light materials
    [SerializeField] private MeshRenderer[] lightMeshes;
    public Light[] lights;
    private Material[] lightMaterials;
    private float[] initLightIntensities;
    private Color[] initLightEmissives;
    //modifiers for turning off and on lights, copied and modified from IntroSequenceEvents.cs
    [Header("Off Curve")]
    public AnimationCurve offCurve;
    public float powerDownDuration = .3f; // How long one cycle of the curve takes
    public float intensityMultiplier = 1f;
    [Header("On Factors")]
    public float restoreDuration = .3f;
    public float restoreFrequency = 15f;
    //used to prevent light coroutines from running simultaneously
    private bool settingLights = false;

    [SerializeField]
    private PowerObject.PowerStates currentPowerState;
    public PowerObject.PowerStates CurrentPowerState
    {
        get { return currentPowerState; }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    override protected void Start()
    {
        base.Start();

        doorScripts = GetDoors(doors);
        var (matsd, meshesd, initEmissivesd) = GetObjectMaterialsPerObject(doors);
        doorMaterials = matsd;
        doorMeshes = meshesd;
        initDoorEmissionColors = initEmissivesd;

        connectorScripts = GetDoors(connectors);
        var (matsc, meshesc, initEmissivesc) = GetObjectMaterialsPerObject(connectors);
        connectorMaterials = matsc;
        connectorMeshes = meshesc;
        initConnectorEmissionColors = initEmissivesc;

        if (wires != null)
        {
            wireScripts = wires.GetComponentsInChildren<LiveWire>();
        }
        else
        {
            wireScripts = new LiveWire[0];
        }
        if (elecBarCover != null)
        {
            elecBars = elecBarCover.GetComponentsInChildren<Transform>();
        }
        if (lightsGroup != null){
            //get lights information (copied from Start in IntroSequenceEvents.cs
            lights = GetLights(lightsGroup);
            initLightIntensities = GetLightIntensities(lights);
            var (mats, meshes, initEmissive) = GetObjectMaterialsPerObject(lightsGroup);
            lightMaterials = mats;
            lightMeshes = meshes;
            initLightEmissives = initEmissive;
        }

        SetPowerState(PowerObject.PowerStates.Unpowered);
    }
    //Used to prevent light modifying coroutines from overlapping
    private IEnumerator SetLights(PowerObject.PowerStates powerState)
    {
        if (powerState == PowerObject.PowerStates.Unpowered)
        {
            //ApplyLightState(lights, lightMaterials, 0f, 1f, initLightEmissives, 1f);
            yield return new WaitUntil(()=>!settingLights);
            settingLights = true;
            Debug.Log("Turn Lights Off");
            StartCoroutine(GlitchObjectEmissives(doorMaterials, offCurve, powerDownDuration, initDoorEmissionColors, 1f));
            StartCoroutine(GlitchObjectEmissives(connectorMaterials, offCurve, powerDownDuration, initConnectorEmissionColors, 1f));
            yield return StartCoroutine(GlitchLights(
                lights, lightMaterials, offCurve, powerDownDuration,
                intensityMultiplier, initLightEmissives, 1f));

            settingLights = false;
        }
        else
        {
            yield return new WaitUntil(() => !settingLights);
            settingLights = true;
            Debug.Log("Turn Lights On");
            StartCoroutine(FlickerLightsOn(null, null, connectorMaterials, initConnectorEmissionColors, restoreDuration, restoreFrequency));
            StartCoroutine(FlickerLightsOn(null, null, doorMaterials, initDoorEmissionColors, restoreDuration, restoreFrequency));
            yield return StartCoroutine(FlickerLightsOn(lights, initLightIntensities, lightMaterials, initLightEmissives, restoreDuration, restoreFrequency));
            settingLights = false;
        }
    }
    public void SetPowerState (PowerObject.PowerStates powerState) {
        currentPowerState = powerState;
        if (lights != null)
        {
            StartCoroutine(SetLights(powerState));
        }
        foreach (DoorScript doorScript in doorScripts) {
            doorScript.SetPowerState(powerState);
        }
        foreach (DoorScript connectorScript in connectorScripts)
        {
            connectorScript.SetPowerState(powerState);
        }
        foreach (LiveWire wireScript in wireScripts)
        {
            wireScript.SetPowerState(powerState);
        }
        if (elecBarCover != null)
        {
            foreach (Transform elecBar in elecBars)
            {
                elecBar.gameObject.SetActive(powerState == PowerObject.PowerStates.Unpowered ? false : true);
            }
        }
    }
}
