using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Controls;


public class SparkScript : MonoBehaviour
{
    [SerializeField] private Bounds sparkBounds;
    [SerializeField] private GameObject player;
    [SerializeField] private ZeroGravity zeroG;
    [SerializeField] private Camera MainCamera;
    [SerializeField] private ParticleSystem sys;
    [SerializeField] private bool WireSpark;
    [SerializeField] private bool electricActive;

    private Vector3 boxSize;
    private float throwCooldown;
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
    }

    // Update is called once per frame
    void Update()
    {
        if (electricActive)
        {
            if (WireSpark)
                sparkBounds = new Bounds(transform.position, boxSize);

            throwCooldown += Time.deltaTime;
            if (sparkBounds.Contains(player.transform.position) && throwCooldown > 1f && sys.particleCount > 0)
                DamagePlayer();
        }
    }

    private void DamagePlayer()
    {
        zeroG.DecreaseHealth(1);
        StartCoroutine(zeroG.ShockEffect());
        throwCooldown = 0;
        zeroG.GetThrown(MainCamera.transform.forward * -1, 8);
        if (zeroG.IsGrabbing)
            zeroG.ReleaseBar();
    }

}
