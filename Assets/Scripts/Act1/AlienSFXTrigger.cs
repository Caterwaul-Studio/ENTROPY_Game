using System.Collections;
using UnityEngine;

public class AlienSFXTrigger : MonoBehaviour
{
    [SerializeField]
    private AudioSource source;

    [SerializeField]
    private GameObject moveToObj;

    [SerializeField]
    private bool useMoveTo = true;

    [SerializeField]
    private AudioClip alienSFXClip;

    [SerializeField]
    private float moveDuration = 2f;

    private Vector3 moveToPos;

    void Start()
    {
        if (moveToObj != null)
            moveToPos = moveToObj.transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GetComponent<Collider>().enabled = false;
            if(useMoveTo)
            {
                StartCoroutine(PlayAndMoveSound());
            }
            else
            {
                source.clip = alienSFXClip;
                if (source.isActiveAndEnabled)
                {
                    source.Play();
                }
            }
            
        }
    }

    private IEnumerator PlayAndMoveSound()
    {
        if (source == null || alienSFXClip == null)
            yield break;

        source.clip = alienSFXClip;
        if (source.isActiveAndEnabled)
        {
            source.Play();
        }

        Vector3 startPos = source.transform.position;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            source.transform.position = Vector3.Lerp(startPos, moveToPos, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure it reaches the final position exactly
        source.transform.position = moveToPos;
    }
}
