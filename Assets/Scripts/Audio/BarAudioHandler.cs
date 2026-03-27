//using Mono.Cecil;
using UnityEngine;

public class BarAudioHandler : MonoBehaviour
{
    //[Header("Audio Sources")]
    //public AudioSource audioSource; // The audio source to play the sounds

    [Header("Grab Sounds")]
    public AudioClip[] grabSounds; // Array of possible grab sounds

    [Header("Release Sounds")]
    public AudioClip[] releaseSounds; // Array of possible release sounds

    public GameObject audioSourcePrefab;
    public Transform audioSourceContainer;

    /// Play a random grab sound.

    public void PlayGrabSound(Vector3 position)
    {
        //Debug.Log(source);
        if (grabSounds.Length == 0) return;

        int index = Random.Range(0, grabSounds.Length);
        PlayBarGrabSoundAtPosition(grabSounds[index], position);
    }
    



    /// Play a random release sound.
  
    public void PlayReleaseSound(Vector3 position)
    {
        //Debug.Log(source);
        if (releaseSounds.Length == 0) return;

        int index = Random.Range(0, releaseSounds.Length);
        PlayBarGrabSoundAtPosition(releaseSounds[index], position);
    }

    private void PlayBarGrabSoundAtPosition(AudioClip clip, Vector3 position)
    {
        if (clip == null || audioSourcePrefab == null) return;

        GameObject audioObj = Instantiate(audioSourcePrefab, position, Quaternion.identity, audioSourceContainer);
        AudioSource newSource = audioObj.GetComponent<AudioSource>();
        if (newSource == null) return;

        newSource.clip = clip;

        newSource.pitch = Random.Range(0.8f, 1.2f);
        newSource.Play();

        Destroy(audioObj, clip.length + 0.1f); // Clean up after sound finishes
    }
}



