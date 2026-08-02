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
    [SerializeField] private GameObject player;
    [SerializeField] private PickupScript pickupScript;
    [SerializeField] private ZeroGravity zeroGravity;
    [SerializeField] private GameObject pauseMenu;
    private float initialEmission;
    private bool holding;

    [SerializeField] private bool tutorialComplete = false;

    public bool extinguisherEquipped = false;
    public GameObject extinguisherGameObj;

    public event System.Action<bool, GameObject> OnFireExtinguisherAcquired;


    public bool HasExtinguisher
    {
        get { return hasExtinguisher; }
        set { hasExtinguisher = value; }
    }

    public bool ExtinguisherEquipped
    {
        get { return extinguisherEquipped; }
        set { extinguisherEquipped = value; }
    }

    public bool TutorialComplete
    {
        get { return tutorialComplete; }
        set { tutorialComplete = value; }
    }

    private void OnEnable()
    {
        sysMain = sys.main;
        sysEmission = sys.emission;
    }

    private void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<ZeroGravity>().gameObject;
            zeroGravity = player.GetComponent<ZeroGravity>();
            pickupScript = player.GetComponent<PickupScript>();
        }
        initialEmission = sysEmission.rateOverTimeMultiplier;
        sysEmission.rateOverTime = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (pauseMenu == null)
            pauseMenu = GameObject.Find("PauseMenu");

        if (holding && canPuff && hasExtinguisher && !pauseMenu.activeSelf == true && !zeroGravity.IsDead)
            StartCoroutine(MakePuff());

        //if (!zeroGravity.IsDead)
        //    if (pickupScript.current != null)
        //        if (pickupScript.current.GetComponent<ExtinguisherObject>() != null && !pickupScript.CanPickUp && (pauseMenu == null || !pauseMenu.activeInHierarchy))
        //            if (pickupScript.current.GetComponent<ExtinguisherObject>().remainingRetardant > 0)
        //                hasExtinguisher = true;
        //            else
        //                hasExtinguisher = false;
        //        else
        //            hasExtinguisher = false;
        //    else
        //        hasExtinguisher = false;
        //else
        //    hasExtinguisher = false;
    }
    System.Collections.IEnumerator MakePuff()
    { //instead of creating nodes, just moving around existing nodes is used again in the hopes it will help with optimization
        if(this.GetComponentInChildren<ExtinguisherObject>() != null &&
            this.GetComponentInChildren<ExtinguisherObject>().remainingRetardant > 0)
        {
            puffObjects[puffCycle].GetComponent<PuffMovement>().Shoot(fireExtinguisherPosition);
            puffCycle++;
            if (puffCycle >= puffObjects.Count)
                puffCycle = 0;
            canPuff = false;
            this.GetComponentInChildren<ExtinguisherObject>().remainingRetardant -= 0.15f;
            sysEmission.rateOverTimeMultiplier = initialEmission;
            yield return new WaitForSeconds(0.15f);
            sysEmission.rateOverTime = 0;
            canPuff = true;
        }
    }

    public void AcquireExtinguisher(GameObject acquiredObj)
    {
        hasExtinguisher = true;
        extinguisherGameObj = acquiredObj;
        OnFireExtinguisherAcquired?.Invoke(true, acquiredObj);
    }

    public void SwapExtinguisher(GameObject obj2)
    {
        extinguisherGameObj.GetComponent<ExtinguisherObject>().DropExtinguisher();
        obj2.GetComponent<ExtinguisherObject>().PickupExtinguisher();
    }

    public void ToggleExtinguisherFromInventory(InputAction.CallbackContext context)
    {
        if(hasExtinguisher && context.performed)
        {
            extinguisherEquipped = !extinguisherEquipped;
            extinguisherGameObj.SetActive(extinguisherEquipped);
        }
    }

    public void OnLeftClick(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            holding = true;
            //Debug.Log("Holding started");
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            holding = false;
            //Debug.Log("Holding ended");
        }
    }
}
