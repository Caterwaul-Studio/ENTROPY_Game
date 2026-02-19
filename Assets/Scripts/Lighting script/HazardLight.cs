using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UIElements;

public class HazardLight : MonoBehaviour
{
    [SerializeField]
    private bool isHazard;
    [SerializeField]
    private Light light;
    [SerializeField] 
    private Light lightBase;
    [SerializeField]
    private float rotateParam;
    


    public AudioSource hazardLightSource;
    public AudioClip hazardLightClip;

    private bool alarmPlaying = false;

    [SerializeField]
    private GameObject lightActive;
    [SerializeField]
    private GameObject lightInactive;

    public bool IsHazard
    {
        get { return isHazard; }
        set { isHazard = value; }
    }


    // Update is called once per frame
    void Update()
    {
        //if the hazrd is set
        if(isHazard)
        {
            if(!light.enabled)
            {
                light.enabled = true;
                lightBase.enabled = true;
                
            }
            light.transform.Rotate(Vector3.up * rotateParam * Time.deltaTime);
            if (!alarmPlaying)
            {
                PlayHazardAlarm();
                alarmPlaying = true;
            }
        }
        else if (!isHazard)
        {
            if (light.enabled)
            {
                light.enabled = false;
                lightBase.enabled = false;
                lightActive.SetActive(false);
                lightInactive.SetActive(true);
            }
            if (alarmPlaying)
            {
                StopHazardAlarm();
                alarmPlaying = false;
            }           
        }
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
