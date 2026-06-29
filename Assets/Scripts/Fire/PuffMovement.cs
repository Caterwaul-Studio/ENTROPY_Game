using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PuffMovement : MonoBehaviour
{
    //this may need to be swapped out for the fire extinguisher models
    [SerializeField] private GameObject player;
    [SerializeField] private Camera MainCamera;
    [SerializeField] private float playerPushForce;
    public void Shoot(Transform firePosition)
    {
        transform.position = firePosition.transform.position;
        transform.rotation = MainCamera.transform.rotation;
        Vector3 direction = new Vector3(0, 0, 0);
        direction = transform.forward;
        direction.x = direction.x * Random.RandomRange(0.9f, 1.1f);
        gameObject.GetComponent<Rigidbody>().linearVelocity = transform.forward;
        //gameObject.GetComponent<Rigidbody>().AddForce(player.GetComponent<Rigidbody>().linearVelocity,ForceMode.VelocityChange); //doesnt do anything
        player.GetComponent<Rigidbody>().linearVelocity = -transform.forward * playerPushForce;
        Debug.Log(player.GetComponent<Rigidbody>().angularVelocity);
    }
}
