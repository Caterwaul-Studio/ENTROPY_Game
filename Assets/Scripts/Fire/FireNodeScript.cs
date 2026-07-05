using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

public class FireNodeScript : MonoBehaviour
{
    [SerializeField] private Bounds bounds;
    [SerializeField] private FireScript myFire;
    [SerializeField] private GameObject player;
    [SerializeField] private Camera camera;
    [SerializeField] private List<GameObject> extinguishNodes;
    [SerializeField] private float flameStrength;
    [SerializeField] private int flameSteady; //should be 1-5
    [SerializeField] private int regenRate;
    [SerializeField] private float flameLoss; //this variable is how much strength the flame loses every time it gets hit.
    [SerializeField] private ParticleSystem sys;
    [SerializeField] private ParticleSystem.MainModule sysMain;
    [SerializeField] private ParticleSystem.EmissionModule sysEmission;
    [SerializeField] private ParticleSystem.ShapeModule sysShape;
    [SerializeField] private ParticleSystem.ColorOverLifetimeModule sysCOL;
    [SerializeField] private ParticleSystem.SizeOverLifetimeModule sysSOL;
    [SerializeField] private ParticleSystem.LightsModule sysLight;

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
        //this method is similar to the audiozone method however this time the boxcolider size is used instead of the object scale
        //the reason for this difference is because changing the object scale is not arbitrary for fire nodes, it will affect the size of their particle systems
        bounds = new Bounds(transform.position,this.gameObject.GetComponent<BoxCollider>().size);
        Destroy(this.gameObject.GetComponent<BoxCollider>());
        //please edit the bounds via editing the box collider, do not change the center of the box collider, just move the whole object

        //setting original params
        originalStartSize = sysMain.startSize.constant;
        originalShapeRadius = sysShape.radius;
    }

    // Update is called once per frame
    void Update()
    {
        throwCooldown += Time.deltaTime;
        for (int i = 0; i < extinguishNodes.Count; i++)
        { //collision detection is done via bounds in the hope it will be less resource intensive, may need to be tested
            if (bounds.Contains(extinguishNodes[i].transform.position))
                DampenFlame(extinguishNodes[i]);
        }

        if (bounds.Contains(player.transform.position) && throwCooldown > 0.5f)
        {
            player.GetComponent<ZeroGravity>().GetThrown(camera.transform.forward * -1, 30);
            throwCooldown = 0;
        }

        //flame regeneration
        if (flameStrength < flameSteady * 20)
            flameStrength += Time.deltaTime * regenRate;

        if (flameStrength > 81)
        {
            flameSteady = 5;
        } else if (flameStrength > 61)
        {
            flameSteady = 4;
        } else if (flameStrength > 41)
        {
            flameSteady = 3;
        } else if (flameStrength > 21)
        {
            flameSteady = 2;
        } else if (flameStrength > 11)
        {
            flameSteady = 1;
        }
    }

    private void DampenFlame(GameObject other)
    {
        flameStrength -= flameLoss; //dampening
        changeFlame();
        other.transform.position = new Vector3(0, 0, 0);
        if (flameStrength < 1) //when flame strength is under a certain value, destroy the flame node
        {
            myFire.myFireNodes.Remove(this.gameObject); //update the parent with the destruction of the node
            Destroy(this.gameObject);
        }
    }

    private void changeFlame()
    {
        //all changes to the flame particle system are to happen here
        sysMain.startSize = originalStartSize * (flameStrength / 100);
        sysShape.radius = originalShapeRadius * (flameStrength / 100);
    }

}
