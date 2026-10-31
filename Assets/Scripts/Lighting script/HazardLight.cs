using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.U2D;
using UnityEngine.UIElements;

public class HazardLight : MonoBehaviour
{
    [SerializeField]
    private bool isHazard;
    [SerializeField]
    private Light hazardLight;
    private Vector3 activeSpotPos;
    private float lightInitIntensity;
    [SerializeField] 
    private Light lightBase;
    private Vector3 activeBasePos;
    private float spotInitIntensity;

    [SerializeField]
    private float rotateParam;
    [SerializeField]
    private float activateTimer = 2f;
    
    public AudioSource hazardLightSource;
    public AudioClip hazardLightClip;

    private bool alarmPlaying = false;

    [SerializeField]
    private GameObject lightActive;
    [SerializeField]
    private GameObject lightInactive;
    [SerializeField] private GameObject hazardActivePos;
    [SerializeField] private GameObject hazardInactivePos;
    [SerializeField] private GameObject hazardLightsInactivePos;
    [SerializeField]
    private AnimationCurve setActiveCurve;

    private bool appliedState;
    private bool initialized;
    private Coroutine transitionRoutine;

    public bool IsHazard
    {
        get { return isHazard; }
        set 
        { isHazard = value; }
    }

    void Start()
    {
        activeBasePos = lightBase.transform.position;
        activeSpotPos = hazardLight.transform.position;
        lightInitIntensity = lightBase.intensity;
        spotInitIntensity = hazardLight.intensity;

        if (!isHazard)
        {
            lightBase.enabled = false;
            hazardLight.enabled = false;
            lightBase.transform.position = lightInactive.transform.position;
            hazardLight.transform.position = lightInactive.transform.position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!initialized || isHazard != appliedState)
        {
            initialized = true;
            appliedState = isHazard;
            ApplyState(isHazard);
        }
        if (isHazard)
            hazardLight.transform.Rotate(Vector3.up * rotateParam * Time.deltaTime);
    }

    private void ApplyState(bool on)
    {

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(SetActiveHazardLight(on));

        if (on) PlayHazardAlarm();
        else StopHazardAlarm();
    }

    private IEnumerator SetActiveHazardLight(bool tf)
    {
        Vector3 start = lightActive.transform.position;
        Vector3 spotStart = hazardLight.transform.position;
        Vector3 lightStart = lightBase.transform.position;

        Vector3 target;
        Vector3 spotTarget;
        float spotStartIntensity = hazardLight.intensity;
        float spotTargetIntensity = 0f;
        Vector3 lightTarget;
        float lightStartIntensity = lightBase.intensity;
        float lightTargetIntensity = 0f;

        if (tf)
        {
            target = hazardActivePos.transform.position;
            spotTarget = activeSpotPos;
            spotTargetIntensity = spotInitIntensity;
            lightTarget = activeBasePos;
            lightTargetIntensity = lightInitIntensity;
            hazardLight.enabled = true;
            lightBase.enabled = true;
            lightActive.SetActive(true);
        }
        else
        {
            target = hazardInactivePos.transform.position;
            spotTarget = hazardLightsInactivePos.transform.position;
            spotTargetIntensity = 0.1f;
            lightTarget = hazardLightsInactivePos.transform.position;
            lightTargetIntensity = 0.001f;
        }
        yield return StartCoroutine(ApplyHazardLightState(tf, start, target, 
            spotStart, spotTarget, 
            lightStart, lightTarget, 
            spotStartIntensity,spotTargetIntensity, 
            lightStartIntensity, lightTargetIntensity, 
            activateTimer));
        if (!tf)
        {
            hazardLight.enabled = false;
            lightBase.enabled = false;
        }

        transitionRoutine = null;
    }

    private IEnumerator ApplyHazardLightState(bool tf, 
        Vector3 Objfrom, Vector3 Objto, 
        Vector3 spotStart, Vector3 spotTarget, 
        Vector3 lightStart, Vector3 lightTarget, 
        float spotStartIntensity, float spotTargetIntensity,
        float lightStartIntensity, float lightTargetIntensity,
        float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                float curveValue = setActiveCurve.Evaluate(t);
                //object position
                lightActive.transform.position = Vector3.Lerp(Objfrom, Objto, curveValue);
                //light positions
                hazardLight.transform.position = Vector3.Lerp(spotStart, spotTarget, curveValue);
                lightBase.transform.position = Vector3.Lerp(lightStart, lightTarget, curveValue);
                //light intensities
                hazardLight.intensity = Mathf.Lerp(spotStartIntensity, spotTargetIntensity, curveValue);
                lightBase.intensity = Mathf.Lerp(lightStartIntensity, lightTargetIntensity, curveValue);
            yield return null;
            }
        lightActive.transform.position = Objto;
    }

    public void PlayHazardAlarm()
    {
        /*        Debug.Log("PlayHazardAlarm called");
                if (hazardLightSource == null)
                {
                      Debug.LogError("Hazard Light Source is not assigned!");
                    return;
                }
                if (hazardLightClip == null)
                {
                    Debug.LogError("Hazard Light Clip is not assigned!");
                    return;
                }
                Debug.Log("Playing hazard alarm sound");
                */
        hazardLightSource.clip = hazardLightClip;
        hazardLightSource.loop = true;
        hazardLightSource.Play();

        /*        Debug.Log($"AudioSource isPlaying: {hazardLightSource.isPlaying}");
                Debug.Log($"AudioClip: {hazardLightSource.clip?.name}");*/
    }

    public void StopHazardAlarm()
    {
        //Debug.Log("Stopping hazard alarm sound");
        hazardLightSource.Stop();
    }
}
