using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LightLocation
{
    Dining,
    EscapePod
}

public class LightData
{
    public Transform[] lightGroup;
    public Dictionary<Light, float> initLightIntensity;

    public LightData(Transform[] lg, Dictionary<Light, float> li)
    {
        initLightIntensity = li;
        lightGroup = lg;
    }
}

public class LightManager : MonoBehaviour, ISaveable
{
    [SerializeField]
    public Dictionary<LightLocation, LightData> lightData;

    [SerializeField]
    private Transform[] diningLightGroup;
    [SerializeField]
    private Transform[] escapeLightGroup;


    private Color lightColor = new Color(0.75f, 0.75f, 0.75f, 0.0f);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lightData = new Dictionary<LightLocation, LightData>();

        SaveLightData(LightLocation.Dining, diningLightGroup);
        SaveLightData(LightLocation.EscapePod, escapeLightGroup);

        if (GlobalSaveManager.LoadFromSave) GlobalSaveManager.LoadSavable(this, false);


    }



    //private Coroutine FickerLights(LightLocation lightEnum, float totalDuration, float singleLightDuration, bool randomSequence)
    //{

    //    if (randomSequence)
    //    {
    //        lightData[lightEnum].lightGroup = Shuffle(lightData[lightEnum].lightGroup);
    //    }

    //    return StartCoroutine(EnableLights(lightData[lightEnum].lightGroup, lightData[lightEnum].initLightIntensity, totalDuration, singleLightDuration));
    //}

    //public Coroutine FadeOutLights(LightLocation lightEnum, float totalDuration)
    //{
    //    return StartCoroutine(ControlAllLights(lightData[lightEnum].lightGroup, lightData[lightEnum].initLightIntensity, 0.0f, totalDuration));
    //}

    //public Coroutine MultiplyLights(LightLocation lightEnum, float multiplier, float totalDuration)
    //{
    //    return StartCoroutine(MultiplyLightsTask(lightData[lightEnum].lightGroup, lightData[lightEnum].initLightIntensity, multiplier, totalDuration));
    //}

    public IEnumerator FlickerLights(LightLocation lightEnum, float totalDuration, float singleLightDuration, bool randomSequence)
    {
        //must clone the light group and intensity data so that we can modify it for the flicker without affecting the original data stored in the light manager,
        //which is used for saving/loading and other light control tasks
        Transform[] lightGroup = (Transform[])lightData[lightEnum].lightGroup.Clone();
        Dictionary<Light, float> initLightIntensity = lightData[lightEnum].initLightIntensity;

        if (randomSequence)
        {
            lightGroup = Shuffle(lightGroup);
        }

        // calculate delay time such that it is based on total duration and time of each light
        float delayBetweenLights;

        if (lightGroup.Length > 1)
            delayBetweenLights = (totalDuration - singleLightDuration) / (lightGroup.Length - 1);
        else
            delayBetweenLights = 0f;


        List<Coroutine> runningCoroutines = new List<Coroutine>();

        // each light group
        foreach (Transform t in lightGroup)
        {
            Light[] lights = t.GetComponentsInChildren<Light>();
            MeshRenderer[] meshes = t.GetComponentsInChildren<MeshRenderer>();

            foreach (MeshRenderer mesh in meshes)
            {
                runningCoroutines.Add(StartCoroutine(FlickerIntensity(null, mesh, 2.0f, singleLightDuration)));
            }

            // flicker lights on with coroutine
            foreach (Light light in lights)
            {
                runningCoroutines.Add(StartCoroutine(FlickerIntensity(light, null, initLightIntensity[light], singleLightDuration)));
            }

            // if there is no delay, dont wait
            if (delayBetweenLights > 0.0f)
            {
                yield return new WaitForSeconds(delayBetweenLights);
            }

        }

        foreach (Coroutine coroutine in runningCoroutines)
        {
            yield return coroutine;
        }
    }

    public IEnumerator FlickerLightsForever(LightLocation lightEnum, float minFlickerDuration = 5f, float maxFlickerDuration = 10f, float minPauseDuration = 3f, float maxPauseDuration = 8f)
    {

        Transform[] lightGroup = lightData[lightEnum].lightGroup;
        Dictionary<Light, float> initLightIntensity = lightData[lightEnum].initLightIntensity;

        while (true)
        {
            // Pick a random light from the group (skipping index 0, this is hard coded for the escape pod rn)
            Transform randomLight = lightGroup[Random.Range(1, lightGroup.Length)];

            Light[] lights = randomLight.GetComponentsInChildren<Light>();
            MeshRenderer[] meshes = randomLight.GetComponentsInChildren<MeshRenderer>();

            float flickerDuration = Random.Range(minFlickerDuration, maxFlickerDuration);
            //Debug.Log(flickerDuration);

            List<Coroutine> runningCoroutines = new List<Coroutine>();

            // Start all flickers for this light
            foreach (MeshRenderer mesh in meshes)
            {
                runningCoroutines.Add(StartCoroutine(FlickerIntensity(null, mesh, 2.0f, flickerDuration, 0.5f, 2, 0, 0.5f, false)));
            }

            foreach (Light light in lights)
            {
                runningCoroutines.Add(StartCoroutine(FlickerIntensity(light, null, initLightIntensity[light], flickerDuration, 0.0f, 0.02f, 0.8f, 1.5f, false)));
            }

            // Wait for this light to finish flickering
            //foreach (Coroutine coroutine in runningCoroutines)
            //{
            //    yield return coroutine;
            //}

            // Random pause before next flicker
            float pauseDuration = Random.Range(minPauseDuration, maxPauseDuration);
            yield return new WaitForSeconds(pauseDuration);
        }
    }

    private IEnumerator FlickerIntensity(Light light, MeshRenderer mesh, float maxIntensity, float singleLightDuration,
        float onDelayMin = 0.05f, float onDelayMax = 0.5f, float offDelayMin = 0.0f, float offDelayMax = 0.01f, bool lerpLightIntensity = true)
    {
        // do flicker logic here
        float timer = 0f; // overall time of flickering
        float flickerTimer = 0f; // time on a current state
        float flickerDelay = 0f; // time till next flicker

        float lerpIntensity = maxIntensity;
        bool isOn = false;

        // loops duration of flicker. 2.0f duration
        while (timer < singleLightDuration)
        {
            // lerp the brightness of max intensity so it gradually fades brighter
            if (lerpLightIntensity) lerpIntensity = Mathf.Lerp(0, maxIntensity, Mathf.Clamp01(timer / singleLightDuration));
            else lerpIntensity = maxIntensity;

           
            // swaps to lowlight after flickerdelay
            if (flickerTimer > flickerDelay)
            {
                isOn = !isOn;
                flickerTimer = 0.0f;
            }

            if (isOn && flickerTimer == 0.0f)
            {
                // longer delay if on
                flickerDelay = Random.Range(onDelayMin, onDelayMax);
                //Debug.Log("Delay when on: " + flickerDelay);
            }
            else if (!isOn && flickerTimer == 0.0f)
            {
                // randomize the intensity if flickering
                lerpIntensity = Random.Range(0.0f, lerpIntensity);
                flickerDelay = Random.Range(offDelayMin, offDelayMax);
                //Debug.Log("Delay when off: " + flickerDelay);
            }

            // applies intensity to respective item
            if (mesh != null && flickerTimer == 0)
            {
                mesh.material.SetColor("_EmissionColor", lightColor * lerpIntensity);
            }
            else if (light != null && flickerTimer == 0)
            {
                light.intensity = lerpIntensity;
            }

            timer += Time.deltaTime;
            flickerTimer += Time.deltaTime;

            yield return null;
        }

        // ensure on when flicker over
        if (mesh != null)
        {
            mesh.material.SetColor("_EmissionColor", lightColor * maxIntensity);
        }
        else if (light != null)
        {
            light.intensity = maxIntensity;
        }


        yield return null;
    }

    public IEnumerator FadeOutAllLights(LightLocation lightEnum, float endIntensity, float totalDuration)
    {
        Transform[] lightGroup = lightData[lightEnum].lightGroup;
        Dictionary<Light, float> initLightIntensity = lightData[lightEnum].initLightIntensity;

        // Gather all lights first
        List<Light> allLights = new List<Light>();

        foreach (Transform t in lightGroup)
        {
            allLights.AddRange(t.GetComponentsInChildren<Light>());
        }

        float time = 0.0f;

        while (time < totalDuration)
        {
            time += Time.deltaTime;
            float t = time / totalDuration;

            foreach (Light light in allLights)
            {
                light.intensity = Mathf.Lerp(initLightIntensity[light], endIntensity, t);
            }

            // yield once per frame for smooth fading
            yield return null;
        }

        // Ensure final intensity is exactly 0
        foreach (Light light in allLights)
        {
            light.intensity = endIntensity;
        }
    }

    
    public IEnumerator MultiplyAllLights(LightLocation lightEnum, float multiplier, float totalDuration)
    {
        Transform[] lightGroup = lightData[lightEnum].lightGroup;
        Dictionary<Light, float> initLightIntensity = lightData[lightEnum].initLightIntensity;

        // Gather all lights first
        List<Light> allLights = new List<Light>();
        Dictionary<Light, float> targetIntensities = new Dictionary<Light, float>();

        foreach (Transform t in lightGroup)
        {
            Light[] lights = t.GetComponentsInChildren<Light>();
            foreach (Light light in lights)
            {
                if (initLightIntensity.ContainsKey(light))
                {
                    allLights.Add(light);
                    targetIntensities[light] = initLightIntensity[light] * multiplier;
                }
            }
        }

        float time = 0.0f;

        while (time < totalDuration)
        {
            time += Time.deltaTime;
            float t = time / totalDuration;

            foreach (Light light in allLights)
            {
                light.intensity = Mathf.Lerp(initLightIntensity[light], targetIntensities[light], t);
            }

            // yield once per frame for smooth fading
            yield return null;
        }

        // Ensure final intensity is exactly 0
        foreach (Light light in allLights)
        {
            light.intensity = targetIntensities[light];
        }
    }



   

    private void SaveLightData(LightLocation saveLocation, Transform[] array)
    {
        Dictionary<Light, float> initLightIntensity = new Dictionary<Light, float>();

        foreach (Transform transform in array)
        {
            // saves the initial light values so in editor can display the intended lights
            foreach (Light l in transform.GetComponentsInChildren<Light>())
            {
                initLightIntensity[l] = l.intensity;
            }

        }

        lightData.Add(saveLocation, new LightData(array, initLightIntensity));
        //Debug.Log("Save Successful!");

        DisableLights(array);
    }

    // turn off lights
    private void DisableLights(Transform[] array)
    {
        foreach (Transform transform in array)
        {
            // saves the initial light values so in editor can display the intended lights
            foreach (Light l in transform.GetComponentsInChildren<Light>())
            {
                l.intensity = 0.0f;
            }

            // sets lights to off
            foreach (MeshRenderer m in transform.GetComponentsInChildren<MeshRenderer>())
            {
                m.material.SetColor("_EmissionColor", lightColor * 0f);
            }
        }
    }

    // randomizes array
    private Transform[] Shuffle(Transform[] array)
    {
        System.Random rng = new System.Random();
        int n = array.Length;
        while (n > 1)
        {
            // Get a random index from 0 to n-1
            int k = rng.Next(n--);
            // Swap the element at the current end with the random element
            Transform temp = array[n];
            array[n] = array[k];
            array[k] = temp;
        }

        return array;
    }

    // Data class to hold the save information
    [System.Serializable]
    public class LightManagerData
    {
        public List<float> lightIntensities;
        public List<float> meshEmissionIntensities;

        public LightManagerData(List<float> intensities, List<float> emissions)
        {
            lightIntensities = intensities;
            meshEmissionIntensities = emissions;
        }
    }

    // Add these methods to your LightManager class
    public void LoadSaveFile(string fileName)
    {
        string path = Application.persistentDataPath;
        string loadedData = GlobalSaveManager.LoadTextFromFile(path, fileName);
        //catch if no save data exists, this can happen if player tries to load a save before saving for the first time, or if save data is deleted
        if (loadedData == null || loadedData == "") return;

        LightManagerData _lightManagerData = JsonUtility.FromJson<LightManagerData>(loadedData);

        // Get all lights in the same order as when we saved
        List<Light> allLights = GetAllLightsInOrder();

        // Load the light intensities in the same order they were saved
        for (int i = 0; i < _lightManagerData.lightIntensities.Count && i < allLights.Count; i++)
        {
            allLights[i].intensity = _lightManagerData.lightIntensities[i];
        }
        // create this integer j to keep track of the order of the mesh emission intensities in the save data,
        // which should correspond to the order of the meshes in the light groups for loading correctly
        int j = 0;
        //cycle through the light groups of the light manager and save the current intensity of each light in the same order for loading later
        foreach (LightLocation loc in new[] { LightLocation.Dining, LightLocation.EscapePod })
        {
            //cycle through the transforms so we can directly access the meshes in the same order for loading later
            foreach (Transform t in lightData[loc].lightGroup)
            {
                //cycle through the mesh renderers in the current transform and load the emission intensity values in the same order they were saved using int j to keep track of the order
                foreach (MeshRenderer mesh in t.GetComponentsInChildren<MeshRenderer>())
                {
                    if (j < _lightManagerData.meshEmissionIntensities.Count)
                    {
                        // load the saved emission intensity value into the mesh in the order they were saved, using int j, and multiply by lightColor to get the correct emission color
                        float emissionIntensity = _lightManagerData.meshEmissionIntensities[j] * lightColor.r; // Assuming lightColor is the base color for emission
                        // set the emission color of the mesh material to the loaded intensity value multiplied by the light color
                        mesh.material.SetColor("_EmissionColor", lightColor * emissionIntensity);
                        j++;
                    }
                    else
                    {
                        Debug.LogWarning("Not enough emission intensity values in save data for all meshes.");
                        return;
                    }
                }
            }
        }
    }

    //BUG____the lights in the Dining room are not working
    //intensity not saved correctly
    //check organization of the lights in the project
    /// <summary>
    /// Create save file information for the lights
    /// </summary>
    /// <param name="fileName"></param>
    public void CreateSaveFile(string fileName)
    {
        //establish
        LightManagerData _lightManagerData = new LightManagerData(new List<float>(), new List<float>());

        // Get all lights in consistent order
        List<Light> allLights = GetAllLightsInOrder();
        foreach(Light light in allLights)
        {
            _lightManagerData.lightIntensities.Add(light.intensity);
            //debug to verify the light names and intensities are being saved in the correct order
            //Debug.Log("Create Save File " + light.gameObject.name + " | " + light.intensity);
        }

        //cycle through the light groups of the light manager and save the current intensity of each light in the same order for loading later
        foreach (LightLocation loc in new[] {LightLocation.Dining, LightLocation.EscapePod})
        {
            //cycle through the transforms so we can directly access the lights in the same order for loading later
            foreach (Transform t in lightData[loc].lightGroup)
            {
                foreach(MeshRenderer mesh in t.GetComponentsInChildren<MeshRenderer>())
                {
                    //save the emission intensity of the mesh in the same order for loading later
                    Color emissionColor = mesh.material.GetColor("_EmissionColor");
                    _lightManagerData.meshEmissionIntensities.Add(emissionColor.r / lightColor.r);
                }
            }
        }

        string json = JsonUtility.ToJson(_lightManagerData);
        string path = Application.persistentDataPath;
        GlobalSaveManager.SaveTextToFile(path, fileName, json);
    }

    // Helper method to get all lights in a consistent order
    private List<Light> GetAllLightsInOrder()
    {
        List<Light> allLights = new List<Light>();

        // Add dining lights
        foreach (Transform transform in diningLightGroup)
        {
            Light[] lights = transform.GetComponentsInChildren<Light>();
            allLights.AddRange(lights);
            // Debug the light names and intensities to verify correct order
            //foreach (Light light in lights)
            //{
            //    Debug.Log("Load Save File " + light.gameObject.name + " | " + light.intensity);
            //}
        }

        // Add escape pod lights
        foreach (Transform transform in escapeLightGroup)
        {
            Light[] lights = transform.GetComponentsInChildren<Light>();
            allLights.AddRange(lights);
            // Debug the light names and intensities to verify correct order
            //foreach (Light light in lights)
            //{
            //    Debug.Log("Load Save File " + light.gameObject.name + " | " + light.intensity);
            //}
        }

        return allLights;
    }

}
