using System.Linq;
using UnityEngine;

public class LiveWire : MonoBehaviour
{
    [SerializeField] private ParticleSystem sys;
    [SerializeField] private Rigidbody endBody;
    [SerializeField] private GameObject[] bodies;
    [SerializeField] private float cooldown;
    [SerializeField] private GameObject player;
    [SerializeField] private bool wireActive;
    //[SerializeField] PlayerUIManager uiManager;
    private float cooldownTimer = 0;

    // Update is called once per frame
    void Update()
    {
        if (wireActive)
        {
            cooldown -= Time.deltaTime;
            if (sys.particleCount > 0 && cooldownTimer <= 0)
            { //applying a force to the last body in the chain of bodies 'wriggles' the wire
                endBody.AddForce(new Vector3(Random.Range(0, 100), Random.Range(0, 100), Random.Range(0, 100)));
                cooldownTimer = cooldown;
            }
        }
    }

    //For the next two functions, the tag of the capsules determines if they do damage to the player when the player collides with them

    public void DeactivateWire()
    {
        wireActive = false;
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].tag = "barrier";
        }
    }

    public void ActivateWire()
    {
        wireActive = true;
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].tag = "LiveWire";
        }
    }
}
