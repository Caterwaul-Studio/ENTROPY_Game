using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FireExtinguisher : MonoBehaviour
{
    [SerializeField] private bool canPuff = true;
    [SerializeField] private List<GameObject> puffObjects;
    [SerializeField] private int puffCycle = 0;
    [SerializeField] private Transform fireExtinguisherPosition;
    [SerializeField] private ParticleSystem sys;
    [SerializeField] private ParticleSystem.MainModule sysMain;
    [SerializeField] private ParticleSystem.EmissionModule sysEmission;
    private float initialEmission;


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
        if (canPuff)
            StartCoroutine(MakePuff());
    }

        // Update is called once per frame
    void Update()
    {
    }
    System.Collections.IEnumerator MakePuff()
    { //instead of creating nodes, just moving around existing nodes is used again in the hopes it will help with optimization
        puffObjects[puffCycle].GetComponent<PuffMovement>().Shoot(fireExtinguisherPosition);
        puffCycle++;
        if (puffCycle >= puffObjects.Count)
            puffCycle = 0;
        canPuff = false;
        sysEmission.rateOverTimeMultiplier = initialEmission;
        yield return new WaitForSeconds(0.15f);
        sysEmission.rateOverTime = 0;
        canPuff = true;
    }
}
