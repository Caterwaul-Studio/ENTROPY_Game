using UnityEngine;

public class FireAudio : MonoBehaviour
{
    [SerializeField] private AudioSource fireSource;
    [SerializeField] private AudioClip fireLoop;
    [SerializeField] private float fadeOutDuration = 1.5f;

    private Coroutine fade;

    private void Start()
    {
        if (fireSource == null) fireSource = GetComponent<AudioSource>();
        fireSource.clip = fireLoop;
        fireSource.loop = true;
        fireSource.Play();
    }

    // Call this from whatever script extinguishes the fire
    public void Extinguish()
    {
        if (fade != null) StopCoroutine(fade);
        fade = StartCoroutine(FadeOutAndStop());
    }

    private System.Collections.IEnumerator FadeOutAndStop()
    {
        float startVolume = fireSource.volume;
        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            fireSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeOutDuration);
            yield return null;
        }
        fireSource.Stop();
    }
}