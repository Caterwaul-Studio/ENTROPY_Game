using System.Collections;
using UnityEngine;

public class Sparks : MonoBehaviour
{
    public ParticleSystem spark;
    public AudioSource audio;
    public AudioClip sparkSFX;
    public bool enabled = true;
    public Light lightSource;
    private float lightIntensity = 0.05f;

    public float minBurstDelay = 1.5f;
    public float maxBurstDelay = 4f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lightSource.intensity = 0;
        StartCoroutine(BurstLoop());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator BurstLoop()
    {
        yield return new WaitForSeconds(5f);
        while (true)
        {
            if (spark.emission.enabled)
            {
                float delay = Random.Range(minBurstDelay, maxBurstDelay);
                yield return new WaitForSeconds(delay);
                
                spark.Emit(Random.Range(5, 12));
                StartCoroutine(LightFlash());
                if (audio.isActiveAndEnabled)
                {
                    audio.PlayOneShot(sparkSFX);
                }


                }
            else
            {
                yield return new WaitForSeconds(0.1f); // Prevent CPU spin
            }
        }
    }

    public void ToggleSparks(bool value)
    {
        var emission = spark.emission;
        emission.enabled = value;
        enabled = value;
    }
    
    private IEnumerator LightFlash()
    {
        lightSource.intensity = 0.1f;
        yield return new WaitForSeconds(0.1f);
        lightSource.intensity = 0f;
    }

}
