using System.Collections;
using UnityEngine;

public class BodyScareEvent : MonoBehaviour, ISaveable
{
    private ZeroGravity player;
    [SerializeField]
    private DoorScript bodyDoor;
    [SerializeField]
    private DoorScript[] doorsToUnlock;
    [SerializeField]
    private Collider[] barsToGrab;
    [SerializeField]
    private GameObject body;
    [SerializeField]
    private Rigidbody bodyRb;

    [SerializeField]
    private GameObject tempCollider;
    //this trigger box is for when the player returns to the dining room after the dead body reveal
    [SerializeField]
    private GameObject colliderTriggerEnd;
    private DialogueManager dialogueManager;

    public bool waitForGrabbingBar = false;

    public Light[] escapePodLights;

    public AnimationCurve lightCurve;

    public float intensityMultiplier;

    public GameObject bodyPos;

    public GameplayBeatAudio audioManager;

    private AmbientController ambientController;

    public LightManager lightManager;

    [SerializeField]
    private GameObject[] sparksToDisable;

    private bool eventTriggered;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dialogueManager = FindFirstObjectByType<DialogueManager>();
        ambientController = FindFirstObjectByType<AmbientController>();
        player = FindFirstObjectByType<ZeroGravity>();
        //ensure that the collider is set inactive so we don't have the end dialogue play until we complete the body scare sequence
        colliderTriggerEnd.SetActive(false);

        foreach (Light light in escapePodLights)
        {
            light.intensity = 0;
        }

        if (GlobalSaveManager.LoadFromSave) GlobalSaveManager.LoadSavable(this, false);

        if (eventTriggered)
        {
            body.SetActive(true);
            body.transform.position = bodyPos.transform.position;
            bodyRb.isKinematic = false;
            //turn off the collider that keeps the player from moving through the door prematurely.
            tempCollider.SetActive(false);
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (waitForGrabbingBar)
        {
            foreach (Collider bar in barsToGrab)
            {
                if (player.GrabbedBar != null)
                {
                    //Debug.Log(player.GrabbedBar.name);
                }

                if (bar == player.GrabbedBar)
                {
                    //Debug.Log("Grabbed bar detected");
                    waitForGrabbingBar = false;
                    eventTriggered = true;
                    StartCoroutine(PlayBodyScare());
                }
            }
        }
    }

    public void SetOffBarCheck()
    {
        waitForGrabbingBar = true;
    }

    public IEnumerator PlayBodyScare()
    {


        yield return new WaitForSeconds(2.0f);

        //bodyRb.AddForce(new Vector3(-.5f, -1, 0) * 30f, ForceMode.Impulse);

        // disable sparks on door open
        foreach (GameObject spark in sparksToDisable)
        {
            spark.SetActive(false);
        }

        bodyDoor.SetState(DoorScript.States.JoltOpen);


        //put any body movement Logic here;


        yield return new WaitUntil(() => bodyDoor.aboutToJolt == true);

        body.SetActive(true);
        body.transform.position = bodyPos.transform.position;
        bodyRb.isKinematic = false;
        //turn off the collider that keeps the player from moving through the door prematurely.
        tempCollider.SetActive(false);

        //bodyRb.AddForce(new Vector3(0f, -1f, 0f) * .5f, ForceMode.Impulse);

        //foreach (Rigidbody rb in bodyRb.GetComponentsInChildren<Rigidbody>())
        //{
        //    rb.AddForce(new Vector3(0f, -1f, 0f) * 5f, ForceMode.Impulse);
        //}

        //yield return new WaitUntil(() => bodyDoor.showingBody);

        yield return new WaitForSeconds(1.5f);
        ActivateLights();

        //ambientController.Progress();
    }

    public void ActivateLights()
    {
        StartCoroutine(TurnOnLights(6));
    }

    //public IEnumerator TurnOnLights(float duration)
    //{
    //    float elapsed = 0f;
    //    audioManager.playBodyStinger();

    //    while (elapsed < duration)
    //    {
    //        elapsed += Time.deltaTime;
    //        float t = elapsed / duration;
    //        float intensity = lightCurve.Evaluate(t) * intensityMultiplier;

    //        foreach (Light light in escapePodLights)
    //        {
    //            light.intensity = intensity;
    //        }

    //        yield return null;
    //    }


    //    // Ensure final value is set
    //    float finalIntensity = lightCurve.Evaluate(1f) * intensityMultiplier;
    //    foreach (Light light in escapePodLights)
    //    {
    //        light.intensity = finalIntensity;
    //    }

    //    foreach (DoorScript door in doorsToUnlock)
    //    {
    //        door.SetState(DoorScript.States.Closed);
    //    }

    //    yield return new WaitForSeconds(5.5f);

    //    dialogueManager.StartDialogueSequence(8, 0f);

    //    yield return new WaitForSeconds(5f);
    //}

    public IEnumerator TurnOnLights(float duration)
    {
        //float elapsed = 0f;

        //audioManager.playBodyStinger();

        yield return StartCoroutine(lightManager.FlickerLights(LightLocation.EscapePod, duration, 3.0f, false));
        StartCoroutine(lightManager.FlickerLightsForever(LightLocation.EscapePod));
        //yield return StartCoroutine(lightManager.MultiplyAllLights(LightLocation.EscapePod, 2.0f, 0.7f));
        //yield return StartCoroutine(lightManager.FadeOutAllLights(LightLocation.EscapePod, 0.0f, 0.3f));

        foreach (DoorScript door in doorsToUnlock)
        {
            door.SetState(DoorScript.States.Closed);
        }

        
        yield return new WaitForSeconds(3.5f);
        //where we use to play the dialogue we will now instead make the trigger for that dialogue active in the dining room
        colliderTriggerEnd.SetActive(true);

        yield return new WaitForSeconds(5f);
    }

    public class BodyScareEventData
    {
        public bool eventTriggered;

        public BodyScareEventData(bool triggered)
        {
            eventTriggered = triggered;
        }
    }

    
    public void LoadSaveFile(string fileName)
    {
        string path = Application.persistentDataPath;
        string loadedData = GlobalSaveManager.LoadTextFromFile(path, fileName);
        if (loadedData != null && loadedData != "")
        {
            BodyScareEventData _bodyScareEventData = JsonUtility.FromJson<BodyScareEventData>(loadedData);
            eventTriggered = _bodyScareEventData.eventTriggered;
        }
    }

    public void CreateSaveFile(string fileName)
    {
        BodyScareEventData _bodyScareEventData = new BodyScareEventData(eventTriggered);

        string json = JsonUtility.ToJson(_bodyScareEventData);
        string path = Application.persistentDataPath;
        GlobalSaveManager.SaveTextToFile(path, fileName, json);
    }
}
