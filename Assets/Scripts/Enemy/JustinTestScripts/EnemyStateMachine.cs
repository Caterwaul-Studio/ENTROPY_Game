using UnityEngine;

public enum EnemyState
{
    Idle,
    Chase,
    Investigate,
    Retreat,
    Patrol,
    Kill,
    Wait
}

public class EnemyStateMachine : MonoBehaviour
{
    public EnemyState currentState;

    [SerializeField] private complex

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
