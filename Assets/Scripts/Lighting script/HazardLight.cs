using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UIElements;

public class HazardLight : MonoBehaviour
{
    [SerializeField]
    private bool isHazard;
    [SerializeField]
    private Light hazardLight;
    [SerializeField] 
    private Light lightBase;
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
        {
            isHazard = value;
            if(isActiveAndEnabled && (!initialized || isHazard != appliedState))
            {
                initialized = true;
                appliedState = value;
                ApplyState(isHazard);
            }
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
        hazardLight.enabled = tf;
        lightBase.enabled = tf;
        lightActive.SetActive(tf);
        lightInactive.SetActive(!tf);

        Vector3 start = lightActive.transform.position;
        Vector3 target = tf ? hazardActivePos.transform.position : hazardInactivePos.transform.position;

        float elapsed = 0f;
        while (elapsed < activateTimer)
        {
            float v = setActiveCurve.Evaluate(elapsed / activateTimer);
            ApplyHazardLightState(start, target, v);
            elapsed += Time.deltaTime;
            yield return null;
        }
        ApplyHazardLightState(start, target, 1f);  
    }

    private void ApplyHazardLightState(Vector3 from, Vector3 to, float v)
    {
        lightActive.transform.position = Vector3.LerpUnclamped(from, to, v);
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
