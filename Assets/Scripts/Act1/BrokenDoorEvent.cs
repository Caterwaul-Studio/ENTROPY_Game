using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

public class BrokenDoorEvent : MonoBehaviour
{
    [SerializeField]
    DoorScript brokenDoor;
    [SerializeField]
    LightManager lightManager;
    [SerializeField]
    float delay = 0f;
    [SerializeField]
    private AudioSource brokenDoorAudio;
    [SerializeField]
    private AudioSource shootAudio;
    [SerializeField]
    private AudioSource lightOnAudio;
    private DialogueManager manager;
    private WristMonitor monitor;
    public StingerManager stingerManager;
    [SerializeField] private Checkpoint diningCheckpoint;

    [SerializeField] private Transform floatingObjectContainer;
    [SerializeField] private GameObject sparksToEnable;
    [SerializeField] private Transform dispenserDoor;
    [SerializeField] private Transform canisterPosition;
    [SerializeField] private GameObject canisterPrefab;
    [SerializeField] private int dispenseAmount = 10;
    [SerializeField] private BoxCollider[] collidersToDisable;
    private Queue<GameObject> canisterQueue = new Queue<GameObject>();


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        manager = FindFirstObjectByType<DialogueManager>();
        monitor = FindFirstObjectByType<WristMonitor>();

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartBrokenDoorBeat()
    {
        //StartCoroutine(BrokenDoorBeat());
        stingerManager.BrokenDoorStingerTriggered();
        //flicker lights
        StartCoroutine(lightManager.FlickerLights(LightLocation.Dining, 2.0f, 2.5f, true));
        StartCoroutine(MalfunctionDispenser());
        lightOnAudio.Play();

        manager.OnDialogueLineEndBrokenDoor += OnLastDialogueLineBrokenDoorStarted;
        manager.StartDialogueSequence(5, delay);
    }

    private void OnLastDialogueLineBrokenDoorStarted(int sequenceIndex)
    {
        //hard code that we are looking for a line in the broken door sequence
        if (sequenceIndex != 5) return;
        //unregister event so it doesn't get called again
        manager.OnDialogueLineEndBrokenDoor -= OnLastDialogueLineBrokenDoorStarted;
        StartCoroutine(BrokenDoorBeat());
    }

    private IEnumerator BrokenDoorBeat()
    {
        brokenDoorAudio.Play();
        yield return new WaitForSeconds(3f);
        brokenDoor.SetState(DoorScript.States.Broken);
        diningCheckpoint.TriggerCheckpointManually();
        monitor.CompleteObjective();
    }

    private IEnumerator MalfunctionDispenser()
    {

        yield return new WaitForSeconds(8f);
        sparksToEnable.active = true;

        while (true)
        {

            if (canisterQueue.Count >= dispenseAmount)
            {
                GameObject first = canisterQueue.Dequeue();
                Destroy(first);
            }

            yield return StartCoroutine(InstantiateCanister());

            yield return new WaitForSeconds(Random.Range(5, 10));


        }
    }

    private IEnumerator InstantiateCanister()
    {

        GameObject canisterInstance = GameObject.Instantiate(canisterPrefab, canisterPosition.position, canisterPosition.rotation, floatingObjectContainer);
        canisterQueue.Enqueue(canisterInstance);

        BoxCollider collider = canisterInstance.GetComponent<BoxCollider>();
        Rigidbody rb = canisterInstance.GetComponent<Rigidbody>();

        foreach (BoxCollider b in collidersToDisable)
        {
            Physics.IgnoreCollision(collider, b, true);
        }

        //play audio of shooting/launching a canister from the foodchute
        shootAudio.Play();

        yield return StartCoroutine(AnimateShootDoor(90f, 0.2f));

        float randForce = Random.Range(400, 800);
        float randTorque = Random.Range(5, 15);
        rb.AddForce(new Vector3(1f, 0.5f, 0) * randForce);
        rb.AddTorque(new Vector3(0, 0, -1) * randTorque);
        
        yield return new WaitForSeconds(2f);
        foreach (BoxCollider b in collidersToDisable)
        {
            Physics.IgnoreCollision(collider, b, false);
        }

        yield return StartCoroutine(AnimateShootDoor(0f, 0.2f));

    }

    private IEnumerator AnimateShootDoor(float rotationAngle, float speed)
    {
        Quaternion startRot = dispenserDoor.rotation;
        Quaternion endRot = Quaternion.Euler(dispenserDoor.eulerAngles.x, rotationAngle, dispenserDoor.eulerAngles.z);


        float t = 0f;
        while (t < speed)
        {
            t += Time.deltaTime;
            dispenserDoor.rotation = Quaternion.Lerp(startRot, endRot, t / speed);
            yield return null;
        }

        dispenserDoor.rotation = endRot; // ensure final rotation
    }
}
