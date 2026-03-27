using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.UI;
using Random = UnityEngine.Random;
using System.Collections;



public struct Layer
{
    public int clip;
    public bool isOn;
}
public class AmbientController : MonoBehaviour
{

    public float startingVolume;
    public static int maxLayers = 5;
    public float bpm = 120.0f;
    public int numBeatsPerSegment;
    public AudioClip[] clips;
    public GameObject looperPrefab;
    // public Vector4[] sequence;
    int layerCount = 0;
    public int progression = 0;

    // I have tried a couple of ways doing this. This one is not the best for code legibility, but it is the one that...
    // is serializable. This way they can be changed in the inspector.
    public int[] beatsLookup; //Contains the beats length of each layer
    // public Tuple<int,string>[] clipLookup; //Contains [beats,name] of each loop layer
    
    

    private double[] endTimes;
    private double nextMinLoop;
    private int sourceIndex = 0;
    // private int clipIndex = 0;
    private Looper[] loopers = new Looper[maxLayers];
    private int[] layerLookup  = new int[maxLayers]; // the index matching the looper index contains the currently playing track
    private bool running = false;
    private int currentProgressStep = 0; //individual steps through audio changes over the course of one progress level

    [Header("Stinger Settings")]
    [SerializeField] private AudioClip tutorialStingerClip;
    [SerializeField] private AudioClip dormHallStingerClip;
    [SerializeField] private AudioSource tutorialStingerSource;
    [SerializeField] private AudioSource dormHallStingerSource;

    public AudioClip TutorialStingerClip => tutorialStingerClip;
    public AudioClip DormHallStingerClip => dormHallStingerClip;

    private Coroutine currentTutorialFade;
    private Coroutine currentDormFade;

    private void Awake()
    {
        layerCount = clips.Length;
        loopers = new Looper[maxLayers];
        // for (int i = 0; i < layerCount; i++)
        // {
        //     GameObject child = new GameObject("MusicPlayer");
        //     child.transform.parent = gameObject.transform;
        //     loopers[i] = child.AddComponent<AudioSource>();
        //     loopers[i].loop = false;
        // }
    }
    void Start()
    {
        endTimes = new double[clips.Length];
        double startTime = AudioSettings.dspTime + 2.0f;
        for (int i = 0; i < layerCount; i++)
        {
            endTimes[i] = startTime;
        }

        for (int i = 0; i < maxLayers; i++)
        {
            // GameObject looperObject = new GameObject("looper");
            // AudioSource audioObject = looperObject.AddComponent<AudioSource>();
            // loopers[i] = audioObject.AddComponent<Looper>();
            GameObject prefabInstance = Instantiate(looperPrefab);
            loopers[i] = prefabInstance.GetComponent<Looper>();
            layerLookup[i] = -1;
        }

        SetVolume(startingVolume);

        nextMinLoop = startTime;
        
        running = true;
        //Debug.Log("AmbientStarted");
    }

    void Update()
    {
        if (!running)
        {
            return;
        }

        double time = AudioSettings.dspTime;

        // Wait until next loop event is less than a second away
        if (time + 2.0f > nextMinLoop)
        {
            // Pick Layers based on progression
            // This is the main place where scoring takes place
            Layer[] nextLayers;
            switch (progression)
            {
                case 0:
                    nextLayers = new Layer[2];
                    nextLayers[0] = new Layer { clip = 0, isOn = true }; // Bass loop 0
                    nextLayers[1] = new Layer { clip = 7, isOn = false }; // Drums start muted
                    QueueNext(nextLayers, nextMinLoop);
                    break;

                case 1:
                    nextLayers = new Layer[2];
                    if (currentProgressStep == 0)
                    {
                        nextLayers[0] = new Layer { clip = 0, isOn = true }; // Bass loop 0
                    }
                    else
                    {
                        nextLayers[0] = new Layer { clip = 1, isOn = true }; // Bass loop 1
                    }

                    nextLayers[1] = new Layer { clip = 7, isOn = true }; // Drums 
                    QueueNext(nextLayers, nextMinLoop);
                    currentProgressStep++;
                    break;
                case 2:
                    nextLayers = new Layer[4];
                    switch (currentProgressStep)
                    {
                        case 0:
                            nextLayers[0] = new Layer { clip = 1, isOn = true }; // Bass loop 1
                            nextLayers[2] = new Layer { clip = 5, isOn = false }; //breathearp
                            break;
                        case 1:
                            nextLayers[0] = new Layer { clip = 2, isOn = true }; // Bass loop 2
                            nextLayers[2] = new Layer { clip = 5, isOn = true }; //breathearp
                            nextLayers[3] = new Layer { clip = 8, isOn = true }; //fallstart
                            break;
                        case 2: 
                            nextLayers[0] = new Layer { clip = 2, isOn = true }; // Bass loop 2
                            nextLayers[2] = new Layer { clip = 5, isOn = false }; //breathearp
                            nextLayers[3] = new Layer { clip = 10, isOn = true }; //fallend
                            break;
                        
                    }
                    nextLayers[1] = new Layer { clip = 7, isOn = true }; // Drums 
                    QueueNext(nextLayers, nextMinLoop);
                    if (currentProgressStep == 0)
                    {
                        int looperIndex = Array.IndexOf(layerLookup,5);
                        if (looperIndex == -1)
                        {
                            //Debug.Log("Somehow had no muted arp track to unmute");
                        }
                        else
                        {
                            loopers[looperIndex].FadeIn();
                        }
                        // break;
                    }

                    if (currentProgressStep == 1)
                    {
                        int looperIndex = Array.IndexOf(layerLookup,5);
                        if (looperIndex == -1)
                        {
                            //Debug.Log("Somehow had no arp track to fade out");
                        }
                        else
                        {
                            //Debug.Log("Fading out");
                            loopers[looperIndex].FadeOut();
                        }
                        
                    }

                    currentProgressStep++;
                    if (currentProgressStep == 3)
                    {
                        currentProgressStep = 0;
                    }

                    break;
                case 3:
                    nextLayers = new Layer[1];
                    nextLayers[0] = new Layer { clip = 0, isOn = false };
                    break;
                case 4:
                    nextLayers = new Layer[1];
                    nextLayers[0] = new Layer { clip = 0, isOn = true };
                    break;
                case 5: // these are all muted at start so they can be turned on immediately after stinger finishes.
                    nextLayers = new Layer[4];
                    nextLayers[0] = new Layer { clip = 3, isOn = false }; //Bass pattern 3
                    nextLayers[1] = new Layer { clip = 7, isOn = false }; //Drums
                    nextLayers[2] = new Layer { clip = 9, isOn = false }; //Fall
                    nextLayers[3] = new Layer { clip = 5, isOn = false }; //breathearp
                    break;
                case 6: 
                    nextLayers = new Layer[4];
                    nextLayers[0] = new Layer { clip = 3, isOn = true }; //Bass pattern 3
                    nextLayers[1] = new Layer { clip = 7, isOn = true }; //Drums
                    nextLayers[2] = new Layer { clip = 9, isOn = true }; //Fall
                    nextLayers[3] = new Layer { clip = 5, isOn = true }; //breathearp
                    break;
                case 7:
                    nextLayers = new Layer[5];
                    nextLayers[0] = new Layer { clip = 4, isOn = true }; //Bass pattern 4
                    nextLayers[1] = new Layer { clip = 7, isOn = true }; //Drums
                    nextLayers[2] = new Layer { clip = 9, isOn = true }; //Fall
                    nextLayers[3] = new Layer { clip = 5, isOn = true }; //breathearp
                    nextLayers[4] = new Layer { clip = 6, isOn = true }; //complarp
                    break;
            }
            
            // Pick a random clip and play it with bass
            // int[] nextLayers = new int[2];
            // nextLayers[0] = 0;
            // nextLayers[1] = Random.Range(1, clips.Length);
            // QueueNext(nextLayers,nextMinLoop);

            // Debug.Log("Scheduled source " + " to start at time " + nextMinLoop);

            nextMinLoop += (60.0f / bpm) * numBeatsPerSegment;
            
            // running = false;
        }
    }

    public void QueueNext(Layer[] nextSet, double nextEventTime)
    {
        //nextSet must not be longer than maxlayers
        for (int i = 0; i < maxLayers; i++)
        {
            // If the layerLookup entry is not -1 and is not in the nextset
            if (layerLookup[i] >= 0 && !(Array.IndexOf(nextSet,layerLookup[i])>-1))
            {
                
                if (endTimes[layerLookup[i]]-1.0f > nextEventTime) 
                {
                    //Debug.Log("stopping early");
                    loopers[i].StopAt(nextEventTime);
                }
                
                layerLookup[i] = -1;
            }
        }
        foreach (Layer layer in nextSet)
        {
            if (beatsLookup[layer.clip]>numBeatsPerSegment)
            {
                //check how much time left, and see if requeue is necessary
                if (endTimes[layer.clip]-1.0f> nextEventTime) 
                {
                    // It is already playing. leave it be.
                    // Debug.Log("No need to queue");
                    continue;
                }
            }
            // find the first free looper and play it
            int looperIndex = Array.IndexOf(layerLookup,layer.clip);
            if (looperIndex == -1)
            {
                looperIndex = Array.IndexOf(layerLookup, -1);
            } 
            if (looperIndex > -1)
            {
                endTimes[layer.clip] = nextEventTime + ((60.0f / bpm) * beatsLookup[layer.clip]);
                loopers[looperIndex].Enqueue(clips[layer.clip], layer.isOn, nextEventTime);
                layerLookup[looperIndex] = layer.clip;
            }
        }
    }
    
    public void SetVolume(float newVolume)
    {
        foreach (Looper looper in loopers)
        {
            if (looper != null)
            {
                looper.SetVolume(newVolume); 
            }
        }
    }

    public void Progress()
    {
        progression++;
        currentProgressStep = 0;
        switch (progression)
        {
            case 1: //Finished tutorial
                int looperIndex = Array.IndexOf(layerLookup,7); //Find looper for drum track
                if (looperIndex == -1)
                {
                    //Debug.Log("Somehow had no muted drum track to unmute prog = 1");
                }
                else
                {
                    loopers[looperIndex].FadeIn();
                }
                break;
            case 2: //Found wrist monitor
                break;
            case 3: //Nearing server room
                foreach (Looper looper in loopers)
                {
                    looper.FadeOut();
                }

                break;
            case 4: //Server room reboot complete
                foreach (Looper looper in loopers)
                {
                    looper.FadeIn();
                }

                break;
            case 5: //Found body
                foreach (Looper looper in loopers)
                {
                    looper.FadeOut();
                }

                break;
            case 6: //Body Stinger over
                foreach (Looper looper in loopers)
                {
                    looper.FadeIn();
                }

                break;
            case 7: //level beat
                break;
                
        }
    }

    public void FadeAll(bool fadingIn)
    {
        //Debug.Log(loopers);
        if(fadingIn)
        {
            foreach(Looper looper in loopers)
            {
                if(looper != null)
                looper.FadeIn();
            }
        }
        else
        {
            foreach(Looper looper in loopers)
            {
                if(looper != null)
                looper.FadeOut();
            }
        }
    }
/*
    public void PlayStinger(AudioClip clip, bool loop = false, float fadeInDuration = 0f)
    {
        if (clip == null) return;

        AudioSource targetSource = null;
        Coroutine fadeCoroutine = null;
        bool isTutorial = false;

        if (clip == tutorialStingerClip && tutorialStingerSource != null)
        {
            targetSource = tutorialStingerSource;
            fadeCoroutine = currentTutorialFade;
            isTutorial = true;
        }
        else if (clip == dormHallStingerClip && dormHallStingerSource != null)
        {
            targetSource = dormHallStingerSource;
            fadeCoroutine = currentDormFade;
        }

        if (targetSource == null) return;

        // Stop any existing fade
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        if (isTutorial && loop)
        {
            // Start looping coroutine with fade-in on each loop
            if (currentTutorialFade != null) StopCoroutine(currentTutorialFade);
            currentTutorialFade = StartCoroutine(LoopTutorialStinger(targetSource, clip, fadeInDuration));
        }
        else
        {
            // Normal playback (one-shot or dorm hall)
            targetSource.clip = clip;
            targetSource.loop = loop;
            targetSource.volume = 0f;
            targetSource.Play();

            if (fadeInDuration > 0f)
            {
                if (isTutorial)
                    currentTutorialFade = StartCoroutine(FadeAudioSource(targetSource, 0f, 1f, fadeInDuration));
                else
                    currentDormFade = StartCoroutine(FadeAudioSource(targetSource, 0f, 1f, fadeInDuration));
            }
            else
            {
                targetSource.volume = 1f;
            }
        }
    }


    public void StopStinger(AudioClip clip, float fadeOutDuration = 0f)
    {
        AudioSource targetSource = null;
        Coroutine fadeCoroutine = null;

        if (clip == tutorialStingerClip && tutorialStingerSource != null)
        {
            targetSource = tutorialStingerSource;
            fadeCoroutine = currentTutorialFade;
        }
        else if (clip == dormHallStingerClip && dormHallStingerSource != null)
        {
            targetSource = dormHallStingerSource;
            fadeCoroutine = currentDormFade;
        }

        if (targetSource == null || !targetSource.isPlaying)
            return;

        // Stop any fade-in currently happening
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // Begin fade-out
        if (fadeOutDuration > 0f)
        {
            if (clip == tutorialStingerClip)
                currentTutorialFade = StartCoroutine(FadeAndStop(targetSource, fadeOutDuration));
            else
                currentDormFade = StartCoroutine(FadeAndStop(targetSource, fadeOutDuration));
        }
        else
        {
            targetSource.Stop();
        }
    }

    private IEnumerator FadeAudioSource(AudioSource source, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (source == null) yield break;
            source.volume = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        source.volume = to;
    }

    private IEnumerator FadeAndStop(AudioSource source, float duration)
    {
        float startVol = source.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (source == null) yield break;
            source.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        source.volume = 0f;
        source.Stop();
    }

    private IEnumerator LoopTutorialStinger(AudioSource source, AudioClip clip, float fadeInDuration)
    {
        while (true)
        {
            source.volume = 0f;
            source.clip = clip;
            source.loop = false; // we control looping manually
            source.Play();

            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                source.volume = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            source.volume = 1f;

            // Wait for clip to finish
            yield return new WaitForSeconds(clip.length);
        }
    }*/


}
