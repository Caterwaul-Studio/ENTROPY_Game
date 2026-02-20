using System.Collections;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.Audio;

// A utility class to be used by the ambient controller (and anything else that finds it useful).
// The looper has two audio sources it switches between to make seamless transitions between loops.
// Timing and source control must be done externally, but the looper will manage flipping between players.
public class Looper : MonoBehaviour
{

    private int sourceIndex = 0;
    private AudioSource[] audioSources = new AudioSource[2];
    // private double endTime = 0.0f;
    private float normalVolume = 1f;
    private string currentClip = "none";

    public const double shortFadeDuration = 0.5f;
    public const double mediumFadeDuration = 4.0f;
    public const double longFadeDuration = 10.0f;
    
    public AudioMixerGroup musicGroup;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject child = new GameObject("LoopPlayer");
            child.transform.parent = gameObject.transform;
            audioSources[i] = child.AddComponent<AudioSource>();
            audioSources[i].loop = false;
        }

        foreach (AudioSource audioSource in audioSources)
        {
            audioSource.outputAudioMixerGroup = musicGroup;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    

    // public void Enqueue(AudioClip clip, double time, double end)
    public void Enqueue(AudioClip clip, bool isOn, double time)
    {
        currentClip = clip.name;
        sourceIndex = 1 - sourceIndex;
        audioSources[sourceIndex].clip = clip;
        audioSources[sourceIndex].volume = normalVolume;
        audioSources[sourceIndex].PlayScheduled(time);
        if (!isOn)
        {
            audioSources[0].volume = 0;
            audioSources[1].volume = 0;
        }
        else
        {
            audioSources[0].volume = normalVolume;
            audioSources[1].volume = normalVolume;
        }
            
        // endTime = end;
    }

    public void StopAt(double stopTime)
    {
        // Debug.Log("Had to fade");
        float delay = (float)(stopTime - (AudioSettings.dspTime + shortFadeDuration));
        StartCoroutine(Fade(audioSources[sourceIndex], delay, shortFadeDuration ,true));
    }

    public void FadeOut(double duration = longFadeDuration)
    {
        // Debug.Log("FadeOut triggered");
        StartCoroutine(Fade(audioSources[0], 0.0f, duration, true));
        StartCoroutine(Fade(audioSources[1], 0.0f, duration, true));
    }

    public void FadeIn()
    {
        // Debug.Log("FadeIn triggered" + currentClip);
        StartCoroutine(Fade(audioSources[0], 0.0f, longFadeDuration, false));
        StartCoroutine(Fade(audioSources[1], 0.0f, longFadeDuration, false));
    }
    
    // if fade out is true, will fade out, else will fade in
    IEnumerator Fade(AudioSource source, float timeBeforeFade,double duration, bool fadeOut)
    {
        
        yield return new WaitForSecondsRealtime(timeBeforeFade);

        double start = AudioSettings.dspTime;

        float destinationVolume;
        float startVolume;
        bool keepFading = true;
        if (fadeOut)
        {
            destinationVolume = 0;
            startVolume = normalVolume;
        }
        else
        {
            startVolume = 0;
            destinationVolume = normalVolume;
        }
        
        while (keepFading)
        {
            source.volume = Mathf.Lerp(startVolume, destinationVolume, (float)(Mathf.Abs((float)(AudioSettings.dspTime-start))/duration));
            // source.volume = Mathf.Lerp(startVolume, destinationVolume, (float)(AudioSettings.dspTime/end));
            if (fadeOut)
            {
                keepFading = (source.volume > 0);
            }
            else
            {
                keepFading = (source.volume < normalVolume);
            }
            yield return null;
        }

        // if (fadeOut)
        // {
        //     source.Stop();
        // }
        
    }

    public void SetVolume(float newVolume)
    {
        audioSources[0].volume = newVolume;
        audioSources[1].volume = newVolume;
        normalVolume = newVolume;
    }
}
