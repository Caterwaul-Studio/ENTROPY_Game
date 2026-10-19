using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

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
            persistantManager = FindObjectOfType<PersistantManager>();
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

    public IEnumerator GlitchObjectEmissives(
        Material[] materials, 
        AnimationCurve curve,
        float duration,
        Color emissionColor, 
        float emissionMultiplier)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float v = curve.Evaluate(elapsed / duration);
            ApplyEmissiveState(materials, v, emissionColor, emissionMultiplier);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // land exactly on the curve's final value
        ApplyEmissiveState(materials, curve.Evaluate(1f), emissionColor, emissionMultiplier);
    }

    public void ApplyEmissiveState(Material[] mats, float curveValue
        , Color emissionColor, float emissionMultiplier)
    {
        if (mats != null)
        {
            Color emission = emissionColor * (curveValue * emissionMultiplier);
            foreach (Material m in mats)
                if (m != null) m.SetColor("_EmissionColor", emission);
        }
    }

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
        Color emissionColor,
        float emissionMultiplier)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float v = curve.Evaluate(elapsed / duration);
            ApplyLightState(lights, emissiveMaterials, v, lightIntensityMultiplier, emissionColor, emissionMultiplier);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // land exactly on the curve's final value
        ApplyLightState(lights, emissiveMaterials, curve.Evaluate(1f), lightIntensityMultiplier, emissionColor, emissionMultiplier);
    }

    private void ApplyLightState(
        Light[] lights, Material[] mats, float curveValue,
        float lightMultiplier, Color emissionColor, float emissionMultiplier)
    {
        if (lights != null)
        {
            foreach (Light l in lights)
                if (l != null) l.intensity = curveValue * lightMultiplier;
        }

        if (mats != null)
        {
            Color emission = emissionColor * (curveValue * emissionMultiplier);
            foreach (Material m in mats)
                if (m != null) m.SetColor("_EmissionColor", emission);
        }
    }

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

    public IEnumerator SetBrokenDoors(bool tf, bool restore, DoorScript[] doors, float maxDelay = 0.5f)
    {
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
                else if (!tf && !restore)
                {
                    d.SetState(DoorScript.States.Locked);
                }
                else if(originalDoorStates.TryGetValue(d, out var original))
                {
                    d.FixBroken(original);
                    originalDoorStates.Remove(d);
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
}

