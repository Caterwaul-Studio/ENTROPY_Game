using UnityEngine;
using UnityEngine.InputSystem;

public class GloveScript : MonoBehaviour
{

    [SerializeField] private GameObject player;
    [SerializeField] private ZeroGravity zeroG;
    [SerializeField] private PickupScript pickupScript;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (player == null || zeroG == null)
        {
            player = GameObject.FindAnyObjectByType<ZeroGravity>().gameObject;
            zeroG = player.GetComponent<ZeroGravity>();
            pickupScript = player.GetComponent<PickupScript>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (pickupScript.HeldObject == this.gameObject)
        {
            zeroG.hasGloves = true;
            pickupScript.ThrowObject();
            pickupScript.current = null;
            this.gameObject.SetActive(false);
        }
        
    }
}
