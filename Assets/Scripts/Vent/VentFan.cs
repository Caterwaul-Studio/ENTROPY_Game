using UnityEngine;

public class VentFan : MonoBehaviour
{
    [SerializeField] private PersistantManager persistant;

    [SerializeField] private bool playerInZone;
    [SerializeField] private float speed = 45f;

    [SerializeField] private BoxCollider fanActiveCollider;

    // Update is called once per frame
    void Update()
    {
        if(persistant == null)
        {
            persistant = FindFirstObjectByType<PersistantManager>();
            return;
        }

        //if the player is colliding with the fanactivecollider, rotate the fan
        if (IsPlayerInFanZone())
            RotateFan();
    }
    private bool IsPlayerInFanZone()
    {
        if (persistant.Player == null || fanActiveCollider == null)
            return false;

        Vector3 playerPos = persistant.Player.transform.position;
        return fanActiveCollider.bounds.Contains(playerPos);
    }

    private void RotateFan()
    {
        transform.localRotation *= Quaternion.Euler(0f, speed * Time.deltaTime, 0f);
    }
}
