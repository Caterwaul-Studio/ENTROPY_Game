using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PuffMovement : MonoBehaviour
{
    //this may need to be swapped out for the fire extinguisher models
    [SerializeField] private GameObject player;
    [SerializeField] private Camera MainCamera;
    public void Shoot(Transform firePosition)
    {
        transform.position = firePosition.transform.position;
        transform.rotation = MainCamera.transform.rotation;
        gameObject.GetComponent<Rigidbody>().linearVelocity = transform.forward;
        gameObject.GetComponent<Rigidbody>().AddForce(player.GetComponent<Rigidbody>().linearVelocity,ForceMode.VelocityChange);
    }
}
