using UnityEngine;

public class FlickerLight : MonoBehaviour
{
    [Header("FlickerSettings")]
    [SerializeField] private float flickerSpeedMax;
    [SerializeField] private float flickerSpeedMin;
    private float flickerSpeed;
    [SerializeField] private float flickerIntensityRange = 0.015f;

    [SerializeField] public bool flickerActive = false;
    [SerializeField] public bool lightActive = false;

    [SerializeField] float baseIntensity = 0.5f;

    private void Start()
    {
        if (lightActive)
        {
            EnableLight();
        }
        else if (!lightActive && this.GetComponent<Light>().enabled)
        {
            DisableLight();
        }

        flickerSpeed = Random.Range(flickerSpeedMin, flickerSpeedMax);
    }

    private void Update()
    {
        if (lightActive && flickerActive)
        {
            if(!this.GetComponent<Light>().enabled)
            {
                EnableLight();
            }
            UpdateFlicker();
        }
    }

    public void UpdateFlicker()
    {
        float noise = Mathf.PerlinNoise(flickerSpeed, Time.time * flickerSpeed);
        float flickerOffset = (noise - 0.5f) * flickerIntensityRange;
        this.GetComponent<Light>().intensity = baseIntensity + flickerOffset;
    }

    public void EnableLight()
    {
        Debug.Log(this.GetComponent<Light>().name);
        this.GetComponent<Light>().enabled = true;
    }

    public void DisableLight()
    {
        this.GetComponent<Light>().intensity = 0.0f;
        this.GetComponent<Light>().enabled = false;
    }
}
