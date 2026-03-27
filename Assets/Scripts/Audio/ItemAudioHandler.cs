using UnityEngine;

public enum ItemType
{
    MetalSheet,
    PlasticBin,
    SoftBox
}

public class ItemAudioHandler : MonoBehaviour
{
    [Header("Grab Sounds")]
    public AudioClip[] pickupSounds;

    [Header("Throw Sounds")]
    public AudioClip[] throwSounds;

    public GameObject audioSourcePrefab;
    public Transform audioSourceContainer;

    [Header("Impact Settings")]
    public float lightImpactThreshold = 1.5f;  // below = light, above = hard

    [Header("Metal Sheet")]
    public AudioClip[] metalLightImpacts;
    public AudioClip[] metalHardImpacts;

    [Header("Plastic Bin")]
    public AudioClip[] plasticLightImpacts;
    public AudioClip[] plasticHardImpacts;

    [Header("Soft Box")]
    public AudioClip[] softLightImpacts;
    public AudioClip[] softHardImpacts;

    public void PlayImpactSound(ItemType type, float velocity, Vector3 position)
    {
        AudioClip[] clips = null;

        bool hardImpact = velocity >= lightImpactThreshold;

        switch (type)
        {
            case ItemType.MetalSheet:
                clips = hardImpact ? metalHardImpacts : metalLightImpacts;
                break;

            case ItemType.PlasticBin:
                clips = hardImpact ? plasticHardImpacts : plasticLightImpacts;
                break;

            case ItemType.SoftBox:
                clips = hardImpact ? softHardImpacts : softLightImpacts;
                break;
        }

        if (clips == null || clips.Length == 0) return;

        int index = Random.Range(0, clips.Length);
        PlayOneShot(clips[index], position);
    }

    private void PlayOneShot(AudioClip clip, Vector3 position)
    {
        if (!clip || !audioSourcePrefab) return;

        GameObject obj = Instantiate(audioSourcePrefab, position, Quaternion.identity, audioSourceContainer);
        AudioSource src = obj.GetComponent<AudioSource>();

        src.clip = clip;
        src.pitch = Random.Range(0.85f, 1.15f);

        src.Play();
        Destroy(obj, clip.length + 0.1f);
    }
    public void PlayPickUpSound(Vector3 position)
    {
        if (pickupSounds.Length == 0) return;

        int index = Random.Range(0, pickupSounds.Length);
        PlaySoundAtPosition(pickupSounds[index], position);
    }

    public void PlayThrowSound(Vector3 position)
    {
        if (throwSounds.Length == 0) return;

        int index = Random.Range(0, throwSounds.Length);
        PlaySoundAtPosition(throwSounds[index], position);
    }

    private void PlaySoundAtPosition(AudioClip clip, Vector3 position)
    {
        if (clip == null || audioSourcePrefab == null) return;

        GameObject tempAudioObj =
            Instantiate(audioSourcePrefab, position, Quaternion.identity, audioSourceContainer);

        AudioSource tempSource = tempAudioObj.GetComponent<AudioSource>();

        if (tempSource == null)
        {
            Debug.LogError("Audio prefab missing AudioSource!");
            Destroy(tempAudioObj);
            return;
        }

        tempSource.clip = clip;
        tempSource.pitch = Random.Range(0.8f, 1.2f);
        tempSource.spatialBlend = 1f; // Ensure 3D sound
        tempSource.Play();

        Destroy(tempAudioObj, clip.length + 0.1f);
    }
}




/*private void OnCollisionEnter(Collision collision)
{
    // Prevent sounds when object is being held
    if (GetComponent<Rigidbody>()?.isKinematic == true)
        return;

    // Only play if above threshold
    if (collision.relativeVelocity.magnitude >= impactThreshold)
    {
        PlayImpactSound();
    }
}

public void PlayImpactSound()
{
    if (impactSounds.Length == 0 || audioSource == null) return;
    int index = Random.Range(0, impactSounds.Length);
    audioSource.pitch = Random.Range(0.9f, 1.1f);
    audioSource.PlayOneShot(impactSounds[index]);
}*/

//Code for FloatingObject script(s) once new objects have been added://
/*public enum ItemType
{
    MetalSheet,
    PlasticBin,
    SoftBox
}

public class FloatingObject : MonoBehaviour
{
    public ItemType itemType;
    public ItemAudioHandler audioHandler;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (audioHandler == null) return;

        float impactSpeed = collision.relativeVelocity.magnitude;

        // Prevent tiny micro-sounds
        if (impactSpeed < 0.2f) return;

        Vector3 hitPoint = collision.contacts[0].point;

        audioHandler.PlayImpactSound(itemType, impactSpeed, hitPoint);
    }
}*/
