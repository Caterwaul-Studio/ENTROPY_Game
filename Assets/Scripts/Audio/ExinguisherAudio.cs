using UnityEngine;

// Sits on the fire extinguisher prefab. Holds that specific extinguisher's
// AudioSources so the sound emits from the physical object in the world.
public class ExtinguisherAudio : MonoBehaviour
{
    public AudioSource startSource;
    public AudioSource sustainSource;
    public AudioSource emptySource;
}
