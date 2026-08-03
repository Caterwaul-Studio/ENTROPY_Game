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
    [SerializeField] private bool WireSpark;
    [SerializeField] private bool electricActive;

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

        if (myGrabbable != null)
            myGrabbable.AddComponent<SparkBar>();
    }

    // Update is called once per frame
    void Update()
    {
        if (zeroG.hasGloves && zeroG.IsGrabbing)
            damageCoolDown = 0;

        damageCoolDown += Time.deltaTime;
        if (electricActive)
        {
            if (WireSpark)
                sparkBounds = new Bounds(transform.position, boxSize);

            if (sparkBounds.Contains(player.transform.position) && damageCoolDown > 1f && sys.particleCount > 0)
                if (!WireSpark)
                    DamagePlayer();
        }
    }

    private void DamagePlayer()
    {
        if (!zeroG.hasGloves || (zeroG.hasGloves && !zeroG.IsGrabbing))
        {
            zeroG.DecreaseHealth(1);
            StartCoroutine(zeroG.ShockEffect());
            damageCoolDown = 0;
            zeroG.GetThrown(MainCamera.transform.forward * -1, 8);
            if (zeroG.IsGrabbing)
                zeroG.ReleaseBar();
        }
    }
}
