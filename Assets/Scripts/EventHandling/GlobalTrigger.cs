using UnityEngine;
using UnityEngine.Events;

public class GlobalTrigger : MonoBehaviour
{

    [Header("Events")]
    //The method that this trigger calls when it's triggered.
    public UnityEvent triggeredEvent;

    //Specify a specialized method that is called when useLoadEvent is true, only when the scene is reloaded and the trigger is recalled
    public UnityEvent loadEvent;

    [Header("Save Data")]
    
    public bool isTriggered = false;

    //When reload the scene and this trigger has already been set off, call the behavior of this trigger again?
    public bool recallUponSceneLoad;

    //When trigger recalled, use the triggered event or the specialized load event?
    public bool useLoadEvent;

    private Collider triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (isTriggered)
        {
            triggerCollider.enabled = false;
            if(recallUponSceneLoad)
                RecallTriggeredBehavior();
        }
    }

    /// <summary>
    /// Simple method detects if the player enters the trigger and then disables it and invokes the method that the trigger is supposed to do.
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered || !other.CompareTag("Player")) return;

        isTriggered = true;
        this.GetComponent<Collider>().enabled = false;
        triggeredEvent?.Invoke();
    }

    public void ResetTrigger()
    {
        triggerCollider.enabled = true;
        isTriggered = false;
    }

    public void Deactivate()
    {
        triggerCollider.enabled = false;
    }

    /// <summary>
    /// If we want the trigger to recall its behavior it will do this
    /// </summary>
    public void RecallTriggeredBehavior()
    {
        if (useLoadEvent)
        {
            if (loadEvent.GetPersistentEventCount() > 0)
            {
                loadEvent.Invoke();
            }
            else
            {
                Debug.LogError($"No load event for {this.name} has been specified.");
            }
        }
        else
        {
            triggeredEvent?.Invoke();
        }
    }
}
