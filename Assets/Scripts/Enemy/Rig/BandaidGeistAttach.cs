using UnityEngine;

public class BandaidGeistAttach : MonoBehaviour
{
    public GameObject geistRigPrefab;
    [SerializeField] public GeistDirectorBrain brain;
    [SerializeField] private GameObject geistRig;
    [SerializeField] private Transform[] children;
    [SerializeField] private float rigYawOffset = 0f; // tweak in Inspector until the eye faces the walk direction

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        geistRig = Instantiate(geistRigPrefab, Vector3.zero, Quaternion.identity, null);
        if (brain == null)
            brain = this.gameObject.GetComponentInParent<GeistDirectorBrain>();

        brain.GeistRig = geistRig.GetComponentInChildren<GeistStepper>();
        brain.GeistEyeRig = geistRig.GetComponentInChildren<GeistLookAt>();
        brain.GeistEye = brain.GeistEyeRig.coneOrigin;
        brain.coneHalfAngle = brain.GeistEyeRig.coneHalfAngle;
        brain.geistEyeRangeMax = brain.GeistEyeRig.detectionRange;

        children = geistRig.GetComponentsInChildren<Transform>();

        // holy bandaid
        children[1].parent = this.transform;
        children[1].transform.localPosition = Vector3.zero;
        children[1].localRotation = Quaternion.Euler(0f, rigYawOffset, 0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
