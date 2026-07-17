using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PuffMovement : MonoBehaviour
{
    //this may need to be swapped out for the fire extinguisher models
    [SerializeField] private GameObject player;
    [SerializeField] private Camera MainCamera;
    [SerializeField] private float playerPushForce;
    [SerializeField] private float puffSpeed;

    void Start()
    {
        if (player == null || MainCamera == null)
        {
            player = GameObject.FindAnyObjectByType<ZeroGravity>().gameObject;
            MainCamera = player.GetComponent<ZeroGravity>().cam;
        }
    }

    public void Shoot(Transform firePosition)
    {
        transform.position = firePosition.transform.position;
        transform.rotation = MainCamera.transform.rotation;
        Vector3 direction = new Vector3(0, 0, 0);
        direction = transform.forward;
        gameObject.GetComponent<Rigidbody>().linearVelocity = transform.forward * puffSpeed;
        gameObject.GetComponent<Rigidbody>().AddForce(player.GetComponent<Rigidbody>().linearVelocity,ForceMode.VelocityChange);
        //player.GetComponent<Rigidbody>().AddForce(player.GetComponent<Rigidbody>().linearVelocity * -playerPushForce, ForceMode.VelocityChange); //slows you down gradually, cant go backwards.
        player.GetComponent<Rigidbody>().AddForce(transform.forward * -playerPushForce, ForceMode.VelocityChange);
    }
}
