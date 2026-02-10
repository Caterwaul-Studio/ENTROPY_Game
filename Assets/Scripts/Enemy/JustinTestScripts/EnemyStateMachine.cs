using UnityEngine;

public enum EnemyState
{
    Idle,
    Chase,
    Investigate,
    Retreat,
    Patrol,
    Kill,
    Wait,
    Pause,
}

public enum EnemyVersion
{
    Complex,
    Simple
}

public class EnemyStateMachine : MonoBehaviour
{
    public EnemyVersion enemyVersion;
    public EnemyState currentState;

    [SerializeField] private ComplexEnemyAI ComplexEnemyAI;

    [SerializeField] private GameObject simpleEnemy;
    [SerializeField] private GameObject complexEnemy;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (simpleEnemy == null)
        {

        }

        if (complexEnemy == null)
        {

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
