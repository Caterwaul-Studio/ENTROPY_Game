using UnityEngine;

// Holds audio for usable/held items (fire extinguisher now, more items later).
// Item scripts hold a reference to this and call the public Play methods.
// AudioSources live on the held item itself (via ExtinguisherAudio) so the
// sound emits from the physical object in the world.
public class UsableItemAudio : MonoBehaviour
{
    [Header("Fire Extinguisher SFX")]
    public AudioClip extinguisherStart;
    public AudioClip extinguisherSustain;   // the 15-second sample
    public AudioClip extinguisherEmpty;

    private Coroutine extinguisherFade;
    private ExtinguisherAudio activeExtinguisher;   // which one is currently spraying

    public void PlayExtinguisherStart(ExtinguisherAudio ext)
    {
        if (extinguisherStart == null || ext == null || ext.startSource == null) return;
        ext.startSource.PlayOneShot(extinguisherStart);
    }

    public void StartExtinguisherSustain(ExtinguisherAudio ext)
    {
        if (extinguisherSustain == null || ext == null || ext.sustainSource == null) return;

        activeExtinguisher = ext;   // remember it so Stop hits the same source
        AudioSource source = ext.sustainSource;

        if (extinguisherFade != null) StopCoroutine(extinguisherFade);

        // First time this particular extinguisher is used: load clip from the start
        if (source.clip != extinguisherSustain)
        {
            source.clip = extinguisherSustain;
            source.time = 0f;
            source.volume = 0f;
        }

        source.Play(); // resumes from wherever THIS extinguisher was paused
        extinguisherFade = StartCoroutine(FadeSource(source, 1f, 0.15f, false));
    }

    public void StopExtinguisherSustain()
    {
        if (activeExtinguisher == null || activeExtinguisher.sustainSource == null) return;
        if (!activeExtinguisher.sustainSource.isPlaying) return;

        if (extinguisherFade != null) StopCoroutine(extinguisherFade);
        extinguisherFade = StartCoroutine(FadeSource(activeExtinguisher.sustainSource, 0f, 0.3f, true));
    }

    public void PlayExtinguisherEmpty(ExtinguisherAudio ext)
    {
        if (extinguisherEmpty == null || ext == null || ext.emptySource == null) return;
        ext.emptySource.PlayOneShot(extinguisherEmpty);
    }

    private System.Collections.IEnumerator FadeSource(AudioSource source, float targetVolume, float duration, bool pauseAtEnd)
    {
        float startVolume = source.volume;
        float t = 0f;
        while (t < duration)
        {
            if (source == null) yield break;   // item may be destroyed mid-fade
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, t / duration);
            yield return null;
        }
        source.volume = targetVolume;
        if (pauseAtEnd) source.Pause(); // Pause keeps the playback position for the resume trick
    }
}