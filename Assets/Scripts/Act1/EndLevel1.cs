using System.Collections;
using UnityEngine;

public class EndLevel1 : MonoBehaviour
{
    //[SerializeField]
    //private Collider player;
    [SerializeField]
    private GameObject endLevel1Trigger;
    private DialogueManager dialogueManager;
    //public GameplayBeatAudio audioManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dialogueManager = FindFirstObjectByType<DialogueManager>();     
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartEndLevel1Event()
    {
        StartCoroutine(Level1End());
    }

    public IEnumerator Level1End()
    {
        dialogueManager.StartDialogueSequence(6, 0f);
        yield return new WaitForSeconds(6f);
        endLevel1Trigger.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            StartEndLevel1Event();
        }
    }
}
