using System.Collections;
using UnityEngine;

public enum SpecificEnemyState
{
    Chase,
    Investigate,
    Patrol,
    Charge,
    Lunge,
    Kill,
    Retreat,
    Stunned,
}

public enum GeneralEnemyState
{
    Pause,
    Active,
    Retreat,
    Idle
}

public enum EnemyVersion
{
    Complex,
    Simple
}

public class EnemyStateMachine : MonoBehaviour
{
    public static EnemyStateMachine Instance { get; private set; }

    public void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    [Header("Enum Values")]
    public EnemyVersion enemyVersion;
    public GeneralEnemyState currentGeneralState;
    public SpecificEnemyState currentSpecificState;

    [SerializeField] private ComplexEnemyAI ComplexEnemyAI;

    [Header("References")]
    public GameObject player;
    [SerializeField] private GameObject simpleEnemy;
    [SerializeField] private GameObject complexEnemy;

    [SerializeField] public bool isPaused = false;
    [SerializeField] public bool isActive = false;
    [SerializeField] public bool isRetreating = false;

    [Header("Detection")]

    public LayerMask playerLayer;
    public LayerMask barrierLayer; // Set this to "Barrier"
    public float detectionRadius = 5f;
    public float wakeLossCooldown = 10f;
    private float timeSinceLastSeenPlayer = 0f;
    private bool canDetectPlayer = false;

    [Header("Interest")]
    public float interestDuration = 10f;
    //[SerializeField] private float interestTimer;
    private Coroutine InvestingTimerCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (simpleEnemy == null)
        {
            simpleEnemy = GameObject.FindGameObjectWithTag("SimpleEnemy");
        }

        if (complexEnemy == null)
        {
            complexEnemy = GameObject.FindGameObjectWithTag("ComplexEnemy");
        }
    }

    // Update is called once per frame
    void Update()
    {
        EnemyStateHandler();
    }

    #region StateHandler
    private void EnemyStateHandler()
    {
        if (isPaused)
        {
            //currentGeneralState = GeneralEnemyState.Pause;
            return;
        }
        //active
        if (isActive)
        {
            ChaseStates();
        }
        //Retreating
        else if (isRetreating)
        {

        }
        //idle
        else if (!isActive)
        {

        }
    }


    private void ChangeCurrentState()
    {
        
    }

    private void ManualStateChange()
    {

    }
    #endregion

    //Enemy Detection 

    #region Detection/Attention Methods

    private void ChaseStates()
    {
        isActive = true;

        if (currentSpecificState == SpecificEnemyState.Chase)
        {
            if (EnemyDetection() == true)
            {
                return;
            }

            currentSpecificState = SpecificEnemyState.Investigate;
        }

        if (currentSpecificState == SpecificEnemyState.Investigate)
        {
            StartTimer();

            if (EnemyDetection() == true)
            {
                StopTimer();

                currentSpecificState = SpecificEnemyState.Chase;
            }
        }

        if (canDetectPlayer == false)
        {
        }
    }

    private bool EnemyDetection()
    {
        RaycastHit hit;
        //This should allow the Geist to detect the player only and only if it's directly in the LOS and area of detection
        if (Physics.Raycast(transform.position,player.transform.position,out hit,detectionRadius, playerLayer))
        {
            canDetectPlayer = true;
            return true;
        }
        else
        {
            canDetectPlayer = false;
            return false;
        }
    }

    private IEnumerator TimerRoutine(float time) 
    {
        float remaining = time;
        while (remaining > 0)
        {
            remaining -= Time.deltaTime;
            yield return null;
        }

        InvestingTimerCoroutine = null;
    }

    private void StartTimer()
    {
        //Stops any existing timers for the investing state 
        StopTimer();

        InvestingTimerCoroutine = StartCoroutine(TimerRoutine(interestDuration));
    }

    private void StopTimer()
    {
        if (InvestingTimerCoroutine != null)
        {
            StopCoroutine(InvestingTimerCoroutine);
            InvestingTimerCoroutine = null; // Clear the reference
            Debug.Log("Timer Interrupted!");
        }
    }



    #endregion

    void OnDrawGizmosSelected()
    {
        // Only draw when selected in the editor
        Gizmos.color = Color.yellow;

        //if the Geist can detect the player
        if (canDetectPlayer)
        {
            Gizmos.color = Color.green;
        }
        else 
        {
            Gizmos.color = Color.red;
        }
        // Wake distance (yellow)

        // 1. Calculate the offset from start to target
        Vector3 offset = player.transform.position - transform.position;

        // 2. Clamp that offset to your max length
        Vector3 limitedOffset = Vector3.ClampMagnitude(offset, detectionRadius);

        // 3. Calculate the final "clamped" point
        Vector3 clampedPoint = player.transform.position + limitedOffset;

        Gizmos.DrawLine(transform.position, clampedPoint);

        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Chase distance (red)
        //Gizmos.color = Color.red;
        //Gizmos.DrawWireSphere(transform.position, chaseDistance);
    }
}
