
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

public enum SpecificEnemyState
{
    Chase,
    Investigate,
    Patrol,
    Grab,
    Throw,
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
    public ZeroGravity playerController;
    public ComplexEnemyAI complexEnemyAI;
    public SimpleEnemyAI simpleEnemyEnemyAI;
    //[SerializeField] private GameObject simpleEnemy;
    //[SerializeField] private GameObject complexEnemy;

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
    [SerializeField] private bool canRetreat;

    [Header("Throw Settings")]
    [SerializeField] private float throwDistanceCheck;
    [SerializeField] private float checkRadius;
    [SerializeField] private bool canThrow;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private int throwCheckLimit;
    [SerializeField] private Vector3 throwCheckOffset;

    [SerializeField] private bool canTakeHealth;

    [Header("Grab Settings")]
    [SerializeField] private float forceLookSpeedTime;

    [Header("Light Detection")]
    private bool isLightOn;
    private float lightDetectionRange;
    private float lightDetectionCooldown = 3f;
    private float lightDetectionDuration = 0f;
    private float lightDetectionTimer = 0f;

    [Header("Dynamic Value Stuff")]
    [SerializeField] private float minChaseDistance;
    [SerializeField] private float maxChaseDistance;
    [SerializeField] private float maxChaseSpeed;
    [SerializeField] private float minChaseSpeed;

    [Header("Gizmos")]
    [SerializeField] private bool showDetectionRadius;
    [SerializeField] private bool showRetreatRadius;

    private void Start()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (playerController == null) playerController = FindAnyObjectByType<ZeroGravity>();

        if (complexEnemyAI == null) complexEnemyAI = GetComponent<ComplexEnemyAI>(); 

        currentSpecificState = SpecificEnemyState.Patrol;
    }

    public void GeneralLogic()
    {
        if (enemyVersion == EnemyVersion.Complex)
        {
            if (currentGeneralState == GeneralEnemyState.Pause) return;

            // Run detection every frame
            canDetectPlayer = PerformRaycastDetection();

            //Debug.Log(canDetectPlayer);

            if (isLightOn)
            {
                //canDetectPlayer = FireDetectionGrid();
            }

            switch (currentGeneralState)
            {
                case GeneralEnemyState.Active:
                    HandleActiveLogic();
                    break;
                case GeneralEnemyState.Retreat:
                    HandleRetreatLogic();
                    break;
                case GeneralEnemyState.Idle:
                    break;
                case GeneralEnemyState.Dev:
                    break;

            }
        }
        else if (enemyVersion == EnemyVersion.Simple)
        {
            switch (currentGeneralState)
            {
                case GeneralEnemyState.Active:
                    HandleActiveLogic();
                    break;
                case GeneralEnemyState.Retreat:
                    HandleRetreatLogic();
                    break;

            }
        }
        
    }



    #region Active Logic
    private void HandleActiveLogic()
    {
        switch (currentSpecificState)
        {
            case SpecificEnemyState.Patrol:
                complexEnemyAI.isPatroling();

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

                    complexEnemyAI.investigatingWaypoint = null;
                    complexEnemyAI.lastSeenWaypoint = null;
                }
                break;

            case SpecificEnemyState.Chase:
                detectionRadius = chaseDetectionRadius;

                if (currentSpecificState == SpecificEnemyState.Chase)
                    //Add audio que here
                    complexEnemyAI.IsChasingPlayer();

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
                    if (complexEnemyAI.investigatingWaypoint != null)
                    {
                        complexEnemyAI.currentWaypoint = complexEnemyAI.investigatingWaypoint;
                    }

                    isInvestigating = false;
                    complexEnemyAI.investigatingWaypoint = null;
                    ChangeSpecificState(SpecificEnemyState.Patrol);
                }
                else
                {
                    // Increase distance threshold to 1.0f for more reliable detection
                    float dist = (complexEnemyAI.investigatingWaypoint != null) ? 
                        Vector3.Distance(transform.position, complexEnemyAI.investigatingWaypoint.transform.position) : 0f;

                    bool reachedPoint = complexEnemyAI.investigatingWaypoint == null || dist < 1.0f;

                    if (reachedPoint)
                    {
                        // Reset the flag so GetRandomInvestPoint can be called again
                        isInvestigating = false;

                        Waypoint searchOrigin = (complexEnemyAI.lastSeenWaypoint != null) ? 
                            complexEnemyAI.lastSeenWaypoint : complexEnemyAI.currentWaypoint;

                        if (searchOrigin != null && !isInvestigating)
                        {
                            complexEnemyAI.investigatingWaypoint = GetRandomInvestPoint(searchOrigin);
                            shouldFollow = (Random.value > 0.5f);
                            isInvestigating = true;
                        }
                    }
                    complexEnemyAI.IsInvestigating();
                }
                break;
        }
    }

    public float DetermineGeistSpeedChange()
    {
        float distance = Vector3.Distance(transform.position, player.transform.position);
        float t = Mathf.Clamp01(distance / maxChaseDistance);
        //MinChaseSpeed is the base speed, maxChaseSpeed is the max speed the geist can go
        float currentSpeed = Mathf.Lerp(minChaseSpeed, maxChaseSpeed, t);

        return currentSpeed;
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

        if (waypoints == null || waypoints.Count == 0)
        {
            // Fall back to the origin itself so the AI doesn't crash
            //Debug.LogWarning($"GetRandomInvestPoint: no neighbours on {OriginPoint?.name}");
            return OriginPoint;
        }

        return waypoints[Random.Range(0, waypoints.Count)];
    }
    private void GetLastSeenLocation()
    {
        playersLastKnownLocation = player.transform.position;

        complexEnemyAI.lastSeenWaypoint = complexEnemyAI.FindClosestWaypoint(playersLastKnownLocation);


    }

    #endregion

    #region Retreat Logic
    private void HandleRetreatLogic()
    {
        retreatTimer -= Time.deltaTime;
        if (retreatTimer <= 0)
        {
            ChangeGeneralState(GeneralEnemyState.Active);
            ChangeSpecificState(SpecificEnemyState.Patrol);
            return;
        }

        if (IsPlayerLookingAtMe())
        {
            // Only BFS if we don't have a path or the player is blocking it
            if (complexEnemyAI.path.Count == 0 || complexEnemyAI.CheckIfPlayerInWay())
            {
                complexEnemyAI.FindRetreatPath();
            }
            complexEnemyAI.TrackPath();
        }
        else
        {
            complexEnemyAI.TeleportToWaypoint();
            retreatTimer = 0;
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

    #region Idle Logic

    #endregion

    #region Grab/Throw/Attack Logic

    public void GrabPlayer()
    {
        // Start the sequence as a Coroutine so we can use "yield return" timers
        StartCoroutine(GrabAndThrowSequence());
    }

    private IEnumerator GrabAndThrowSequence()
    {
        // 1. Lock Player
        LockPlayerInputs();

        player.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;

        // 2. Force Look (Starts its own internal coroutine for camera)
        yield return StartCoroutine(RotateCameraToTarget(this.transform, forceLookSpeedTime));

        complexEnemyAI.DetermineThrowTarget(); // Finds where to throw

        // 3. Struggle Pause
        // This gives the player a moment to see the Geist's face before being launched
        yield return new WaitForSeconds(5.5f);

        // 4. Execute Throw
        // We call a new function in AI to handle the kinematic "launch"
        complexEnemyAI.ExecuteKinematicThrow(complexEnemyAI.throwLocation,10f);

        // 5. Cleanup
        yield return new WaitForSeconds(0.5f); // Brief moment of stun after throw
        UnlockPlayerInputs();
        GoIdle(); // Transitions state
    }

    public void GrabAttackLogic()
    {
        if (player.GetComponent<ZeroGravity>().PlayerHealth <= 1)
        {
            ChangeSpecificState(SpecificEnemyState.Kill);

        }
        else
        {
            //Can place the audio call here (place 2)
            ChangeSpecificState(SpecificEnemyState.Grab);
        }

    }

    public IEnumerator GrabAndWait()
    {
        //Can place the audio call here (place 1)
        yield return new WaitForSeconds(3.0f);
    }

    /*
    private Vector3 GetFallbackThrowPosition(Vector3 offset)
    {
        Vector3 startPos = transform.position + transform.TransformDirection(offset);
        Vector3 direction = -transform.forward;

        if (Physics.SphereCast(startPos, checkRadius, direction, out RaycastHit hit, throwDistanceCheck, wallLayer))
        {
            return startPos + direction * Mathf.Max(0f, hit.distance - 0.1f);
        }

        return startPos + direction * throwDistanceCheck;
    }

    private void SetThrowLocation(Vector3 offsetUsed)
    {
        Vector3 startPos = transform.position + transform.TransformDirection(offsetUsed);
        Vector3 direction = -transform.forward;
        complexEnemyAI.throwLocation = (startPos + direction * throwDistanceCheck);
    }
    */

    public bool HasSpaceBehind(float distance, float radius, Vector3 offset)
    {
        // 1. Calculate the starting point (Player position + your custom offset)
        // Using TransformDirection ensures the offset moves WITH the player's rotation
        Vector3 startPos = transform.position + transform.TransformDirection(offset);

        // 2. Define the direction (Straight back from where the player is facing)
        Vector3 direction = -transform.forward;

        // 3. Perform the SphereCast
        // We use a RaycastHit to see what we bumped into, if anything.
        if (Physics.SphereCast(startPos, radius, direction, out RaycastHit hit, distance))
        {
            // If we hit something, there isn't enough space.
            return false;
        }

        // If the SphereCast reaches the 'distance' without hitting anything, return true.
        return true;
    }
    private void LockPlayerInputs()
    {
        Debug.Log("player control locked");

        playerController.CanMove = false;
        playerController.StopRollingQuickly();

        // Kill all momentum and freeze physics entirely
        playerController.RB.linearVelocity = Vector3.zero;
        playerController.RB.angularVelocity = Vector3.zero;
        playerController.RB.isKinematic = true;

        canTakeHealth = true;

    }

    public void UnlockPlayerInputs()
    {
        Debug.Log("player control unlocked");
        playerController.CanMove = true;

        // Restore physics - but only if we're not about to throw
        // (GetThrown handles its own kinematic transition)
        if (!playerController.IsBeingThrown)
        {
            playerController.RB.isKinematic = false;
        }

        if (canTakeHealth)
        {
            Debug.Log("player took damage");
            playerController.PlayerHealth -= 1;
            canTakeHealth = false;
        }
        
    }

    public void GoIdle()
    {
        StartCoroutine(GoIdleRoutine());
    }

    private IEnumerator GoIdleRoutine()
    {
        UnlockPlayerInputs();
        ChangeGeneralState(GeneralEnemyState.Idle);
        yield return new WaitForSeconds(10.0f); // actually waits
        ChangeGeneralState(GeneralEnemyState.Active);
        ChangeSpecificState(SpecificEnemyState.Investigate);
    }

    IEnumerator RotateCameraToTarget(Transform target, float duration)
    {
        float elapsed = 0f;
        Quaternion startRotation = playerController.cam.transform.rotation;

        while (elapsed < duration)
        {
            // 1. Calculate the direction to the target
            Vector3 direction = target.position - playerController.cam.transform.position;

            // 2. Determine the target rotation (LookRotation)
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // 3. Smoothly interpolate between start and end
            playerController.cam.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / duration);

            elapsed += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Ensure it finishes exactly at the target
        playerController.cam.transform.LookAt(target);
    }

    #endregion

    #region Light Detection Logic

    private bool DetectPlayerFlashLight()
    {
        if (!isLightOn) return false;



        return false;
    }

    private void FireDetectionGrid(float columns, float rows, float spacing, float range)
    {
        // Calculate offsets to ensure the grid is centered on the forward vector
        float halfWidth = (columns - 1) * spacing / 2f;
        float halfHeight = (rows - 1) * spacing / 2f;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                float xOffset = (x * spacing) - halfWidth;
                float yOffset = (y * spacing) - halfHeight;

                Vector3 rayDirection = transform.forward + (transform.right * xOffset) + (transform.up * yOffset);

                rayDirection.Normalize();

                if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, range))
                {

                    // Add hit logic here
                }
            }
        }
    }
    #endregion

    #region State Tools
    public void ChangeSpecificState(SpecificEnemyState newState)
    {
        if (currentSpecificState == newState) return;

        // Clear pathing when changing states to prevent "Ghost Paths"
        if (complexEnemyAI != null)
        {
            complexEnemyAI.path.Clear();
            complexEnemyAI.targetWaypoint = null;
        }

        pastSpecificState = currentSpecificState;
        currentSpecificState = newState;
        //Debug.Log($"State Changed to: {newState}");
    }

    public void ChangeGeneralState(GeneralEnemyState newState)
    {
        if (currentGeneralState == newState) return;

        // Clear pathing when changing states to prevent "Ghost Paths"
        if (complexEnemyAI != null)
        {
            complexEnemyAI.path.Clear();
            complexEnemyAI.targetWaypoint = null;
        }

        pastGeneralState = currentGeneralState;
        currentGeneralState = newState;
        //Debug.Log($"State Changed to: {newState}");
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
                //Debug.Log("Can Detect Player");
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
