using UnityEngine;
using System.Collections;

public class TerminalAudioManager : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip bootupClip;
    public AudioClip uploadCompleteClip;
    public AudioClip offBeepClip;
    public AudioClip loadingBarClip;

    //[Header("Audio Sources")]
    //public AudioSource bootupSource;            // For bootup sound
    //public AudioSource uploadCompleteSource;    // For upload complete sound

    private Coroutine fadeCoroutine;

    /// <summary>
    /// Play the bootup sound with optional fade-in
    /// </summary>
    public void PlayBootupSound(AudioSource source, float fadeInDuration = 3f)
    {
        if (source == null || bootupClip == null) return;

        source.clip = bootupClip;
        source.loop = false;
        source.volume = 0f;
        source.Play();

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeAudio(source, 0f, 1f, fadeInDuration));
    }

    /// <summary>
    /// Stop or fade out the bootup sound
    /// </summary>
    public void FadeOutBootup(AudioSource source, float fadeOutDuration = .1f)
    {
        if (source == null) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeAudio(source, source.volume, 0f, fadeOutDuration, stopAfterFade: true));
    }

    /// <summary>
    /// Play the upload complete sound
    /// </summary>
    public void PlayUploadCompleteSound(AudioSource source)
    {
        if (source == null || uploadCompleteClip == null) return;

        source.clip = uploadCompleteClip;
        source.loop = false;
        source.volume = 1f;
        source.Play();
    }

    public void PlayOffBeepSound(AudioSource source)
    {
        if (source == null || offBeepClip == null) return;
        source.clip = offBeepClip;
        source.loop = false;
        source.volume = 1f;
        source.Play();
    }

    public void PlayLoadingBarSound(AudioSource source)
    {
        if (source == null || loadingBarClip == null) return;
        source.clip = loadingBarClip;
        source.loop = false;
        source.volume = 1f;
        source.Play();
    }

    /// <summary>
    /// Generic coroutine to fade audio
    /// </summary>
    private IEnumerator FadeAudio(AudioSource source, float startVolume, float endVolume, float duration, bool stopAfterFade = false)
    {
        float time = 0f;
        while (time < duration)
        {
            source.volume = Mathf.Lerp(startVolume, endVolume, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        source.volume = endVolume;

        if (stopAfterFade)
            source.Stop();
    }
}





