using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach this to any UI Button to give it hover and click sounds.
/// Drag an AudioSource into the slot (the wrist monitor's UI Audio Source works great),
/// then drag your hover and click clips in. Reusable on any button in the game.
/// </summary>
public class WristMonitorUIButtonSounds : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Audio")]
    [Tooltip("Which AudioSource plays the sounds. If left empty, it searches parent objects for one.")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;

    private void Awake()
    {
        // Fallback: if no AudioSource was dragged in, look for one on this object or its parents
        if (audioSource == null)
        {
            audioSource = GetComponentInParent<AudioSource>();
        }
    }

    // Called automatically by Unity when the mouse enters this button
    public void OnPointerEnter(PointerEventData eventData)
    {
        PlaySound(hoverSound);
    }

    // Called automatically by Unity when this button is clicked
    public void OnPointerClick(PointerEventData eventData)
    {
        PlaySound(clickSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
