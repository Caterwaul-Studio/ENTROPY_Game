using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FireExtinguisher : MonoBehaviour
{
    [SerializeField] private bool canPuff = true;
    [SerializeField] private bool hasExtinguisher = true;
    [SerializeField] private List<GameObject> puffObjects;
    [SerializeField] private int puffCycle = 0;
    [SerializeField] private Transform fireExtinguisherPosition;
    [SerializeField] private ParticleSystem sys;
    [SerializeField] private ParticleSystem.MainModule sysMain;
    [SerializeField] private ParticleSystem.EmissionModule sysEmission;
    [SerializeField] private PickupScript pickupScript;
    private float initialEmission;
    private bool holding;


    private void OnEnable()
    {
        sysMain = sys.main;
        sysEmission = sys.emission;
    }

    private void Start()
    {
        initialEmission = sysEmission.rateOverTimeMultiplier;
        sysEmission.rateOverTime = 0;
    }

    public void OnThrow(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
            holding = true;
        else if (context.phase == InputActionPhase.Canceled)
            holding = false;
    }

        // Update is called once per frame
    void Update()
    {
        if (holding && canPuff && hasExtinguisher)
            StartCoroutine(MakePuff());

        if (pickupScript.current != null)
            if (pickupScript.current.GetComponent<ExtinguisherObject>() != null && !pickupScript.CanPickUp)
                if (pickupScript.current.GetComponent<ExtinguisherObject>().remainingRetardant > 0)
                    hasExtinguisher = true;
                else
                    hasExtinguisher = false;
            else
                hasExtinguisher = false;
        else
            hasExtinguisher = false;
    }
    System.Collections.IEnumerator MakePuff()
    { //instead of creating nodes, just moving around existing nodes is used again in the hopes it will help with optimization
        puffObjects[puffCycle].GetComponent<PuffMovement>().Shoot(fireExtinguisherPosition);
        puffCycle++;
        if (puffCycle >= puffObjects.Count)
            puffCycle = 0;
        canPuff = false;
        pickupScript.current.GetComponent<ExtinguisherObject>().remainingRetardant -= 0.15f;
        sysEmission.rateOverTimeMultiplier = initialEmission;
        yield return new WaitForSeconds(0.15f);
        sysEmission.rateOverTime = 0;
        canPuff = true;
    }
}
