using UnityEngine;
using System.Collections.Generic;

public class LabFireEvent : MonoBehaviour
{
    [SerializeField] List<FireNodeScript> fireNodes;
    [SerializeField] DoorScript labDoor;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartFire()
    {
        Debug.Log("Fire Started");
        foreach (FireNodeScript node in fireNodes)
        {
            node.gameObject.SetActive(true);
        }

        labDoor.DoorState = DoorScript.States.Locked;
    }
}
