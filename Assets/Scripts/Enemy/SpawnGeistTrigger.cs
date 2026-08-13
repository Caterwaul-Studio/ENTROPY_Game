using UnityEngine;

public class SpawnGeistTrigger : MonoBehaviour
{
    [SerializeField] private PersistantManager persistant;
    [SerializeField] private BoxCollider geistSpawnCollider;

    public bool triggerEntered = false;

    public event System.Action<bool> OnGeistTriggerEnter;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (persistant == null)
        {
            persistant = FindFirstObjectByType<PersistantManager>();
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(persistant != null)
        {
            PlayerEntersTrigger();
        }
    }

    private void PlayerEntersTrigger()
    {
        if (persistant.Player == null || geistSpawnCollider == null)
            return;

        Vector3 playerPos = persistant.Player.transform.position;
        if (geistSpawnCollider.bounds.Contains(playerPos))
        {
            triggerEntered = true;
            OnGeistTriggerEnter?.Invoke(triggerEntered);
        }
    }
}
