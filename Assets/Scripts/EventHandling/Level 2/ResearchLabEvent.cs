using JetBrains.Annotations;
using System.Collections;
using UnityEngine;

public class ResearchLabSequence : Event {
    [Header("Research Lab Lights")]
    //light group
    [SerializeField] private GameObject lightsGroup;
    [SerializeField] private MeshRenderer[] lightMeshes;
    public Light[] lights;
    private Material[] lightMaterials;
    private float[] initLightIntensities;
    private Color[] initLightEmissives;

    [Header("Hazard Lights")]
    //hazard light group
    public GameObject hazardLightGroup;
    public HazardLight[] hazardLights;

    [Header("Fire Spawning")]
    //Center and spawn positions for spawning fires within each module of the carasouel
    [SerializeField] private GameObject centerPos;
    [SerializeField] private GameObject spawnPos;
    [SerializeField] private GameObject centerPos1;
    [SerializeField] private GameObject spawnPos1;
    [SerializeField] private GameObject centerPos2;
    [SerializeField] private GameObject spawnPos2;
    [SerializeField] private GameObject centerPos3;
    [SerializeField] private GameObject spawnPos3;


    //Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start() {
        //store the light data using a helper function from Event.cs
        lights = GetLights(lightsGroup);
        initLightIntensities = GetLightIntensities(lights);
        var (mats, meshes, initEmissive) = GetObjectMaterialsPerObject(lightsGroup);
        lightMaterials = mats;
        lightMeshes = meshes;
        initLightEmissives = initEmissive;

        //hazardlights
        hazardLights = GetHazardLights(hazardLightGroup);
    }

    //Update is called once per frame
    protected override void Update() {
        
    }

    //Method called by the terminal when it finishes uploading
    public async void startLabSequence() {
        Debug.Log("Start Lab Sequence Called.");

        //Awaits for 3 seconds, destroys cancellation token for memory purposes
        await Awaitable.WaitForSecondsAsync(3f, destroyCancellationToken);

        StartCoroutine(lightsSequence());
        StartCoroutine(startFires());
    }

    //Function that flickers back on the lights
    private IEnumerator lightsSequence() {
        Debug.Log("Lights Sequence Called.");

        yield return null;
    }

    //Function that starts fire in the research lab carousel
    private IEnumerator startFires() {
        /* Calls the event method that instantiates fires in a circular zone for each module in the carasoul */
        InstantiateFireInCircumference(centerPos, spawnPos, 4, 0, 5);
        InstantiateFireInCircumference(centerPos1, spawnPos1, 4, 45, 15);
        InstantiateFireInCircumference(centerPos2, spawnPos2, 4, 0, 5);
        InstantiateFireInCircumference(centerPos3, spawnPos3, 4, 45, 15);

        yield return null;
    }
}