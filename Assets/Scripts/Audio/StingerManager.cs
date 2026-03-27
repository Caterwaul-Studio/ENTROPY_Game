using UnityEngine;
using System.Collections;

public class StingerManager : MonoBehaviour
{
    [Header("Stinger Settings")]
    [SerializeField] private AudioClip tutorialStingerClip;
    [SerializeField] private AudioClip dormRoomStingerClip;
    [SerializeField] private AudioClip brokenDoorStingerClip;
    [SerializeField] private AudioClip explosionClip;
    [SerializeField] private AudioClip bodyScareStingerClip;
    [SerializeField] private AudioSource tutorialStingerSource;
    [SerializeField] private AudioSource dormRoomStingerSource;
    [SerializeField] private AudioSource brokenDoorStingerSource;
    [SerializeField] private AudioSource explosionSource;
    [SerializeField] private AudioSource bodyScareStingerSource;

    [SerializeField] private int priority = 10;

    public AudioClip TutorialStingerClip => tutorialStingerClip;
    public AudioClip DormRoomStingerClip => dormRoomStingerClip;
    public AudioClip BrokenDoorStingerClip => brokenDoorStingerClip;
    public AudioClip ExplosionClip => explosionClip;

    public AudioClip BodyScareStingerClip => bodyScareStingerClip;

    private Coroutine tutorialFadeCoroutine;
    /*    private Coroutine currentDormFade;
        private Coroutine currentDoorFade;*/

    /// <summary>
    /// Plays the tutorial stinger after a 7-second delay with fade-in and looping.
    /// </summary>
    /// <summary>
    /// Plays the tutorial stinger with a controllable fade-in and looping.
    /// Call this from your TutorialManager (after any desired delay).
    /// </summary>
    public void PlayTutorialStinger(float fadeInDuration = 3f)
    {
        if (tutorialStingerSource == null || tutorialStingerClip == null)
        {
            //Debug.LogWarning("AmbientController: Missing tutorial stinger AudioSource or Clip.");
            return;
        }

        // Stop any current fade coroutines
        if (tutorialFadeCoroutine != null)
            StopCoroutine(tutorialFadeCoroutine);

        tutorialStingerSource.priority = priority;
        tutorialStingerSource.clip = tutorialStingerClip;
        tutorialStingerSource.loop = true;
        tutorialStingerSource.volume = 0f;
        tutorialStingerSource.Play();

        // Begin fade-in
        tutorialFadeCoroutine = StartCoroutine(FadeAudioSource(tutorialStingerSource, 0f, 1f, fadeInDuration));
    }

    /// <summary>
    /// Stops (fades out) the tutorial stinger when the tutorial ends or is skipped.
    /// </summary>
    public void StopTutorialStinger(float fadeOutDuration = 12f)
    {
        if (tutorialStingerSource == null || !tutorialStingerSource.isPlaying)
            return;

        if (tutorialFadeCoroutine != null)
            StopCoroutine(tutorialFadeCoroutine);

        tutorialFadeCoroutine = StartCoroutine(FadeOutAndStop(tutorialStingerSource, fadeOutDuration));
    }

    // --- Internal helpers ---
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

    private IEnumerator FadeOutAndStop(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (source == null) yield break;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        source.volume = 0f;
        source.Stop();
    }
    /*    public void BrokenDoorStingerTriggered()
        {
            Debug.Log("Playing broken door stinger");
            brokenDoorStingerSource.clip = brokenDoorStingerClip;
            brokenDoorStingerSource.Play();
        }*/
    public void BrokenDoorStingerTriggered()
    {
        if (brokenDoorStingerSource == null || brokenDoorStingerClip == null) return;
        brokenDoorStingerSource.priority = priority;
        brokenDoorStingerSource.clip = brokenDoorStingerClip;
        brokenDoorStingerSource.loop = false;
        brokenDoorStingerSource.volume = 0f;
        brokenDoorStingerSource.Play();

        // Fade in at start
        StartCoroutine(FadeAudioSource(brokenDoorStingerSource, 0f, 1f, 2f));

        // Auto fade out near the end of the clip
        StartCoroutine(AutoFadeOut(brokenDoorStingerSource, brokenDoorStingerClip.length, 2f));
    }
    public void PlayExplosion()
    {
        //Debug.Log("Playing explosion sound");
        explosionSource.priority = priority;
        explosionSource.clip = explosionClip;
        explosionSource.loop = false;
        explosionSource.Play();

        StartCoroutine(FadeAudioSource(explosionSource, 0f, 1f, 4f));
    }

    public void PlayBodyScareStinger()
    {
        if (bodyScareStingerSource == null || bodyScareStingerClip == null)
        {
            //Debug.LogWarning("StingerManager: Missing body scare stinger AudioSource or Clip.");
            return;
        }
        bodyScareStingerSource.priority = priority;
        bodyScareStingerSource.clip = bodyScareStingerClip;
        bodyScareStingerSource.loop = false;
        bodyScareStingerSource.Play();
        // Fade in at start
        StartCoroutine(FadeAudioSource(bodyScareStingerSource, 0f, 1f, 5f));
        // Auto fade out near the end of the clip
        
    }

    public void PlayDormRoomStinger()
        {
        if (dormRoomStingerSource == null || dormRoomStingerClip == null)
        {
            //Debug.LogWarning("StingerManager: Missing dorm hall stinger AudioSource or Clip.");
            return;
        }
        dormRoomStingerSource.priority = priority;
        dormRoomStingerSource.clip = dormRoomStingerClip;
        dormRoomStingerSource.loop = false;
        dormRoomStingerSource.volume = 0f;
        dormRoomStingerSource.Play();
        // Fade in at start
        StartCoroutine(FadeAudioSource(dormRoomStingerSource, 0f, 1f, 8f));
        // Auto fade out near the end of the clip
        StartCoroutine(AutoFadeOut(dormRoomStingerSource, dormRoomStingerClip.length, 3f));
    }

    private IEnumerator AutoFadeOut(AudioSource source, float clipLength, float fadeDuration)
    {
        if (source == null) yield break;

        float waitTime = Mathf.Max(0f, clipLength - fadeDuration);
        yield return new WaitForSeconds(waitTime);

        // Fade out
        float startVolume = source.volume;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            if (source == null) yield break;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        source.volume = 0f;
        source.Stop();
    }

}

