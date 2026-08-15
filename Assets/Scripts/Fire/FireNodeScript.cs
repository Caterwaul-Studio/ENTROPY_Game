using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

public class FireNodeScript : MonoBehaviour
{
    [SerializeField] private Bounds fireBounds;
    [SerializeField] private Bounds playerBounds;
    [SerializeField] private FireScript myFire;
    [SerializeField] private GameObject myLight;
    private Light myLightComponent;
    private float flickerSeed;
    [Header("FlickerSettings")]
    [SerializeField] private float flickerSpeed = 8f;
    [SerializeField] private float flickerIntensityRange = 0.15f;

    [Header("References")]
    [SerializeField] private PersistantManager persistantManager;
    [SerializeField] private GameObject player;
    [SerializeField] private Camera MainCamera;
    [SerializeField] private PuffMovement[] extinguishNodes;

    [Header("Fire Settings")]
    [SerializeField] private int flameSteady; //should be 1-5
    [SerializeField] private int regenRate;
    [SerializeField] private float flameLoss; //this variable is how much strength the flame loses every time it gets hit.
    [SerializeField] private float flameStrength;
    [SerializeField] private float playerHitBoxSizeModifier;
    [SerializeField] private ParticleSystem sys;
    [SerializeField] private ParticleSystem.MainModule sysMain;
    [SerializeField] private ParticleSystem.EmissionModule sysEmission;
    [SerializeField] private ParticleSystem.ShapeModule sysShape;
    [SerializeField] private ParticleSystem.ColorOverLifetimeModule sysCOL;
    [SerializeField] private ParticleSystem.SizeOverLifetimeModule sysSOL;
    [SerializeField] private ParticleSystem.LightsModule sysLight;


    [SerializeField] public bool fireActive;

    //Particle system original param containers
    private float originalStartSize;
    private float originalShapeRadius;
    private float throwCooldown;

    private void OnEnable()
    {
        sysMain = sys.main;
        sysEmission = sys.emission;
        sysShape = sys.shape;
        sysCOL = sys.colorOverLifetime;
        sysSOL = sys.sizeOverLifetime;
        sysLight = sys.lights;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (persistantManager == null)
        {
            persistantManager = FindFirstObjectByType<PersistantManager>();
            player = persistantManager.PlayerObject;
            MainCamera = persistantManager.MainCamera;
        }

        StartCoroutine(SetPuffs());

        //this method is similar to the audiozone method however this time the boxcolider size is used instead of the object scale
        //the reason for this difference is because changing the object scale is not arbitrary for fire nodes, it will affect the size of their particle systems
        fireBounds = new Bounds(transform.position,this.gameObject.GetComponent<BoxCollider>().size);
        playerBounds = new Bounds(transform.position, this.gameObject.GetComponent<BoxCollider>().size * playerHitBoxSizeModifier);
        Destroy(this.gameObject.GetComponent<BoxCollider>());
        //please edit the fireBounds via editing the box collider, do not change the center of the box collider, just move the whole object

        //setting original params
        originalStartSize = sysMain.startSize.constant;
        originalShapeRadius = sysShape.radius;

        myLightComponent = myLight.GetComponent<Light>();
        flickerSpeed = Random.Range(0f, 10f);
    }

    // Update is called once per frame
    void Update()
    {
        if (persistantManager == null)
        {
            persistantManager = FindFirstObjectByType<PersistantManager>();
            player = persistantManager.PlayerObject;
            MainCamera = persistantManager.MainCamera;
        }

        if (!extinguishNodes.Contains(null))
        {
            if (fireActive)
            {
                throwCooldown += Time.deltaTime;
                for (int i = 0; i < extinguishNodes.Length; i++)
                { //collision detection is done via fireBounds in the hope it will be less resource intensive, may need to be tested
                    if (fireBounds.Contains(extinguishNodes[i].transform.position))
                        DampenFlame(extinguishNodes[i].gameObject);
                }

                if (playerBounds.Contains(player.transform.position) && throwCooldown > 0.5f)
                {
                    StartCoroutine(BurnPlayer());
                    player.GetComponent<ZeroGravity>().GetThrown(MainCamera.transform.forward * -1, 5);
                    throwCooldown = 0;
                }

                //flame regeneration
                if (flameStrength < flameSteady * 20)
                    flameStrength += Time.deltaTime * regenRate;

                if (flameStrength > 81)
                {
                    flameSteady = 5;
                }
                else if (flameStrength > 61)
                {
                    flameSteady = 4;
                }
                else if (flameStrength > 41)
                {
                    flameSteady = 3;
                }
                else if (flameStrength > 21)
                {
                    flameSteady = 2;
                }
                else if (flameStrength > 11)
                {
                    flameSteady = 1;
                }
            }
        }
        if (fireActive)
            UpdateFlicker();
    }

    private void UpdateFlicker()
    {
        float baseIntensity = flameStrength / 1000f;
        float noise = Mathf.PerlinNoise(flickerSpeed, Time.time * flickerSpeed);
        float flickerOffset = (noise - 0.5f) * flickerIntensityRange * baseIntensity;
        myLightComponent.intensity = baseIntensity + flickerOffset;
    }

    private void DampenFlame(GameObject other)
    {
        flameStrength -= flameLoss; //dampening
        ChangeFlame();
        other.transform.position = new Vector3(0, 0, 0);
        if (flameStrength < 1) //when flame strength is under a certain value, destroy the flame node
        {
            fireActive = false;
            sysMain.startSize = 0;

            /* //old method
            myFire.myFireNodes.Remove(this.gameObject); //update the parent with the destruction of the node
            Destroy(this.gameObject);
            */
        }
    }

    System.Collections.IEnumerator BurnPlayer()
    {
        player.GetComponent<ZeroGravity>().DecreaseHealth(1);
        yield return new WaitForSeconds(3f);
        player.GetComponent<ZeroGravity>().DecreaseHealth(1);
    }
    private void ChangeFlame()
    {
        //all changes to the flame particle system are to happen here
        sysMain.startSize = originalStartSize * (flameStrength / 100);
        sysShape.radius = originalShapeRadius * (flameStrength / 100);
    }

    public void Reignite(int reigniteLevel) //this function is to be used by other scripts to reignite the flame
    {
        Mathf.Clamp(reigniteLevel, 0, 5);
        flameStrength = reigniteLevel * 20;
        fireActive = true;
        ChangeFlame();
    }

    private IEnumerator SetPuffs()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("fire node start");
        extinguishNodes = null;
        extinguishNodes = FindObjectsByType<PuffMovement>(FindObjectsSortMode.None);
    }

}
