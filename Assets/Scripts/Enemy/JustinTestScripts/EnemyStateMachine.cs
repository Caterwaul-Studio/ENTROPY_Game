using System.Collections;
using UnityEngine;

public enum SpecificEnemyState
{
    Chase,
    Investigate,
    Patrol,
    Idle,
    Kill,
    Retreat,
    Stunned,
}

public enum GeneralEnemyState
{
    Pause,
    Active,
    Retreat,
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
    public SpecificEnemyState pastSpecificState;

    [SerializeField] private ComplexEnemyAI ComplexEnemyAI;

    [Header("References")]
    public GameObject player;
    [SerializeField] private GameObject simpleEnemy;
    [SerializeField] private GameObject complexEnemy;

    //[SerializeField] public bool isPaused = false;
    //[SerializeField] public bool isActive = false;
    //[SerializeField] public bool isRetreating = false;

    [Header("Detection")]

    public LayerMask playerLayer;
    public LayerMask barrierLayer; // Set this to "Barrier"
    public float detectionRadius = 5f;
    public float wakeLossCooldown = 10f;
    private float timeSinceLastSeenPlayer = 0f;
    private bool canDetectPlayer = false;

    public float detectionDuration = 10f;
    private Coroutine detectingTimerCoroutine;
    [SerializeField] private float detectionTimer;

    [Header("Interest")]
    public float interestDuration = 10f;
    [SerializeField] private float interestTimer;
    private Coroutine investingTimerCoroutine;

    [Header("Retreat")]
    [SerializeField] private float test;

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

    //
    /*


     */

    private void EnemyStateHandler()
    {
        if (currentGeneralState == GeneralEnemyState.Pause)
        {
            //currentGeneralState = GeneralEnemyState.Pause;
            return;
        }
        //active
        if (currentGeneralState == GeneralEnemyState.Active)
        {
            EnemyDetection();

            ChaseStates();
        }
        //Retreating
        else if (currentGeneralState == GeneralEnemyState.Retreat)
        {

        }
        //idle
        else if (currentGeneralState != GeneralEnemyState.Active && currentGeneralState != GeneralEnemyState.Retreat)
        {

        }
    }

    //This is basically a hard State Change it will change the state without all the logic
    private void ManualStateChange()
    {
         
    }
    #endregion

    //Enemy Detection 

    #region Detection/Attention Methods

    //
    /*

     */
    private void ChaseStates()
    {
        if (canDetectPlayer == true)
        {
            ChangeSpecificState(SpecificEnemyState.Chase);


            //--Chase--
        }
        else if (canDetectPlayer == false && currentSpecificState == SpecificEnemyState.Chase)
        {
            //Changes to Specific State of Investigate
            ChangeSpecificState(SpecificEnemyState.Investigate);
        }

        if (currentSpecificState == SpecificEnemyState.Investigate)
        {
            //Start the Investigation Timer and stop chasing
            if (investingTimerCoroutine == null)
            {
                StartTimer(interestDuration, interestTimer, investingTimerCoroutine);
            }

            //If the player is detected again, within the time limit start chasing again
            if (canDetectPlayer == true)
            {
                StopTimer(investingTimerCoroutine);

                ChangeSpecificState(SpecificEnemyState.Chase);
            }

            //If the player isn't detected in the time limit start patroling again,
            //or some other behavior this can be changed
            if (interestTimer <= 0 && currentSpecificState == SpecificEnemyState.Investigate)
            {
                ChangeSpecificState(SpecificEnemyState.Patrol);
            }

            //During the time limit start Investigating 

            //--Investigate--
        }

        if (currentSpecificState == SpecificEnemyState.Patrol)
        {

        }
    }

    //Changes currentSpecificState to the new state and saves the immediate past state 
    private void ChangeSpecificState(SpecificEnemyState newState)
    {
        if (currentSpecificState != newState)
        {
            return;
        }

        pastSpecificState = currentSpecificState;
        currentSpecificState = newState;
    }

    private void EnemyDetection()
    {
        RaycastHit hit;
        //This should allow the Geist to detect the player only and only if it's directly in the LOS and area of detection
        if (Physics.Raycast(transform.position,player.transform.position,out hit,detectionRadius, playerLayer))
        {
            canDetectPlayer = true;
        }
        else
        {
            canDetectPlayer = false;
        }
    }

    #endregion

    #region Retreat Logic

    //When retreating, move away from the player by going through waypoints,
    //if moving to the only waypoint to retreat isn't valid or will collide 
    //with the player move through the walls or some 
    private void RetreatStates()
    {
        
    }

    private void LeavePlayerLOS()
    {
        RaycastHit hit;
        //This should allow the Geist to detect the player only and only if it's directly in the LOS and area of detection
        if (Physics.Raycast(transform.position, player.transform.position, out hit, detectionRadius, playerLayer))
        {
            canDetectPlayer = true;
        }
        else
        {
            canDetectPlayer = false;
        }
    }

    private void MoveToRandomPoint()
    {

    }

    #endregion

    #region Timer Methods

    private IEnumerator TimerRoutine(float duration, float timer, Coroutine timerCoroutine)
    {
        timer = duration;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        timerCoroutine = null;
    }

    private void StartTimer(float duration, float timer, Coroutine timerCoroutine)
    {
        //Stops any existing timers for the investing state 
        StopTimer(timerCoroutine);

        timerCoroutine = StartCoroutine(TimerRoutine(duration, timer, timerCoroutine));
    }

    private void StopTimer(Coroutine timerCoroutine)
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null; // Clear the reference
        }
    }

    #region Outdated Timer Methods
    /*
    private IEnumerator InvestigationTimerRoutine(float time) 
    {
        interestTimer = time;
        while (interestTimer > 0)
        {
            interestTimer -= Time.deltaTime;
            yield return null;
        }

        InvestingTimerCoroutine = null;
    }

    private void StartInvestigationTimer()
    {
        //Stops any existing timers for the investing state 
        StopInvestigationTimer();

        InvestingTimerCoroutine = StartCoroutine(InvestigationTimerRoutine(interestDuration));
    }

    private void StopInvestigationTimer()
    {
        if (InvestingTimerCoroutine != null)
        {
            StopCoroutine(InvestingTimerCoroutine);
            InvestingTimerCoroutine = null; // Clear the reference
        }
    }

    // if the player enters within the detection range start the timer

    
    private IEnumerator DetectionTimerRoutine(float time)
    {
        interestTimer = time;
        while (interestTimer > 0)
        {
            interestTimer -= Time.deltaTime;
            yield return null;
        }

        InvestingTimerCoroutine = null;
    }

    private void StartDetectionTimer()
    {
        //Stops any existing timers for the investing state 
        StopInvestigationTimer();

        InvestingTimerCoroutine = StartCoroutine(InvestigationTimerRoutine(interestDuration));
    }

    private void StopDetectionTimer()
    {
        if (InvestingTimerCoroutine != null)
        {
            StopCoroutine(InvestingTimerCoroutine);
            InvestingTimerCoroutine = null; // Clear the reference
        }
    }
    */
    #endregion

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
