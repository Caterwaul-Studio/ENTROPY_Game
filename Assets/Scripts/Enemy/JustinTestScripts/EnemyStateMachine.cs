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

    [Header("Enum Values")]
    public EnemyVersion enemyVersion;
    public GeneralEnemyState currentGeneralState = GeneralEnemyState.Active;
    public SpecificEnemyState currentSpecificState = SpecificEnemyState.Patrol;
    public SpecificEnemyState pastSpecificState;

    [Header("References")]
    public GameObject player;
    [SerializeField] private GameObject simpleEnemy;
    [SerializeField] private GameObject complexEnemy;

    [Header("Detection Settings")]
    public LayerMask detectionMask; // Set this to "Default", "Player", and "Obstacles"
    public float detectionRadius = 15f;
    public float detectionDuration = 1.5f; // Time to "spot" player
    [SerializeField] private float detectionTimer;
    [SerializeField] private bool canDetectPlayer = false;
    [SerializeField] private bool chasePlayer = false;

    [Header("Interest/Search Settings")]
    public float interestDuration = 10f;
    [SerializeField] private float interestTimer;
    public Transform playersLastKnownLocation;

    [Header("Retreat Settings")]
    public float retreatDuration = 3f;
    public float retreatDistanceCheck = 20f;
    public LayerMask barrierLayer;
    public LayerMask waypointLayer;
    public float minRadius = 7f;
    public float maxRadius = 9f;
    [SerializeField] private float retreatTimer;

    [Header("Gizmos")]
    [SerializeField] private bool showDetectionRadius;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); }
        else { Instance = this; DontDestroyOnLoad(gameObject); }
    }

    private void Start()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        currentSpecificState = SpecificEnemyState.Patrol;
    }

    public void GeneralLogic()
    {
        if (currentGeneralState == GeneralEnemyState.Pause) return;

        // Run detection every frame
        canDetectPlayer = PerformRaycastDetection();

        if (currentGeneralState == GeneralEnemyState.Active)
        {
            HandleActiveLogic();
        }
        else if (currentGeneralState == GeneralEnemyState.Retreat)
        {
            HandleRetreatLogic();
        }
    }

    #region Active Logic
    private void HandleActiveLogic()
    {
        switch (currentSpecificState)
        {
            case SpecificEnemyState.Patrol:
                ComplexEnemyAI.Instance.isPatroling();

                if (canDetectPlayer)
                {
                    // If we haven't committed to the chase yet, count down the "spotting" timer
                    if (!chasePlayer)
                    {
                        detectionTimer -= Time.deltaTime;

                        if (detectionTimer <= 0)
                        {
                            chasePlayer = true;
                            ChangeSpecificState(SpecificEnemyState.Chase);
                        }
                    }
                }
                else
                {
                    // If the player hides, reset the detection progress
                    chasePlayer = false;
                    detectionTimer = detectionDuration;
                }
                break;

            case SpecificEnemyState.Chase:
                ComplexEnemyAI.Instance.IsChasingPlayer();
                if (!canDetectPlayer)
                {
                    interestTimer = interestDuration;
                    ChangeSpecificState(SpecificEnemyState.Investigate);
                }
                break;

            case SpecificEnemyState.Investigate:
                interestTimer -= Time.deltaTime;
                // Geist logic for searching
                if (canDetectPlayer)
                {
                    ChangeSpecificState(SpecificEnemyState.Chase);
                }
                else if (interestTimer <= 0)
                {
                    ChangeSpecificState(SpecificEnemyState.Patrol);
                }
                else
                {

                }

                break;
        }
    }
    #endregion

    #region Investigate
    public List<Waypoint> FindInvestWaypoints(Waypoint OriginPoint)
    {
        HashSet<Waypoint> waypoints = new HashSet<Waypoint>();

        foreach (Waypoint neighbor in OriginPoint.neighbors)
        {
            waypoints.Add(neighbor);

            foreach (Waypoint farneighbor in neighbor.neighbors)
            {
                waypoints.Add(farneighbor);
            }
        }

        return new List<Waypoint>(waypoints);
    }

    public Waypoint GetRandomInvestPoint(Waypoint OriginPoint)
    {
        List<Waypoint> waypoints = FindInvestWaypoints(OriginPoint);

        return waypoints[Random.Range(0, waypoints.Count)];
    }

    #endregion

    #region Retreat Logic

    private void HandleRetreatLogic()
    {
        retreatTimer -= Time.deltaTime;

        // If timer ends or player loses LOS, find a path
        if (retreatTimer <= 0 || !IsPlayerLookingAtMe())
        {
            ComplexEnemyAI.Instance.FindRetreatPath();
        }
    }

    private bool IsPlayerLookingAtMe()
    {
        Vector3 dir = player.transform.position - transform.position;
        // If there's a barrier in the way, the player CANNOT see me
        return !Physics.Raycast(transform.position, dir.normalized, retreatDistanceCheck, barrierLayer);
    }

    // A safer version of your random point logic using a loop instead of recursion
    public Waypoint GetRandomValidPoint()
    {
        float currentMax = maxRadius;
        for (int i = 0; i < 5; i++) // Try 5 times to expand radius
        {
            Collider[] cols = Physics.OverlapSphere(transform.position, currentMax, waypointLayer);
            List<Waypoint> validPoints = new List<Waypoint>();

            foreach (var c in cols)
            {
                if (Vector3.Distance(transform.position, c.transform.position) >= minRadius)
                {
                    Waypoint wp = c.GetComponent<Waypoint>();
                    if (wp != null) validPoints.Add(wp);
                }
            }

            if (validPoints.Count > 0)
                return validPoints[Random.Range(0, validPoints.Count)];

            currentMax += 5f; // Expand search area
        }
        return null;
    }
    #endregion

    #region State Tools
    public void ChangeSpecificState(SpecificEnemyState newState)
    {
        if (currentSpecificState == newState) return; // FIXED: Changed != to ==

        pastSpecificState = currentSpecificState;
        currentSpecificState = newState;
        Debug.Log($"State Changed to: {newState}");
    }

    private bool PerformRaycastDetection()
    {
        if (player == null) return false;

        Vector3 dir = player.transform.position - transform.position;
        RaycastHit hit;

        if (Physics.Raycast(transform.position, dir.normalized, out hit, detectionRadius, detectionMask))
        {
            if (hit.collider.CompareTag("Player"))
            {
                return true;
            }
        }
        return false;
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (!showDetectionRadius) return;
        Gizmos.color = canDetectPlayer ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        if (player != null)
            Gizmos.DrawLine(transform.position, player.transform.position);
    }
}
