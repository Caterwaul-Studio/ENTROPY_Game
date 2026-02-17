using System.Collections;
using System.Collections.Generic;
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
    Idle, // Special State for certain circumstances
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

    //[SerializeField] private ComplexEnemyAI ComplexEnemyAI;

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
    public LayerMask doorLayer;
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
    public float retreatDuration = 3f;
    [SerializeField] private float retreatTimer;
    private Coroutine retreatingTimerCoroutine;
    [SerializeField] private float test;
    private bool playerCanSeeEnemy;
    private bool canRetreat; // This is to see if the Geist can run by moving away or does it have to move through the wall and teleport
    private float retreatDistanceCheck;
    private float randomPointRetreatDistanceMin;
    private float randomPointRetreatDistanceMax;
    [SerializeField] private LayerMask waypointLayer;
    public float minRadius = 3f;
    public float maxRadius = 7f;
    private float maxRadiusAdd;


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

    public void EnemyStateHandler()
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
            DeterminePlayerLOS();

            RetreatStates();
        }
        //idle/roaming
        else if (currentGeneralState == GeneralEnemyState.)
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
        if (canDetectPlayer)
        {
            ChangeSpecificState(SpecificEnemyState.Chase);

            ComplexEnemyAI.Instance.IsChasingPlayer();
            //--Chase--
        }
        else if (!canDetectPlayer && currentSpecificState == SpecificEnemyState.Chase)
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
            if (canDetectPlayer)
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

        Vector3 playerDetection = player.transform.position - transform.position;

        //This should allow the Geist to detect the player only and only if it's directly in the LOS and area of detection
        if (Physics.Raycast(transform.position, playerDetection, out hit,detectionRadius, playerLayer))
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
        if (currentGeneralState == GeneralEnemyState.Retreat)
        {
            //This limits how long the retreat timer is
            StartTimer(retreatDuration, retreatTimer, retreatingTimerCoroutine);



            if (!playerCanSeeEnemy) 
            {
                StopTimer(retreatingTimerCoroutine);

                TeleportToRandomPoint();
            }
        }
    }

    private void DeterminePlayerLOS()
    {
        RaycastHit hit;

        Vector3 playerDetection = player.transform.position - transform.position;
        //This should detect if the player can see the geist, by mainly seeing if there's a door or a wall between the
        //
        if (Physics.Raycast(transform.position, playerDetection, out hit, retreatDistanceCheck, barrierLayer) || Physics.Raycast(transform.position, playerDetection, out hit, retreatDistanceCheck, barrierLayer))
        {
            playerCanSeeEnemy = true;
        }
        else
        {
            playerCanSeeEnemy = false;
        }
    }

    private List<Collider> DetermineRandomPoints()
    {
        Collider[] allWithinMax = Physics.OverlapSphere(transform.position, maxRadius + maxRadiusAdd, waypointLayer);
        if (maxRadiusAdd >= 20)
        {
            return null;
        }

        if (allWithinMax.Length == 0)
        {
            //Change
            maxRadiusAdd += 5;
            return DetermineRandomPoints();
        }

        List<Collider> results = new List<Collider>();
        float minRadiusSquared = minRadius * minRadius; 

        foreach (var col in allWithinMax)
        {
            float distSq = (col.transform.position - transform.position).sqrMagnitude;

            if (distSq >= minRadiusSquared)
            {
                results.Add(col);
            }
        }

        return results;
    }

    private void TeleportToRandomPoint()
    {
        List<Collider> TempList = DetermineRandomPoints();

        maxRadiusAdd = 0;

        if (TempList == null)
        {
            return;
        }

        transform.position = TempList[Random.Range(0, TempList.Count)].transform.position;
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
