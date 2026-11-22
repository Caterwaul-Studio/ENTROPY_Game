using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

public class Event : MonoBehaviour
{
    //Event script parent that givs you access to helpful
    //scripts that are common in game events

    //the persistant manager, which gives access to
    //the player
    private PersistantManager persistantManager;
    private GameObject player;
    private ZeroGravity zeroGravity;

    //the camera
    private Camera mainCamera;

    //door script and state handling
    private readonly Dictionary<DoorScript, DoorScript.States> originalDoorStates = new();

    protected virtual void Start()
    {
        //set the reference to the persistant manager and get the player and camera references
        persistantManager = FindFirstObjectByType<PersistantManager>();
        if (persistantManager != null)
        {
            player = persistantManager.PlayerObject;
            zeroGravity = persistantManager.Player;
            mainCamera = persistantManager.MainCamera;
        }
        else
        {
            Debug.LogError("PersistantManager not found in the scene.");
        }
    }

    protected virtual void Update()
    {
        //refind persistent player is mising
        if(persistantManager == null)
        {
            persistantManager = FindFirstObjectByType<PersistantManager>();
            if (persistantManager != null)
            {
                player = persistantManager.PlayerObject;
                zeroGravity = persistantManager.Player;
                mainCamera = persistantManager.MainCamera;
            }
            else
            {
                Debug.LogError("PersistantManager not found in the scene.");
            }
        }
    }

#region cutscene
    /// <summary>
    /// Method for moving the player to the panel position;
    /// -Destination where to move the player
    /// -duration: how long to take
    /// -eventCalled: bool to use externally for you to flag your logic
    /// </summary>
    /// <param name="destination"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    public IEnumerator LerpPosition(Vector3 destination, float duration, bool eventCalled)
    {
        eventCalled = true;
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
#endregion

#region object materials
    /// <summary>
    /// Method that fetches the materials for the bars & other objects so you can change their emmisive on event
    /// returns
    /// - list of materials
    /// - list of meshes
    /// sets your initial emmisive based on first in matrial list
    /// </summary>
    public (Material[], MeshRenderer[], Color InitBarEmisive) GetObjectMaterials(GameObject group)
    {

        MeshRenderer[] meshes = group.GetComponentsInChildren<MeshRenderer>();
        Material[] materials = new Material[meshes.Length];


        for (int i = 0; i < meshes.Length; i++)
        {
            materials[i] = meshes[i].material;
        }

        Color initBarEmissive = materials[0].GetColor("_EmissionColor");

        return (materials, meshes, initBarEmissive);
    }

    public (Material[], MeshRenderer[], Color[]) GetObjectMaterialsPerObject(GameObject group)
    {
        MeshRenderer[] meshes = group.GetComponentsInChildren<MeshRenderer>();
        Material[] materials = new Material[meshes.Length];
        Color[] initEmissives = new Color[meshes.Length];
        for (int i = 0; i < meshes.Length; i++)
        {
            materials[i] = meshes[i].material;
            initEmissives[i] = materials[i].GetColor("_EmissionColor");
        }
        return (materials, meshes, initEmissives);
    }

    public IEnumerator GlitchObjectEmissives(
        Material[] materials, 
        AnimationCurve curve,
        float duration,
        Color[] emissionColors, 
        float emissionMultiplier)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float v = curve.Evaluate(elapsed / duration);
            ApplyEmissiveState(materials, v, emissionColors, emissionMultiplier);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // land exactly on the curve's final value
        ApplyEmissiveState(materials, curve.Evaluate(1f), emissionColors, emissionMultiplier);
    }

    public void ApplyEmissiveState(Material[] mats, float curveValue
        , Color[] emissionColors, float emissionMultiplier)
    {
        if (mats != null)
        {
            for(int i=0; i < mats.Length; i++)
            {
                if (mats[i] != null)
                {
                    Color emission = emissionColors[i] * (curveValue * emissionMultiplier);
                    mats[i].SetColor("_EmissionColor", emission);
                }
            }
        }
    }
    #endregion

#region auxilliary lights
    public Light[] GetLights(GameObject lightsGroup)
    {
        return lightsGroup.GetComponentsInChildren<Light>();
    }

    /// <summary>
    /// Method that fetches the materials for the lights so you can change their emmisive on event
    ///  returns
    /// - list of materials
    /// - list of meshes
    /// sets your initial emmisive based on first in matrial list
    /// </summary>
    public (Material[], MeshRenderer[], Color initLightEmissive) GetLightMaterials(GameObject lightsGroup)
    {
        
        MeshRenderer[] lightMeshes = lightsGroup.GetComponentsInChildren<MeshRenderer>();
        Material[] lightMaterials = new Material[lightMeshes.Length];

        for (int i = 0; i < lightMeshes.Length; i++)
        {
            lightMaterials[i] = lightMeshes[i].material;
        }

        Color initLightEmissive = lightMaterials[0].GetColor("_EmissionColor");

        return (lightMaterials, lightMeshes, initLightEmissive);
    }

    /// <summary>
    /// Flickers lights and their emissive materials together using an AnimationCurve
    /// Curve value (0 = off, 1 = full) scales light intensity and emission
    /// Yield on it to wait for completion, or StartCoroutine it to run in parallel
    /// </summary>
    public IEnumerator GlitchLights(
        Light[] lights,
        Material[] emissiveMaterials,
        AnimationCurve curve,
        float duration,
        float lightIntensityMultiplier,
        Color[] emissionColors,
        float emissionMultiplier)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float v = curve.Evaluate(elapsed / duration);
            ApplyLightState(lights, emissiveMaterials, v, lightIntensityMultiplier, emissionColors, emissionMultiplier);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // land exactly on the curve's final value
        ApplyLightState(lights, emissiveMaterials, curve.Evaluate(1f), lightIntensityMultiplier, emissionColors, emissionMultiplier);
    }

    private void ApplyLightState(
        Light[] lights, Material[] mats, float curveValue,
        float lightMultiplier, Color[] emissionColors, float emissionMultiplier)
    {
        if (lights != null)
        {
            foreach (Light l in lights)
            {
                //if the light has a light flickr component, make sure it's disabled so it can turn off
                if(l.GetComponent<LightFlicker>() != null && l.GetComponent<LightFlicker>().isActiveAndEnabled) l.GetComponent<LightFlicker>().enabled = false;
                if (l != null) l.intensity = curveValue * lightMultiplier;
            }   
        }

        if (mats != null)
        {
            for(int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null)
                {
                    Color emission = emissionColors[i] * (curveValue * emissionMultiplier);
                    mats[i].SetColor("_EmissionColor", emission);
                }
            }
        }
    }

    /// <summary>
    /// Get the current intensity of every light so they can be restored
    /// call before glitches or power-down
    /// </summary>
    /// <param name="lights"></param>
    /// <returns></returns>
   public float[] GetLightIntensities(Light[] lights)
    {
        float[] intensities = new float[lights.Length];
        for(int i = 0; i < lights.Length; i++)
        {
            intensities[i] = lights[i] != null ? lights[i].intensity : 0f;
        }
        return intensities;
    }

    public IEnumerator FlickerLightsOn(
        Light[] lights,
        float[] targetIntensities,
        Material[] mats,
        Color[] targetEmissions,
        float duration = 2f,
        float frequency = 15f,
        bool reenableLightFlicker = false)
    {
        int lightCount = lights != null ? lights.Length : 0;
        int matsCount = mats != null ? mats.Length : 0;

        //each light/mat gets its own noise seed so they don't all flicker in sync
        float[] lightSeeds = new float[lightCount];
        for(int i = 0;i < lightCount;i++) lightSeeds[i] = Random.Range(0.0f, 1000f);
        float[] matsSeeds = new float[matsCount];
        for (int i = 0; i < matsCount; i++) matsSeeds[i] = Random.Range(0.0f, 1000f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float ramp = elapsed / duration;
            float t = elapsed * frequency;

            for(int i = 0; i < lightCount; i++)
            {
                if (lights[i] == null) continue;
                lights[i].intensity = TargetIntensity(targetIntensities, i) * FlickerValue(lightSeeds[i], t, ramp);
            }
            for (int i = 0; i < matsCount; i++)
            {
                if (mats[i] == null) continue;
                mats[i].SetColor("_EmissionColor", targetEmissions[i] * FlickerValue(matsSeeds[i], t, ramp));
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // set back to the original values
        RestoreLightState(lights, targetIntensities, mats, targetEmissions, reenableLightFlicker);
    }

    public void RestoreLightState(
        Light[] lights, float[] targetIntensities,
        Material[] mats, Color[] targetEmissions,
        bool reenableLightFlicker = false)
    {
        if(lights != null)
        {
            for(int i = 0; i <lights.Length; i++)
            {
                if (lights[i] == null) continue;
                lights[i].intensity = TargetIntensity(targetIntensities, i);

                if (reenableLightFlicker)
                {
                    LightFlicker f = lights[i].GetComponent<LightFlicker>();
                    if (f != null) f.enabled = true;
                }
            }
        }

        if(mats != null)
        {
            for(int i = 0; i < mats.Length; i++)
                if (mats != null) mats[i].SetColor("_EmissionColor", targetEmissions[i]);
        }
    }

    public static float TargetIntensity(float[] intensities, int i)
    {
        return(intensities != null && i < intensities.Length) ? intensities[i] : 0.0f;
    }

    private static float FlickerValue(float seed, float t, float ramp)
    {
        float noise = Mathf.PerlinNoise(seed, t);
        float onThreshold = Mathf.Lerp(0.25f, 0.8f, ramp);

        if (noise < onThreshold) return Mathf.Lerp(0.6f, 1f, ramp);
        return Random.Range(0f, 0.15f);
    }
    #endregion

#region hazard lights
    /// <summary>
    /// Get the hazard light scripts on the hazardlights in our group
    /// </summary>
    /// <param name="group"></param>
    /// <returns></returns>
    public HazardLight[] GetHazardLights(GameObject group)
    {
        // true = include inactive, since hazards are often disabled until the event
        return group != null
            ? group.GetComponentsInChildren<HazardLight>(true)
            : new HazardLight[0];
    }


    /// <summary>
    /// Activate the hazard lights, add a delay to have them activate staggered
    /// </summary>
    /// <param name="tf"></param>
    /// <param name="hazards"></param>
    /// <param name="maxDelay"></param>
    /// <returns></returns>
    public IEnumerator SetActiveHazardLights(bool tf, HazardLight[] hazards, float maxDelay = 0.5f)
    {
        foreach (HazardLight h in hazards)
        {
            if (h != null) h.IsHazard = tf;
            yield return new WaitForSeconds(Random.Range(0f, maxDelay / Mathf.Max(1, hazards.Length)));
        }
    }
    #endregion

#region doors
    /// <summary>
    /// Gets all door scripts assigned to your event group
    /// </summary>
    /// <param name="doorGroup"></param>
    /// <returns></returns>
    public DoorScript[] GetDoors(GameObject doorGroup)
    {
        return doorGroup != null
           ? doorGroup.GetComponentsInChildren<DoorScript>(true)
           : new DoorScript[0];
    }

    public void StoreDoorStates(DoorScript[] doors)
    {
        foreach (DoorScript door in doors)
            if (door != null) originalDoorStates[door] = Settle(door.DoorState);
    }

    public IEnumerator SetBrokenDoors(bool tf, bool restore, DoorScript[] doors, float maxDelay = 0.5f)
    {
        Debug.Log("set broken doors called");
        foreach (DoorScript d in doors)
        {
            if(d != null)
            {
                if (tf)
                {
                    if (!originalDoorStates.ContainsKey(d) && restore)
                        originalDoorStates[d] = Settle(d.DoorState);
                    d.SetState(DoorScript.States.Broken);
                }
                else if (!restore)
                {
                    d.SetState(DoorScript.States.Locked);
                }
                else if(originalDoorStates.TryGetValue(d, out var original))
                {
                    Debug.Log("Door restore called");
                    if (d.DoorState == DoorScript.States.Broken)
                        d.FixBroken(original);
                    else
                        d.SetState(original); // coming out of the powered down step

                    originalDoorStates.Remove(d);
                }
                else
                {
                    Debug.LogWarning($"SetBrokenDoors: no saved state for {d.name}, can't restore.");
                }
            }
            yield return new WaitForSeconds(Random.Range(0f, maxDelay / Mathf.Max(1, doors.Length)));
        }
    }


    private DoorScript.States Settle(DoorScript.States state) => state switch
    {
        DoorScript.States.Opening => DoorScript.States.Open,
        DoorScript.States.Closing => DoorScript.States.Closed,
        _ => state
    };
    #endregion

#region floating objects
    public FloatingObject[] GetFloatingObjects(GameObject group)
    {
        return group.GetComponentsInChildren<FloatingObject>();
    }

    public void SetFloatingObjsStaticState(FloatingObject[] objects)
    {
        foreach(FloatingObject obj in objects)
        {
            obj.useSpeedInput = false;
            obj.startNoSpeed = true;
        }
    }

    #endregion

#region cubby doors
    public HingeJoint[] GetCubbyDoors(GameObject group)
    {
        return group.GetComponentsInChildren<HingeJoint>();
    }

    public void SetCubbyDoorStaticState(bool tf, HingeJoint[] doors)
    {
        foreach(HingeJoint door in doors)
        {
            GameObject parent = door.gameObject;
            Rigidbody rb = parent.GetComponent<Rigidbody>();
            rb.isKinematic = tf;
        }
    }
    #endregion

#region wall shake
    /// <summary>
    /// helper method that stores the origins of the wall objects for when you need to shake them
    /// </summary>
    /// <param name="walls"></param>
    /// <returns></returns>
    public Vector3[] GetWallsOrigins(GameObject[] walls)
    {
        Vector3[] origins = new Vector3[walls.Length];

        for (int i = 0; i < walls.Length; i++)
        {
           origins[i] = walls[i].transform.position;
        }
        return origins;
    }

    public IEnumerator ShakeWalls(
        GameObject[] walls,
        Vector3[] origins,
        AnimationCurve curve, 
        float duration,
        float magnitude = 0.05f,
        float frequency = 25f)
    {
        float[] seeds = new float[walls.Length];
        for (int i = 0; i < seeds.Length; i++)
        seeds[i] = Random.Range(0f, 1000f);

        float elapsed = 0f;
        while(elapsed < duration)
        {
            float intensity = curve.Evaluate(elapsed/duration) * magnitude;
            float t = elapsed * frequency;

            for(int i = 0; i < walls.Length; i++)
            {
                if (walls[i] == null) continue;

                //create an offset
                Vector3 offset = new Vector3(
                    Mathf.PerlinNoise(seeds[i], t) - 0.5f,
                    Mathf.PerlinNoise(seeds[i] + 100f, t) - 0.5f,
                    Mathf.PerlinNoise(seeds[i] + 200f, t) - 0.5f) * 2f;

                //set the position
                walls[i].transform.position = origins[i] + offset * intensity;

                //Debug.Log("shake walls called");
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        //at the end snap everything back to origin
        for (int i = 0; i < walls.Length; i++)
        {
            if (walls[i] != null)
                walls[i].transform.position = origins[i];
        }
    }

    /// <summary>
    /// this method converts a range between whatever, scaled down to between 0 and 1. therefore your animation graph can be shape you want
    /// </summary>
    /// <param name="inValue"></param>
    /// <param name="inMin"></param>
    /// <param name="inMax"></param>
    /// <returns></returns>
    private static float RangeZeroToOne(float inValue, float inMin, float inMax)
    {
        float diffInputRange = Mathf.Abs(inMax - inMin);
        float convFactor = 1 / diffInputRange;
        return convFactor * (inValue - inMin);
    }
    #endregion

#region Geist
    //This is overidable if you want to do other effects, unique logic for your Geist cutscene
    public virtual void TriggerGeistCutscene(ref bool hasPlayed, SpawnGeist spawner, GeistCutsceneRoute route)
    {
        if (hasPlayed) return;
        if (PlayGeistRoute(spawner, route)) hasPlayed = true; ;
    }

    //run the Geist route you have created for your cutscene
    public bool PlayGeistRoute(SpawnGeist spawner, GeistCutsceneRoute route, System.Action onFinished = null)
    {
        if (spawner == null || spawner.Cutscene == null)
        {
            Debug.LogWarning("PlayGeistRoute: no spawner or Geist available");
            return false;
        }
        spawner.Cutscene.Play(route, onFinished);
        return true;
    }
    #endregion

#region Fire Instantiation    
    /* This method can be used to instantiate a fire if you you know an explicit position and rotation you want it at. Otherwise, this is
       a basis any method of more complex fire instantiation can repeatedly call */
    //Instantiates a fire prefab based on position and rotation passed in. Pass in the .transform of a parent object so the fire is grouped
    public virtual Object InstantiateFire(Vector3 position, Quaternion rotation, Transform parent) {
        //Assigns the fire node from the resources folder. This cannot be done outside the method, otherwise it is null
        Object fire = Resources.Load<Object>("Prefabs/FireNode");

        //Instantiate with values
        return Instantiate(fire, position, rotation, parent);
    }

    //If you are making your own fire spawning method, include this seeding method:

    /*
        Outside of the loop:
        
        //Creates a seed for each fire spawn, utilized to spawn them at different times
        float[] fireSeeds = new float[count];
        for (int i = 0; i < count; i++) fireSeeds[i] = Random.Range(0.1f, 0.5f);


        Inside the loop:
        
        //Waits to instantiate the fire based on this instance's generated seed
        await Awaitable.WaitForSecondsAsync(fireSeeds[i]);

                                                                                        */

    /* Using a point in space provided to it (centerPos), creates points along the circumference of a circle by rotating said point in space
       and applies a random variation to it (range) to prevent uniformity. Performs this as many times as required by the call (fireCount) 
       and instantiates a fire at a point parented to the centerPos (spawnPos). SpawnPos is placed away from the centerPos to create its radius,
       and rotating centerPos rotates spawnPos on that radius, giving a point along the circumference. RotMod can be used to not start from zero 
       on iterating through points on the circumference, but normally you can set it to zero. */
    public async void InstantiateFireInCircumference(
        GameObject centerPos, GameObject spawnPos, int fireCount, 
        float rotMod, float range, GameObject fireParent) {

        //Count for the for loop is determined by fireCount passed in
        int count = fireCount;
        //The step or distance traveled along the circumference is based upon how many fires are being created
        float step = (360f / count);

        //Creates a seed for each fire spawn, utilized to spawn them at different times
        float[] fireSeeds = new float[count];
        for (int i = 0; i < count; i++) fireSeeds[i] = Random.Range(0.1f, 0.5f);

        //Loops through the logic for each fire to be instantiated
        for (int i = 0; i < count; i++) {
            /* Creates a variation of the step along the circumference by getting:
                - The current iteration of the loop multiplied by the step
                - A random range, positive or negative, based on the range passed into the method
                - The rotation modification, which changes whether the iteration starts at 0 or not */
            float variation = i * step + Random.Range(-range, range) + rotMod;

            //Rotates centerPos on the z axis using the variation generated
            centerPos.transform.localRotation = Quaternion.Euler(
                centerPos.transform.localEulerAngles.x,
                centerPos.transform.localEulerAngles.y,
                variation
            );

            //Inverts the rotation of centerPos for instantiating the fire in the right direction
            Quaternion flippedRot = Quaternion.Euler(
                centerPos.transform.eulerAngles.x,
                centerPos.transform.eulerAngles.y,
                centerPos.transform.eulerAngles.z - 180f
            );

            //Waits to instantiate the fire based on this instance's generated seed
            await Awaitable.WaitForSecondsAsync(fireSeeds[i]);

            //Calls instatiateFire with the generated variables
            InstantiateFire(spawnPos.transform.position, flippedRot, fireParent.transform);
        }
    }

    #endregion

}