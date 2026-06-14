using System.Collections.Generic;
using UnityEngine;

public class FireNodeScript : MonoBehaviour
{
    [SerializeField] private Bounds bounds;
    [SerializeField] private FireScript myFire;
    [SerializeField] private GameObject player;
    [SerializeField] private List<GameObject> extinguishNodes;
    [SerializeField] private float flameStrength;
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
        for (int i = 0; i < extinguishNodes.Count; i++)
        {
            if (bounds.Contains(extinguishNodes[i].transform.position))
                DampenFlame(extinguishNodes[i]);
        }
    }

    private void DampenFlame(GameObject other)
    {
        flameStrength -= flameLoss;
        changeFlame();
        other.transform.position = new Vector3(0, 0, 0);
        if (flameStrength < 1)
        {
            myFire.myFireNodes.Remove(this.gameObject);
            Destroy(this.gameObject);
        }
    }

    private void changeFlame()
    {
        sysMain.startSize = originalStartSize * (flameStrength / 100);
        sysShape.radius = originalShapeRadius * (flameStrength / 100);
    }

}
