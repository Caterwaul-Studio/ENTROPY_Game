using UnityEngine;

public class EnemyEffects : MonoBehaviour
{
    [Header("Effect Ranges")]
    //Closest
    [SerializeField] private float distance1;
    //Middle
    [SerializeField] private float distance2;
    //Farthest
    [SerializeField] private float distance3;
    [SerializeField] protected float intensity;

    [Header("Eye Effects")]
    [SerializeField] private Color currentEyeColor;
    [SerializeField] private Light eyeLight;

    [Header("References")]
    [SerializeField] private GameObject player;
    [SerializeField] private Material screenMaterials;
    [SerializeField] private EnemyStateMachine stateMachine;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
    }

    private void FixedUpdate()
    {
        CheckEffectDistance();
    }

    private void CheckEffectDistance()
    {
        float currrentDistance = Vector3.Distance(transform.position, player.transform.position);

        if (currrentDistance > distance3)
        {
            return;
        }
        else if (currrentDistance <= distance3 && currrentDistance > distance2)
        {
            intensity = 0.25f;
        }
        else if (currrentDistance <= distance2 && currrentDistance > distance1)
        {
            intensity = 0.50f;
        }
        else if (currrentDistance <= distance1)
        {
            intensity = 1f;
        }
    }

    private void EyeColorLogic()
    {
        if (stateMachine.currentSpecificState == SpecificEnemyState.Chase || SpecificEnemyState.Grab || SpecificEnemyState.Kill)
        {
            
        }
        else if (stateMachine.currentSpecificState)
        {

        }
        else if (stateMachine.currentSpecificState)
        {

        }
        else if (stateMachine.currentSpecificState)
        {

        }
        else
        {

        }
            switch (stateMachine.currentSpecificState)
            {
                case SpecificEnemyState.Chase:

                    break;
                case SpecificEnemyState.Patrol:

                    break;
                case SpecificEnemyState.Investigate:

                    break;
                case SpecificEnemyState.Retreat:

                    break;
                case SpecificEnemyState.Stunned:
                    break;
                case SpecificEnemyState.Grab:
                    break;
            }

    }

    private void ChangeEyeColor(Color newColor)
    {
        eyeLight.color = newColor;
    }

    void OnDrawGizmosSelected()
    {
        // Only draw when selected in the editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distance1);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distance2);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, distance3);
        // Draw a forward-facing line from the transform
        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * 2f; // adjust length as needed
        Gizmos.DrawLine(start, end);

        // Wake distance (yellow)
        Gizmos.color = Color.yellow;
        
    }
}
