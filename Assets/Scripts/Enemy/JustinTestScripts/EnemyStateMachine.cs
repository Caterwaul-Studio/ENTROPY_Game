using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpecificEnemyState
{
    Chase,
    Track,
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
    Dev,
    Idle, // Special State for certain circumstances
}

public enum EnemyVersion
{
    Complex,
    Simple
}

public class EnemyStateMachine : MonoBehaviour
{
    [Header("Enum Values")]
    public EnemyVersion enemyVersion;
    public GeneralEnemyState currentGeneralState = GeneralEnemyState.Active;
    public GeneralEnemyState pastGeneralState;
    public SpecificEnemyState currentSpecificState = SpecificEnemyState.Patrol;
    public SpecificEnemyState pastSpecificState;

    [Header("References")]
    public GameObject player;
    [SerializeField] private ComplexEnemyAI complecEnemyAI;
    [SerializeField] private GameObject simpleEnemy;
    [SerializeField] private GameObject complexEnemy;

    [Header("Detection Settings")]
    public LayerMask detectionMask; // Set this to "Default", "Player", and "Obstacles"
    public float detectionRadius;
    public float defaultDetectionRadius = 10f;
    public float chaseDetectionRadius = 15f;
    public float detectionDuration = 1.5f; // Time to "spot" player
    
    [SerializeField] private float detectionTimer;
    public bool canDetectPlayer = false;
    [SerializeField] private bool chasePlayer = false;

    [Header("Interest/Search Settings")]
    public float interestDuration = 10f;
    [SerializeField] private float interestTimer;
    public Vector3 playersLastKnownLocation;
    private bool isInvestigating;
    public bool shouldFollow;

    [Header("Retreat Settings")]
    public float retreatDuration = 3f;
    public float retreatDistanceCheck = 20f;
    public LayerMask barrierLayer;
    public LayerMask waypointLayer;
    public float minRadius = 7f;
    public float maxRadius = 9f;
    [SerializeField] private float retreatTimer;
    [SerializeField] private float retreatPointReroll;

    [Header("Gizmos")]
    [SerializeField] private bool showDetectionRadius;
    [SerializeField] private bool showRetreatRadius;

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
        else if (currentGeneralState == GeneralEnemyState.Dev)
        {

        }
    }

    #region Active Logic
    private void HandleActiveLogic()
    {
        switch (currentSpecificState)
        {
            case SpecificEnemyState.Patrol:
                complecEnemyAI.isPatroling();

                detectionRadius = defaultDetectionRadius;

                if (canDetectPlayer)
                {
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
                    chasePlayer = false;
                    detectionTimer = detectionDuration;

                    complecEnemyAI.investigatingWaypoint = null;
                    complecEnemyAI.lastSeenWaypoint = null;
                }
                break;

            case SpecificEnemyState.Chase:
                detectionRadius = chaseDetectionRadius;

                complecEnemyAI.IsChasingPlayer();
                if (!canDetectPlayer)
                {
                    interestTimer = interestDuration;
                    GetLastSeenLocation();
                    isInvestigating = false;
                    ChangeSpecificState(SpecificEnemyState.Investigate);
                }
                break;

            case SpecificEnemyState.Investigate:
                interestTimer -= Time.deltaTime;

                if (canDetectPlayer)
                {
                    ChangeSpecificState(SpecificEnemyState.Chase);
                    isInvestigating = false;
                }
                // Only switch to Patrol if the timer is DONE
                else if (interestTimer <= 0)
                {
                    if (complecEnemyAI.investigatingWaypoint != null)
                    {
                        complecEnemyAI.currentWaypoint = complecEnemyAI.investigatingWaypoint;
                    }

                    isInvestigating = false;
                    complecEnemyAI.investigatingWaypoint = null;
                    ChangeSpecificState(SpecificEnemyState.Patrol);
                }
                else
                {
                    // Increase distance threshold to 1.0f for more reliable detection
                    float dist = (complecEnemyAI.investigatingWaypoint != null) ? 
                        Vector3.Distance(transform.position, complecEnemyAI.investigatingWaypoint.transform.position) : 0f;

                    bool reachedPoint = complecEnemyAI.investigatingWaypoint == null || dist < 1.0f;

                    if (reachedPoint)
                    {
                        // Reset the flag so GetRandomInvestPoint can be called again
                        isInvestigating = false;

                        Waypoint searchOrigin = (complecEnemyAI.lastSeenWaypoint != null) ? 
                            complecEnemyAI.lastSeenWaypoint : complecEnemyAI.currentWaypoint;

                        if (searchOrigin != null && !isInvestigating)
                        {
                            complecEnemyAI.investigatingWaypoint = GetRandomInvestPoint(searchOrigin);
                            shouldFollow = (Random.value > 0.5f);
                            isInvestigating = true;
                        }
                    }
                    complecEnemyAI.IsInvestigating();
                }
                break;
        }
    }
    #endregion

    #region Investigate
    public List<Waypoint> FindInvestWaypoints(Waypoint OriginPoint)
    {
        // Safety check to prevent NullReferenceException
        if (OriginPoint == null) return new List<Waypoint>();

        HashSet<Waypoint> waypoints = new HashSet<Waypoint>();

        // Layer 1: Only check the direct neighbors of the starting point
        foreach (Waypoint neighbor in OriginPoint.neighbors)
        {
            if (neighbor != null)
            {
                waypoints.Add(neighbor);
            }
        }

        // We no longer loop through 'neighbor.neighbors', effectively limiting the search
        return new List<Waypoint>(waypoints);
    }

    public Waypoint GetRandomInvestPoint(Waypoint OriginPoint)
    {
        List<Waypoint> waypoints = FindInvestWaypoints(OriginPoint);

        return waypoints[Random.Range(0, waypoints.Count)];
    }
    private void GetLastSeenLocation()
    {
        playersLastKnownLocation = player.transform.position;

        complecEnemyAI.lastSeenWaypoint = complecEnemyAI.FindClosestWaypoint(playersLastKnownLocation);


    }

    #endregion

    #region Retreat Logic

    private void HandleRetreatLogic()
    {
        retreatTimer -= Time.deltaTime;

        // If timer ends or player loses LOS, find a path
        if (retreatTimer <= 0)
        {
            ChangeGeneralState(GeneralEnemyState.Active);

            ChangeSpecificState(SpecificEnemyState.Patrol);

            //complecEnemyAI.FindRetreatPath();
        }

        if (IsPlayerLookingAtMe())
        {
            if (complecEnemyAI.CheckIfPlayerInWay())
            {
                //if the player is in the way attempt to find an alternative route,
                //Try 5 more Points
                while (retreatPointReroll < 5)
                {
                    if(!complecEnemyAI.CheckIfPlayerInWay())
                    {
                        break;
                    }

                    complecEnemyAI.FindRetreatPath();

                    retreatPointReroll++;
                }
            }

            //but if there are no vaild alternative routes have the geist move through the walls
            if ()
            {
                complecEnemyAI.MoveThanTeleportInPointDirection();
            }

            //if player still has LOS move down path towards retreat point
            else if (!complecEnemyAI.CheckIfPlayerInWay())
            {
                complecEnemyAI.FindRetreatPath();
            }
                
        }

        else if (!IsPlayerLookingAtMe())
        {
            GetRandomValidPoint();

            complecEnemyAI.TeleportToWaypoint();
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
        if (currentSpecificState == newState) return;

        // Clear pathing when changing states to prevent "Ghost Paths"
        if (complecEnemyAI != null)
        {
            complecEnemyAI.path.Clear();
            complecEnemyAI.targetWaypoint = null;
        }

        pastSpecificState = currentSpecificState;
        currentSpecificState = newState;
        Debug.Log($"State Changed to: {newState}");
    }

    public void ChangeGeneralState(GeneralEnemyState newState)
    {
        if (currentGeneralState == newState) return;

        // Clear pathing when changing states to prevent "Ghost Paths"
        if (complecEnemyAI != null)
        {
            complecEnemyAI.path.Clear();
            complecEnemyAI.targetWaypoint = null;
        }

        pastGeneralState = currentGeneralState;
        currentGeneralState = newState;
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

    #region Dev Tools



    #endregion

    private void OnDrawGizmos()
    {
        if (showDetectionRadius)
        {
            Gizmos.color = canDetectPlayer ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            if (player != null)
                Gizmos.DrawLine(transform.position, player.transform.position);
        }
        
        if (showRetreatRadius)
        {
            Gizmos.color = Color.blue;

            Gizmos.DrawWireSphere(transform.position, minRadius);
            Gizmos.DrawWireSphere(transform.position, maxRadius);
        }
    }
}
