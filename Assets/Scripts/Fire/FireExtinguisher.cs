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
    [SerializeField] private UsableItemAudio itemAudio;
    private bool isSpraying;
    private float initialEmission;
    private bool holding;


    private void OnEnable()
    {
        sysMain = sys.main;
        sysEmission = sys.emission;
    }

    private void Start()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<ZeroGravity>().gameObject;
            zeroGravity = player.GetComponent<ZeroGravity>();
            pickupScript = player.GetComponent<PickupScript>();
            itemAudio = player.GetComponent<UsableItemAudio>();
        }
        initialEmission = sysEmission.rateOverTimeMultiplier;
        sysEmission.rateOverTime = 0;
    }

    // public void OnThrow(InputAction.CallbackContext context)
    // {
    //     if (context.phase == InputActionPhase.Started)
    //         holding = true;
    //     else if (context.phase == InputActionPhase.Canceled)
    //         holding = false;
    // }

    public void OnThrow(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {                                                       
            holding = true;
            if (pickupScript != null && pickupScript.current != null)          
            {                                                                  
                ExtinguisherObject ext = pickupScript.current.GetComponent<ExtinguisherObject>(); 
                if (ext != null && ext.remainingRetardant <= 0)               
                    itemAudio.PlayExtinguisherEmpty(pickupScript.current.GetComponent<ExtinguisherAudio>());                         
            }                                                                 
        }                                                       
        else if (context.phase == InputActionPhase.Canceled)
            holding = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (pauseMenu == null)
            pauseMenu = GameObject.Find("PauseMenu");

        if (holding && canPuff && hasExtinguisher)
            StartCoroutine(MakePuff());

        if (!zeroGravity.IsDead)
            if (pickupScript.current != null)
                if (pickupScript.current.GetComponent<ExtinguisherObject>() != null && !pickupScript.CanPickUp && (pauseMenu == null || !pauseMenu.activeInHierarchy))
                    if (pickupScript.current.GetComponent<ExtinguisherObject>().remainingRetardant > 0)
                        hasExtinguisher = true;
                    else
                        hasExtinguisher = false;
                else
                    hasExtinguisher = false;
            else
                hasExtinguisher = false;
        else
            hasExtinguisher = false;
                
        // Watcher: detect the moment spraying actually starts or stops      
        bool sprayingNow = holding && hasExtinguisher;                       
        if (sprayingNow && !isSpraying)                                      
        {               
            ExtinguisherAudio extAudio = pickupScript.current.GetComponent<ExtinguisherAudio>();                                                      // <-- NEW
            itemAudio.PlayExtinguisherStart(extAudio);                                
            itemAudio.StartExtinguisherSustain(extAudio);                             
        }                                                                     
        else if (!sprayingNow && isSpraying)                                  
        {                                                                    
            itemAudio.StopExtinguisherSustain();                              
        }                                                                     
        isSpraying = sprayingNow;    
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
