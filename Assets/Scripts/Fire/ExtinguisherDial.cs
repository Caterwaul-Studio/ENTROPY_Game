using UnityEngine;

public class ExtinguisherDial : MonoBehaviour
{
    [SerializeField] ExtinguisherObject extinguisherObject;

    private float scalar = 24f;
    private float maxAngle = 12.5f;

    [SerializeField] private float lerpSpeed = 5f; // higher = snappier, lower = smoother/slower

    private Quaternion targetRotation;
    private Quaternion currentRot;

    private Quaternion lastRot;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentRot = transform.localRotation;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (extinguisherObject.enabled)
        {
            UpdateDial();
        }
    }

    private void UpdateDial()
    {

        float targetZ = Mathf.Clamp(extinguisherObject.remainingRetardant * scalar, maxAngle, 360f);
        targetRotation = currentRot * Quaternion.Euler(0f, 0f, targetZ);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            lerpSpeed * Time.deltaTime
        );
    }
}
