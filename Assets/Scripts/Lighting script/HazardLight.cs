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
    private Transform activeSpotPos;
    [SerializeField] 
    private Light lightBase;
    private Transform activeBasePos;
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
        activeBasePos = lightBase.transform;
        activeSpotPos = hazardLight.transform;

        if (!isHazard)
        {
            lightBase.enabled = false;
            hazardLight.enabled = false;
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
        Vector3 target;
        if (tf)
        {
            target = hazardActivePos.transform.position;
        }
        else
            target = hazardInactivePos.transform.position;

        if(tf)
        {
            lightActive.SetActive(true);
        }
        yield return StartCoroutine(ApplyHazardLightState(start, target, activateTimer));
        if (!tf)
        {
            lightActive.SetActive(false);
        }

        hazardLight.enabled = tf;
        lightBase.enabled = tf;
        transitionRoutine = null;
    }

    private IEnumerator ApplyHazardLightState(Vector3 Objfrom, Vector3 Objto, float duration)
    {
        float elapsedTime = 0f;
        //timer
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            lightActive.transform.position = Vector3.Lerp(Objfrom, Objto, setActiveCurve.Evaluate(t));
            hazardLight.transform.position = Vector3.Lerp(from, to, setActiveCurve.Evaluate(t));
            lightBase.transform.position = Vector3.Lerp(from, to, setActiveCurve.Evaluate(t));
            yield return null;
        }
        lightActive.transform.position = to;
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
