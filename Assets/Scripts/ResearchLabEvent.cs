using System.Collections;
using UnityEngine;

public class ResearchLabSequence : MonoBehaviour {
    //Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        
    }

    //Update is called once per frame
    void Update() {
        
    }

    //Method called by the terminal when it finishes uploading
    public async void startLabSequence() {
        Debug.Log("Start Lab Sequence Called.");

        //Awaits for 3 seconds, destroys cancellation token for memory purposes
        await Awaitable.WaitForSecondsAsync(3f, destroyCancellationToken);

        StartCoroutine(lightsSequence());
        StartCoroutine(startFires());
    }

    //Function that flickers back on the lights
    private IEnumerator lightsSequence() {
        Debug.Log("Lights Sequence Called.");

        yield return null;
    }

    //Function that starts fire in the research lab carousel
    private IEnumerator startFires() {
        Debug.Log("Start Fires Called.");

        yield return null;
    }
}