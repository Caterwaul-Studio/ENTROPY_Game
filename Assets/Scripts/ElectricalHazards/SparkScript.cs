using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Controls;


public class SparkScript : MonoBehaviour
{
    [SerializeField] private Bounds sparkBounds;
    [SerializeField] private GameObject player;
    [SerializeField] private Camera MainCamera;
    [SerializeField] private ParticleSystem sys;
    private float throwCooldown;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (player == null || MainCamera == null)
        {
            player = GameObject.FindAnyObjectByType<ZeroGravity>().gameObject;
            MainCamera = player.GetComponent<ZeroGravity>().cam;
        }

        sparkBounds = new Bounds(transform.position, this.gameObject.GetComponent<BoxCollider>().size);
        Destroy(this.gameObject.GetComponent<BoxCollider>());
    }

    // Update is called once per frame
    void Update()
    {
        throwCooldown += Time.deltaTime;
        if (sparkBounds.Contains(player.transform.position) && throwCooldown > 1f && sys.particleCount > 0)
            DamagePlayer();
    }

    private void DamagePlayer()
    {
        player.GetComponent<ZeroGravity>().DecreaseHealth(1);
        throwCooldown = 0;
        player.GetComponent<ZeroGravity>().GetThrown(MainCamera.transform.forward * -1, 5);
    }

}
