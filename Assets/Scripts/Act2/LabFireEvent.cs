using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

public class LabFireEvent : MonoBehaviour
{
    [SerializeField] List<FireNodeScript> fireNodes;
    [SerializeField] DoorScript labDoor;
    private bool eventComplete;
    private bool eventRunning;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        eventComplete = false;
        eventRunning = false;
    }

    // Update is called once per frame
    void Update()
    {
        // only run the fire checks when the event is running but not yet complete
        if (!eventComplete && eventRunning)
        {
            // count the number of active fires
            int numActiveFires = 0;

            foreach (FireNodeScript node in fireNodes)
            {
                if (node.fireActive)
                {
                    numActiveFires++;
                }
            }

            Debug.Log("Number of Fires: " + numActiveFires);

            // if there are no fires, end the event and unlock the door
            if (numActiveFires < 1)
            {
                labDoor.DoorState = DoorScript.States.Closed;
                eventComplete = true;

                foreach (FireNodeScript node in fireNodes)
                {
                    node.gameObject.SetActive(false);
                }

                Debug.Log("LabFireEventComplete");
            }
        }
    }

    /// <summary>
    /// Turns on the fire nodes. called from Lab Terminal's OnUploadComplete() 
    /// </summary>
    public void StartFire()
    {
        // enable all fire nodes
        Debug.Log("Fire Started");
        eventRunning = true;
        foreach (FireNodeScript node in fireNodes)
        {
            node.gameObject.SetActive(true);
        }

        labDoor.DoorState = DoorScript.States.Locked;
    }
}
