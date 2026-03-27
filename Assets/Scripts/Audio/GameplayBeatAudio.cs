using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class GameplayBeatAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource bodyStingerSource;
    public AudioSource[] serverSources;
    public AudioSource[] lockdownSources;
    public AudioSource leverSFXSource;
    public AudioSource buttonSource;
    public AudioSource alienSource;
    public AudioSource alienSource2;
    public AudioSource grateSource;

    [Header("SFX Clips")]
    //public AudioClip bodyFoundStinger;
    public AudioClip powerCutSFX;
    public AudioClip powerOnSFX;
    public AudioClip leverSFXClip;
    public AudioClip takeItem;
    public AudioClip serverHum;
    public AudioClip buttonPress;
    public AudioClip alienRunAway;
    public AudioClip moveGrate;
    public AudioClip alien1;
    public AudioClip alien2;
    public AudioClip alien3;
    public AudioClip alienBuzz;

    //[Header("Audio Mixer Groups")]
    //public AudioMixerGroup environmentalGroup;
    //public AudioMixerGroup ambienceGroup;

    private float serverVolume;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //foreach(AudioSource source in lockdownSources)
        //{
        //    source.outputAudioMixerGroup = environmentalGroup;
        //}

        //foreach (AudioSource source in serverSources)
        //{
        //    source.outputAudioMixerGroup = environmentalGroup;
        //}
        //buttonSource.outputAudioMixerGroup = environmentalGroup;
        //bodyStingerSource.outputAudioMixerGroup = ambienceGroup;

        if(serverSources[0] != null)
        {
            serverVolume = serverSources[0].volume;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void playMonitorPickup()
    {
        bodyStingerSource.PlayOneShot(takeItem);
    }

/*    public void playBodyStinger()
    {
        bodyStingerSource.PlayOneShot(bodyFoundStinger);
    }*/

    public void playPowerCut()
    {
        foreach(AudioSource source in lockdownSources)
        {
            source.clip = powerCutSFX;
            source.Play();
        }
    }

    public void playPowerOn()
    {
        foreach (AudioSource source in lockdownSources)
        {
            source.clip = powerOnSFX;
            source.Play();
        }
    }
    
    public void playLeverSFX()
    {
        leverSFXSource.clip = leverSFXClip;
        leverSFXSource.Play();
    }   

    public void FadeServers(bool fadeIn)
    {
        foreach(AudioSource source in serverSources)
        {
            StartCoroutine(Fade(source, 0, 2f, fadeIn, serverVolume));
        }
    }

    public void PlayButtonClick()
    {
        buttonSource.PlayOneShot(buttonPress);
    }

    public void playAlienRunAway()
    {
        StartCoroutine(AlienRunAwaySequence());
    }

    public void PlayMoveGrate()
    {
        grateSource.clip = moveGrate;
        grateSource.Play();
    }

    private IEnumerator AlienRunAwaySequence()
    {
        alienSource.clip = alien1;
        alienSource.Play();

        yield return new WaitForSeconds(5f);
        //alienSource2.clip = alienBuzz;
        //StartCoroutine(Fade(alienSource2, 0, 2, true, 1f));
        //alienSource2.Play();

        yield return new WaitForSeconds(10f);
        //StartCoroutine(Fade(alienSource2, 0, 2, false, 1f));
        alienSource.clip = alien2;
        alienSource.Play();

        yield return new WaitForSeconds(12f);

        alienSource.clip = alien3;
        alienSource.Play();

    }


    public IEnumerator Fade(AudioSource source, float timeBeforeFade, float fadeDuration, bool fadeIn, float originalVolume)
    {
        yield return new WaitForSecondsRealtime(timeBeforeFade);

        float startVolume = fadeIn ? 0f : originalVolume;
        float endVolume = fadeIn ? originalVolume : 0f;

        double startTime = AudioSettings.dspTime;
        double endTime = startTime + fadeDuration;

        source.volume = startVolume;

        if (fadeIn && !source.isPlaying)
            source.Play();

        while (AudioSettings.dspTime < endTime)
        {
            float t = (float)((AudioSettings.dspTime - startTime) / fadeDuration);
            source.volume = Mathf.Lerp(startVolume, endVolume, t);
            yield return null;
        }

        source.volume = endVolume;

        if (!fadeIn)
            source.Stop();
    }
}
