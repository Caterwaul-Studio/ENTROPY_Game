using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls air vent physics forces, audio, and particle effects.
/// Can operate continuously or cycle on/off with timers.
/// </summary>
public class AirBreachScript : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private float thrust = 10.0f;
    [SerializeField] private BoxCollider bc;

    [Header("Timer Settings")]
    [SerializeField] private bool useTimers = false;
    [SerializeField] private float activeTimeDuration = 6.0f;
    [SerializeField] private float inactiveTimeDuration = 3.0f;
    [SerializeField] private float timeOffset = 0.0f;

    [Header("Audio/Visual")]
    [SerializeField] private ParticleSystem ps;
    [SerializeField] private ParticleSystem shortPuff;
    [SerializeField] private AudioSource breachAudio;
    [SerializeField] private AudioSource crackAudio;
    [SerializeField] private AudioSource pipeBurstAudio;
    [SerializeField] private AudioClip pipeBurstSFX;
    [SerializeField] private AudioClip airBlowSFX;
    [SerializeField] private AudioClip[] breachLeadupSFX;

    // State tracking
    public bool isPoweredOn = true;
    private bool isCurrentlyBlowing = true;
    private bool hasPlayedPipeBurst = false;

    private float cycleTimer;
    private float originalVolume;
    public HashSet<Rigidbody> affectedRigidbodies = new HashSet<Rigidbody>();

    

    public bool IsPoweredOn
    {
        get => isPoweredOn;
        set
        {
            if (isPoweredOn != value)
            {
                isPoweredOn = value;
                if (!isPoweredOn) DisableVent();
            }
        }
    }

    public bool IsCurrentlyBlowing
    {
        get => isCurrentlyBlowing;
    }

    void Start()
    {
        cycleTimer = activeTimeDuration - timeOffset;
        if (breachAudio != null) originalVolume = breachAudio.volume;
        shortPuff.Stop();
        if (isPoweredOn)
        {
            EnableVent();
        }
    }

    void Update()
    {
        if (!isPoweredOn)
        {
            if (isCurrentlyBlowing) DisableVent();
            return;
        }

        if (useTimers)
        {
            cycleTimer -= Time.deltaTime;

            if (cycleTimer <= 0)
            {
                if (isCurrentlyBlowing)
                {
                    DisableVent();
                    cycleTimer = inactiveTimeDuration;
                }
                else
                {
                    EnableVent();
                    cycleTimer = activeTimeDuration;
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (!isCurrentlyBlowing) return;

        foreach (var rb in affectedRigidbodies)
        {
            if (rb != null)
            {
                //Debug.Log("Adding force");
                rb.AddForce(transform.up * thrust);
            }
        }
    }

    private void EnableVent()
    {
        StartCoroutine(PlayBreachStartup());
    }

    private IEnumerator PlayBreachStartup()
    {
        yield return StartCoroutine(PlayLeadUp());
        isCurrentlyBlowing = true;

        if (bc != null) bc.enabled = true;
        if (ps != null) ps.Play();

        if (breachAudio != null)
        {
            StartCoroutine(FadeAudio(0f, 0.1f, fadeIn: true));
        }
    }

    private void DisableVent()
    {
        isCurrentlyBlowing = false;

        if (bc != null) bc.enabled = false;
        if (ps != null) ps.Stop();

        if (breachAudio != null)
        {
            StartCoroutine(FadeAudio(0f, 0.1f, fadeIn: false));
        }

        affectedRigidbodies.Clear();
    }

    /// <summary>
    /// Immediately turns the vent on, overriding timer state.
    /// </summary>
    public void TurnOn()
    {
        if(isPoweredOn == false)
        {
            isPoweredOn = true;
            EnableVent();
        }
        
    }

    /// <summary>
    /// Immediately turns the vent off, overriding timer state.
    /// </summary>
    public void TurnOff()
    {
        if(isPoweredOn == true)
        {
            isPoweredOn = false;
            DisableVent();
        }
        
    }

    private IEnumerator FadeAudio(float delay, float duration, bool fadeIn)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        float startVolume = fadeIn ? 0f : originalVolume;
        float endVolume = fadeIn ? originalVolume : 0f;

        if (fadeIn && !breachAudio.isPlaying)
        {
            breachAudio.volume = 0f;
            if (breachAudio.isActiveAndEnabled)
                breachAudio.Play();
        }

        double startTime = AudioSettings.dspTime;
        double endTime = startTime + duration;

        while (AudioSettings.dspTime < endTime)
        {
            float t = (float)((AudioSettings.dspTime - startTime) / duration);
            breachAudio.volume = Mathf.Lerp(startVolume, endVolume, t);
            yield return null;
        }

        breachAudio.volume = endVolume;

        if (!fadeIn) breachAudio.Stop();
    }

    private IEnumerator PlayLeadUp()
    {
        int index = Random.Range(0, breachLeadupSFX.Length);
        crackAudio.clip = breachLeadupSFX[index];
        crackAudio.pitch = Random.Range(0.8f, 1.2f);
        if (crackAudio.isActiveAndEnabled)
            crackAudio.Play();
        shortPuff.Play();
        yield return new WaitForSeconds(1.5f);

        PlayPipeBurst();
    }
    private void PlayPipeBurst()
    {

        if (pipeBurstAudio != null && pipeBurstSFX != null)
        {
            pipeBurstAudio.clip = pipeBurstSFX;
            pipeBurstAudio.pitch = Random.Range(0.7f, 1.1f);
            if (pipeBurstAudio.isActiveAndEnabled)
                pipeBurstAudio.Play();
        }
    }
}