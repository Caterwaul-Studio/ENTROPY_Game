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
    }

    // Update is called once per frame
    void Update()
    {
        
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
}
