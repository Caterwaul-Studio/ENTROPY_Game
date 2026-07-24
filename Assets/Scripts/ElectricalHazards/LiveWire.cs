using System.Linq;
using UnityEngine;

public class LiveWire : MonoBehaviour
{
    [SerializeField] private ParticleSystem sys;
    [SerializeField] private Rigidbody endBody;
    [SerializeField] private Rigidbody[] bodies;
    [SerializeField] private float cooldown;
    [SerializeField] private float detectionRadius;
    [SerializeField] private GameObject player;
    //[SerializeField] PlayerUIManager uiManager;
    private float cooldownTimer = 0;

    // Update is called once per frame
    void Update()
    {
        cooldown -= Time.deltaTime;
        if (sys.particleCount > 0 && cooldownTimer <= 0)
        {
            endBody.AddForce(new Vector3 (Random.Range(0,100), Random.Range(0, 100), Random.Range(0, 100)));
            cooldownTimer = cooldown;
        }
    }
}
