using UnityEngine;
using UnityEngine.Audio;

public class PlayerAudio : MonoBehaviour
{
    [Header("Audio Source Prefab")]
    public GameObject audioSourcePrefab; // Must have AudioSource component
    public AudioSource playerAudioSource;

    [Header("Audio Parent (for organizing instances)")]
    public Transform audioContainer;

    [Header("Wall Bounce SFX")]
    public AudioClip softBounce;
    public AudioClip hardBounce;
    public AudioClip fatalBounce;
    public AudioClip useStim;

    [Header("Item Interaction SFX")]
    public AudioClip grabItem;
    public AudioClip throwItem;

    [Header("Movement SFX")]
    public AudioClip kickOffWall;

    public AudioMixerGroup playerGroup;

    public void PlaySoftBounce(Vector3 position)
    {
        PlayBounceSoundAtPosition(softBounce, position, 0.3f);
    }

    public void PlayHardBounce(Vector3 position)
    {
        PlayBounceSoundAtPosition(hardBounce, position, 1f);
    }

    public void PlayFatalBounce(Vector3 position)
    {
        PlayBounceSoundAtPosition(fatalBounce, position, 1f);
    }

    public void PlayUseStim()
    {
        playerAudioSource.clip = useStim;
        playerAudioSource.Play();
    }

    public void PlayGrabItem()
    {
        if (grabItem == null) return;
        playerAudioSource.clip = grabItem;
        playerAudioSource.Play();
    }

    public void PlayThrowItem()
    {
        if (throwItem == null) return;
        playerAudioSource.clip = throwItem;
        playerAudioSource.Play();
    }

    public void PlayKickOffWall(Vector3 position)
    {
        if (kickOffWall == null) return;
        
        PlayBounceSoundAtPosition(kickOffWall, position, 0.5f);
    }


    private void PlayBounceSoundAtPosition(AudioClip clip, Vector3 position, float volume)
    {
        if (clip == null || audioSourcePrefab == null) return;

        //Debug.Log("play bounce called");

        GameObject audioObj = Instantiate(audioSourcePrefab, position, Quaternion.identity, audioContainer);
        AudioSource newSource = audioObj.GetComponent<AudioSource>();
        if (newSource == null) return;

        newSource.clip = clip;
        newSource.outputAudioMixerGroup = playerGroup;
        newSource.volume = volume;
        newSource.pitch = (Random.value / 5f) + 0.85f;

        if (clip == kickOffWall)
        {
            newSource.time = .2f;
        }

        newSource.Play();

        Destroy(audioObj, clip.length + 0.1f); // Clean up after sound finishes
    }
}
