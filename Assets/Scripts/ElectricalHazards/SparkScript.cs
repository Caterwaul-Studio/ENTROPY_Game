using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Controls;


public class SparkScript : MonoBehaviour
{
    [SerializeField] private Bounds sparkBounds;
    [SerializeField] private GameObject player;
    [SerializeField] private ZeroGravity zeroG;
    [SerializeField] private GameObject myGrabbable;
    [SerializeField] private Camera MainCamera;
    [SerializeField] private ParticleSystem sys;
    [SerializeField] private List<FlickerLight> flickerLights;
    [SerializeField] private bool WireSpark;
    public bool electricActive;
    public bool prevElectricActive = false;

    private Vector3 boxSize;
    private float damageCoolDown;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (player == null || MainCamera == null || zeroG == null)
        {
            player = GameObject.FindAnyObjectByType<ZeroGravity>().gameObject;
            zeroG = player.GetComponent<ZeroGravity>();
            MainCamera = zeroG.cam;
        }
        boxSize = this.gameObject.GetComponent<BoxCollider>().size;
        sparkBounds = new Bounds(transform.position, boxSize);
        Destroy(this.gameObject.GetComponent<BoxCollider>());

        if (myGrabbable != null) //the behavior for damaging the player when they grab onto an electrified bar is handled in ZeroGravity.cs
            myGrabbable.AddComponent<SparkBar>(); //it is dependent on the object with the grabbable tag having this component on it
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null || MainCamera == null || zeroG == null)
        {
            player = GameObject.FindAnyObjectByType<ZeroGravity>().gameObject;
            zeroG = player.GetComponent<ZeroGravity>();
            MainCamera = zeroG.cam;
        }

        if (zeroG.hasGloves && zeroG.IsGrabbing)
            damageCoolDown = 0;

        damageCoolDown += Time.deltaTime;
        //enable the lights
        if (!prevElectricActive && electricActive)
        {
            foreach (FlickerLight light in flickerLights)
            {
                light.lightActive = true;
                //enable the flicker
                light.flickerActive = true;
            }
        }
        if (electricActive)
        {

            if (WireSpark)
                sparkBounds = new Bounds(transform.position, boxSize); //updating the position of the bounds is only relevant for wires which move around
            else
                if (sparkBounds.Contains(player.transform.position) && damageCoolDown > 1f && sys.particleCount > 0)
                    DamagePlayer(); //the player is shocked when they are within the bounds and the niagara has active particles
        }
        else
        {
            //disable the lights
            if (prevElectricActive)
            {
                foreach (FlickerLight light in flickerLights)
                {
                    light.lightActive = false;
                    //enable the flicker
                    light.flickerActive = false;
                }
            }
            prevElectricActive = electricActive;
        }
        prevElectricActive = electricActive;
    }

    private void DamagePlayer()
    {
        if ((!zeroG.hasGloves || (zeroG.hasGloves && !zeroG.IsGrabbing)) && !zeroG.PlayerFreeMoveNoClip)
        { //we dont want to damage the player if they either dont have the gloves or they do have them but arent currently grabbing a bar
            zeroG.DecreaseHealth(1);
            StartCoroutine(zeroG.ShockEffect());
            damageCoolDown = 0;
            zeroG.GetThrown(MainCamera.transform.forward * -1, 8);
            if (zeroG.IsGrabbing)
                zeroG.ReleaseBar();
        }
    }
}
