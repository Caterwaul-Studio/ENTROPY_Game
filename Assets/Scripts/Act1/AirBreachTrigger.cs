using System.Collections;
using UnityEngine;

public class AirBreachTrigger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public AirBreachScript breachScript;
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (!breachScript.IsCurrentlyBlowing) return;

        if (other.CompareTag("PickupObject") || other.CompareTag("Player"))
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null && !breachScript.affectedRigidbodies.Contains(rb))
            {
                breachScript.affectedRigidbodies.Add(rb);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null)
        {
            breachScript.affectedRigidbodies.Remove(rb);
        }
    }
}
