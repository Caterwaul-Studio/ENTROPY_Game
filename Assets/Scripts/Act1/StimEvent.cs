using System.Collections;
using UnityEngine;


public class StimEvent : MonoBehaviour
{

    private DialogueManager manager;
    [SerializeField]
    private StimDispenser dispenser;
    private ZeroGravity playerScript;

    [SerializeField]
    private DoorScript doorToOpen;

    [SerializeField]
    private CanvasGroup stimUseCanvasGroup;
    private string stimUseCanvasGroupObj = "StimTutorialPanel";

    [SerializeField]
    private WristMonitor wristMonitor;

    [Header("Air Breaches")]
    [SerializeField] private AirBreachScript air1;
    [SerializeField] private AirBreachScript air2;
    [SerializeField] private AirBreachScript air3;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        manager = FindFirstObjectByType<DialogueManager>();
        playerScript = FindFirstObjectByType<ZeroGravity>(); 
        stimUseCanvasGroup = FindCanvasGroupByName(stimUseCanvasGroupObj);
        //ensure it sets false on start
        stimUseCanvasGroup.gameObject.SetActive(false);
        stimUseCanvasGroup.alpha = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        //refetch the necessary references if they are null, in case the scene was reloaded or objects were destroyed
        if (wristMonitor == null)
        {
            wristMonitor = PersistantManager.Instance.WristMonitor;
        }
        if(stimUseCanvasGroup == null)
        {
            stimUseCanvasGroup = FindCanvasGroupByName(stimUseCanvasGroupObj);
        }

    }

    public void StartStimEvent()
    {
        StartCoroutine(StimTutorial());
    }

    public IEnumerator StimTutorial()
    {
        manager.StartDialogueSequence(3, 0.5f);

        yield return new WaitForSeconds(6f);
        dispenser.ToggleUsability(true);
        wristMonitor.CompleteObjective();

        yield return new WaitUntil(() => playerScript.NumStims == 3);

        stimUseCanvasGroup.gameObject.SetActive(true);
        StartCoroutine(FadeCanvasGroup(stimUseCanvasGroup, 0f, 1f));

        yield return new WaitUntil(() => playerScript.NumStims < 3);

        wristMonitor.CompleteObjective();
        StartCoroutine(FadeCanvasGroup(stimUseCanvasGroup, 1f, 0f));
        manager.StartDialogueSequence(4, 0.5f);

        yield return new WaitForSeconds(2f);

        yield return StartCoroutine(doorToOpen.PlayDoorAlarm(1.2f));
        doorToOpen.SetState(DoorScript.States.Closed);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha)
    {
        float timeElapsed = 0f;
        float fadeDuration = 1f;

        while (timeElapsed < fadeDuration)
        {
            // Lerp alpha from start to end
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timeElapsed / fadeDuration);
            timeElapsed += Time.deltaTime;
            yield return null; // Wait until the next frame
        }

        canvasGroup.alpha = endAlpha; // Ensure it's set to the final alpha
    }

    public void TriggerBreaches()
    {
        StartCoroutine(TriggerBreachesRoutine());
    }

    private IEnumerator TriggerBreachesRoutine()
    {
        air1.TurnOn();
        yield return new WaitForSeconds(3f);
        air2.TurnOn();
        yield return new WaitForSeconds(0.5f);
        air3.TurnOn();
    }

    private CanvasGroup FindCanvasGroupByName(string name)
    {
        GameObject obj = GameObject.Find(name);
        if (obj != null)
        {
            return obj.GetComponent<CanvasGroup>();
        }
        return null;
    }
}
