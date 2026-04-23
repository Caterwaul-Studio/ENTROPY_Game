using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ComplexEnemyAI : MonoBehaviour
{

    [Header("Movement")]
    public float speed = 3.0f;
    public bool useRandomRoaming;
    public float setRotationSpeed = 2.5f;
    private float rotationSpeed = 2.5f;

    [Header("Stun Settings")]
    public float stunSeconds = 3f;
    public float stunVelocityThreshold = 4f;

    [Header("References")]
    public GameObject player;
    public ZeroGravity playerController;
    public Waypoint startingWaypoint;
    public Transform waypointGroup;
    public EnemyStateMachine enemyStateMachine;
    public AudioSource farMusic; //part of temp system
    public AudioSource nearMusic; //part of temp system
    public AudioSource hitSource; //part of temp system
    public AudioClip[] hitAudioBank; //part of temp system
    //public DoorScript door;

    [Header("Tendril Settings")]
    public GameObject tendrilPrefab;
    public List<TendrilOrigin> tendrilOrigins;
    public List<TendrilOrigin> backwardsOrigins = new List<TendrilOrigin>();
    public float spawnInterval = 4f;
    private float lastTendrilTime = 0f;

    [Header("Optimization")]
    public float wakeDistance = 50f;

    //Line of sight
    public LayerMask barrierLayer; // Set this to "Barrier"
    public float wakeLossCooldown = 10f;
    private float timeSinceLastSeenPlayer = 0f;
    private bool hasLineOfSight = false;

    // Internal state
    public Waypoint currentWaypoint;
    public Queue<Waypoint> path = new Queue<Waypoint>();
    public Waypoint playerWaypoint;
    public Waypoint targetWaypoint;
    public Waypoint retreatWaypoint;
    public Waypoint lastSeenWaypoint;
    public Waypoint investigatingWaypoint;

    private List<Waypoint> allWaypoints = new List<Waypoint>();
    private List<Waypoint> roamingWaypoints = new List<Waypoint>();
    private List<Waypoint> SpawnWaypoints = new List<Waypoint>();
    private Waypoint goalWaypoint;


    //private float distanceToPlayer;
    private float sqrDist;
    public bool isChasingPlayer = false;
    public bool isStunned = false;

    public bool shouldPlaySting = true;
    //public bool isAwake = false;

    //direction calculation
    private float directionUpdateThreshold = 0.05f; // Minimal movement to update direction
    private Vector3 lastPosition;
    private Vector3 currentDirection;
    private Vector3 retreatDirection;

    [Header("Stuck Recovery")]
    private Vector3 lastProgressPosition; // Tracks position at intervals
    private float stuckTimer = 0f;
    public float stuckThreshold = 2f;
    private float progressCheckFrequency = 0.5f; // Check progress twice a second
    private float nextProgressCheckTime = 0f;

    //public Vector3 initialPosition;
    private Quaternion initialRotation;

    private float resetCooldown = 0.2f;
    public List<TendrilOrigin> availableOrigins;

    //checking for clear path
    private float clearPathCheckCooldown = 0.25f;
    private float clearPathCheckTimer = 0f;
    private bool hasClearPath = true;

    [Header("Retreat")]

    [SerializeField] private Waypoint FailSafe; //This point will be the failsafe for if the retreating doesn't work correctly

    [Header("Throw")]
    public Vector3 throwLocation; // set via SetThrowLocation()
    public float minThrowDistance = 5f;
    [SerializeField] private float throwForce = 20f;


    void Start()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (playerController == null) playerController = FindAnyObjectByType<ZeroGravity>();

        
        allWaypoints = waypointGroup.GetComponentsInChildren<Waypoint>().ToList();
        foreach (Waypoint wp in allWaypoints)
        {
            if (wp.type == Waypoint.WaypointType.Roaming)
            {
                roamingWaypoints.Add(wp);
            }
        }

        rotationSpeed = setRotationSpeed;
        // Set the current waypoint to the starting one
        currentWaypoint = startingWaypoint;

        //initialPosition = transform.position;
        initialRotation = transform.rotation;

        FindPlayerPath();

        //playerController = player.GetComponent<ZeroGravity>();

        // Rigidbody must exist and start in kinematic mode
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        lastPosition = transform.position;

        tendrilOrigins = GetComponentsInChildren<TendrilOrigin>().ToList();
        availableOrigins = new List<TendrilOrigin>(tendrilOrigins);

    }

    // ... Headers and Variables stay the same ...

    void Update()
    {
        hasLineOfSight = enemyStateMachine.canDetectPlayer;

        if (enemyStateMachine.enemyVersion == EnemyVersion.Complex)
        {
            speed = enemyStateMachine.DetermineGeistSpeedChange();

            if (resetCooldown > 0f || isStunned)
            {
                resetCooldown -= Time.deltaTime;
                return;
            }

            sqrDist = (player.transform.position - transform.position).sqrMagnitude;

            enemyStateMachine.GeneralLogic();

            // Movement execution
            switch (enemyStateMachine.currentSpecificState)
            {
                case SpecificEnemyState.Chase:
                    IsChasingPlayer();
                    break;
                case SpecificEnemyState.Patrol:
                    isPatroling();
                    break;
                case SpecificEnemyState.Investigate:
                    IsInvestigating();
                    break;
                case SpecificEnemyState.Retreat:
                    TrackPath();
                    break;
                case SpecificEnemyState.Stunned:
                    break;
                    /*
                case SpecificEnemyState.Lunge:
                case SpecificEnemyState.Charge:
                    Chargelunge();
                    break;
                    */
            }

            CalculateDirection();
            RotateTowardsDirection();

            // --- IMPROVED STUCK CHECK ---
            if (Time.time >= nextProgressCheckTime)
            {
                // Only check progress if the AI is in a state where it should be moving
                bool shouldBeMoving = enemyStateMachine.currentSpecificState != SpecificEnemyState.Kill &&
                                      enemyStateMachine.currentSpecificState != SpecificEnemyState.Stunned &&
                                      enemyStateMachine.currentSpecificState != SpecificEnemyState.Grab &&
                                      enemyStateMachine.currentSpecificState != SpecificEnemyState.Throw
                                      ;

                if (shouldBeMoving)
                {
                    // If we moved less than 0.3 units in the last 0.5 seconds
                    if (Vector3.Distance(transform.position, lastProgressPosition) < 0.3f)
                    {
                        stuckTimer += progressCheckFrequency;
                        if (stuckTimer >= stuckThreshold)
                        {
                            HandleStuckReset();
                        }
                    }
                    else
                    {
                        stuckTimer = 0f;
                        lastProgressPosition = transform.position;
                    }
                }
                nextProgressCheckTime = Time.time + progressCheckFrequency;
            }
        }

        //** M U S I C - Z O N E ** (temp)

        //part of temporary audio system, to be replaced
        //Debug.Log(Vector3.Distance(player.transform.position, transform.position));
        farMusic.volume = Vector3.Distance(player.transform.position, transform.position) / 10;
        nearMusic.volume = 1 - Vector3.Distance(player.transform.position, transform.position) / 10;
        if (shouldPlaySting)
        {
            StartCoroutine(AlienHit());
        }
        //end music zone



    }

    private void HandleStuckReset()
    {
        Debug.Log("<color=orange>Geist stuck! Clearing path and finding nearest waypoint.</color>");
        stuckTimer = 0f;
        /*
         * 
        path.Clear();
        targetWaypoint = null;
        */
        // Reset to the absolute closest waypoint so it doesn't try to go 'through' the corner
        currentWaypoint = FindClosestWaypoint(transform.position);
       
        lastProgressPosition = transform.position;

        transform.position = currentWaypoint.transform.position;
    }


    public void IsChasingPlayer()
    {
        if (enemyStateMachine.canDetectPlayer)
        {
            ChasePlayer(); // Direct LOS movement
            path.Clear();  // Clear old pathfinding data
        }
        else
        {
            FindPlayerPath(); // Re-calculate breadcrumbs to last seen position
            TrackPath();      // Follow the breadcrumbs
        }
    }

    public void TrackPath()
    {
        if (path == null || path.Count == 0) return;

        targetWaypoint = path.Peek();

        // Move toward target
        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.transform.position, speed * Time.deltaTime);

        // Use a 0.5f threshold - much safer for MoveTowards
        if (Vector3.Distance(transform.position, targetWaypoint.transform.position) < 0.5f)
        {
            currentWaypoint = path.Dequeue();
        }
    }

    public void isPatroling()
    {

        if (currentWaypoint.type == Waypoint.WaypointType.General)
        {
            RoamArea();

        }
        else
        {
            RoamLimited();
        }
    }

    #region Investigation

    public void IsInvestigating()
    {


        if (enemyStateMachine.shouldFollow)
        {
            // Only calculate a path if we don't have one
            if (path.Count == 0)
            {
                path = BFS(currentWaypoint, investigatingWaypoint);
            }
            TrackPath();
        }
        else
        {
            // Direct move if "shouldFollow" is false (ghosting through walls)
            transform.position = Vector3.MoveTowards(transform.position, investigatingWaypoint.transform.position, speed * Time.deltaTime);
            UpdateCurrentWaypointToClosest();
        }
    }

    /*
-- All the times are able to be changed --
1.lose sight of the player
2.get the last point the geist saw the player
3.Start heading towards the closest waypoint to the last seen point
3.after a few seconds around 1-2 seconds, get another closet point to the player
4.search generally around the last point gotten, for around 4 - 5 seconds

 */
    protected void AdvanceInvestigation()
    {
        if (enemyStateMachine.shouldFollow)
        {
            // Only calculate a path if we don't have one
            if (path.Count == 0)
            {
                path = BFS(currentWaypoint, investigatingWaypoint);
            }
            TrackPath();
        }
        else
        {
            // Direct move if "shouldFollow" is false (ghosting through walls)
            transform.position = Vector3.MoveTowards(transform.position, investigatingWaypoint.transform.position, speed * Time.deltaTime);
            UpdateCurrentWaypointToClosest();
        }

        if (currentWaypoint == investigatingWaypoint)
        {

        }
    }

    #endregion

    void ChasePlayer()
    {
        // Move directly towards the player
        transform.position = Vector3.MoveTowards(transform.position, player.transform.position, speed * Time.deltaTime);

        UpdateCurrentWaypointToClosest();
    }

    #region Roam

    void RoamArea()
    {
        // Only pick a new goal if we are done with the current path
        if (path.Count == 0)
        {
            /*
            List<Waypoint> farWaypoints = allWaypoints.Where(wp =>
                Vector3.Distance(transform.position, wp.transform.position) > 20f
            ).ToList();
            */
            List<Waypoint> farWaypoints = allWaypoints.Where(wp => wp != currentWaypoint).ToList();

            if (farWaypoints.Count > 0)
            {
                goalWaypoint = farWaypoints[Random.Range(0, farWaypoints.Count)];
                path = BFS(currentWaypoint, goalWaypoint);
            }
        }

        TrackPath(); // This actually moves the Geist
    }

    void RoamLimited()
    {
        // If we finished the current waypoint, pick a neighbor and put it in the path
        if (path.Count == 0)
        {
            List<Waypoint> roamingNeighbors = currentWaypoint.neighbors
                .Where(n => n != null && n.type == Waypoint.WaypointType.Roaming)
                .ToList();

            if (roamingNeighbors.Count > 0)
            {
                Waypoint next = roamingNeighbors[Random.Range(0, roamingNeighbors.Count)];
                path.Enqueue(next);
            }
        }

        TrackPath(); // Move!
    }

    #endregion

    void FindPlayerPath()
    {
        if (playerWaypoint == null)
        {
            playerWaypoint = FindClosestWaypoint(player.transform.position);
            path = BFS(currentWaypoint, playerWaypoint);
        }
        else
        {
            Waypoint testWaypoint = FindClosestWaypoint(player.transform.position);
            //only do a new BFS if the player waypoint is new.
            if (playerWaypoint != testWaypoint)
            {
                playerWaypoint = testWaypoint;
                path = BFS(currentWaypoint, playerWaypoint);
            }
        }

    }

    #region Retreat
    public void FindRetreatPath()
    {
        enemyStateMachine.GetRandomValidPoint();

        path = BFS(currentWaypoint, retreatWaypoint);
    }

    public void TeleportToWaypoint()
    {
        if (retreatWaypoint == null) return;

        currentWaypoint = retreatWaypoint;
    }

    public void MoveThanTeleportInPointDirection()
    {
        if (retreatDirection == Vector3.zero)
        {
            retreatDirection = GetRandomPointOpposite(player.transform.position, 5, 45);
        }
        
        transform.position = Vector3.MoveTowards(transform.position, retreatDirection, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, retreatDirection) < 0.4f)
        {
            TeleportToWaypoint();
        }
    }

    public bool CheckIfPlayerInWay()
    {
        Waypoint tempPlayerWaypoint = FindClosestWaypoint(player.transform.position);

        Queue<Waypoint> tempPath = BFS(currentWaypoint, retreatWaypoint);

        foreach (Waypoint temp in tempPath)
        {
            if (temp == tempPlayerWaypoint)
            {
                return true;
            }
        }

        return false;
    }

    public Vector3 GetRandomPointOpposite(Vector3 targetPos, float distance, float angleSpread)
    {
        Vector3 awayDir = (transform.position - targetPos).normalized;

        float randomAngle = Random.Range(-angleSpread, angleSpread);
        Quaternion rotation = Quaternion.Euler(0, randomAngle, 0);
        Vector3 randomDir = rotation * awayDir;

        return transform.position + (randomDir * distance);
    }
    #endregion

    #region Throw/Attack

    void ForceLookAtPlayer()
    {
        Vector3 toPlayer = (player.transform.position - transform.position);
        if (toPlayer.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void ThrowPlayerAt(Vector3 throwLocation)
    {
        // Direction from the enemy toward the computed throw target
        Vector3 direction = (throwLocation - transform.position).normalized;


        playerController.GetThrown(direction, throwForce);
    }

    private void ThrowPlayer() => StartCoroutine(ThrowSequence());
    
    private IEnumerator ThrowSequence()
    {
        ForceLookAtPlayer();
        // Wait for the grab animation / hold duration
        yield return enemyStateMachine.GrabAndWait();

        // Now actually apply the throw
        ThrowPlayerAt(throwLocation);

        // Return control to the enemy
        enemyStateMachine.UnlockPlayerInputs();
        enemyStateMachine.GoIdle();
    }

    private void KillPlayer() => StartCoroutine(KillSequence());

    private IEnumerator KillSequence()
    {
        ForceLookAtPlayer();
        yield return StartCoroutine(enemyStateMachine.GrabAndWait());
        if (!playerController.IsDead)
        {
            playerController.IsDead = true;
            isChasingPlayer = false;
        }
    }

    public void ExecuteKinematicThrow(Vector3 targetPos, float force)
    {
        // Calculate direction from player to the landing spot
        Vector3 dir = (targetPos - player.transform.position).normalized;

        // Tell the player controller to move kinematically towards that direction
        if (playerController != null)
        {
            // Use the "GetThrown" logic we discussed to apply velocity over time
            playerController.GetThrown(dir, force);
        }
    }

    public bool DetermineThrowTarget()
    {
        Vector3 origin = player.transform.position;
        // Direction directly away from the Geist
        Vector3 primaryDir = (player.transform.position - transform.position).normalized;

        // 1. Check the primary direction first
        if (IsSpaceClear(origin, primaryDir, minThrowDistance, out throwLocation))
        {
            return true;
        }

        // 2. If primary is blocked, check 5 random offsets
        // We create a "cone" of search directions
        for (int i = 0; i < 5; i++)
        {
            // Generate a random offset vector
            Vector3 randomOffset = new Vector3(
                Random.Range(-0.4f, 0.4f),
                Random.Range(-0.4f, 0.4f),
                Random.Range(-0.4f, 0.4f)
            );

            Vector3 branchedDir = (primaryDir + randomOffset).normalized;

            if (IsSpaceClear(origin, branchedDir, minThrowDistance, out throwLocation))
            {
                return true;
            }
        }

        // 3. Fallback: Find a Waypoint that is "far enough" but still "close enough"
        return FindFallbackWaypoint();
    }

    private bool IsSpaceClear(Vector3 origin, Vector3 direction, float distance, out Vector3 hitPoint)
    {
        RaycastHit hit;
        // We use a SphereCast to ensure the PLAYER fits through the gap, not just a thin line
        if (Physics.SphereCast(origin, 0.5f, direction, out hit, distance))
        {
            hitPoint = hit.point;
            return false; // Hit a wall
        }

        // Path is clear! Set target to the end of the ray
        hitPoint = origin + (direction * distance);
        return true;
    }

    private bool FindFallbackWaypoint()
    {
        // Filter waypoints: Distance between 5m and 15m away
        var validWaypoints = allWaypoints.Where(wp => {
            float d = Vector3.Distance(player.transform.position, wp.transform.position);
            return d >= 5f && d <= 15f;
        }).ToList();

        if (validWaypoints.Count > 0)
        {
            throwLocation = validWaypoints[Random.Range(0, validWaypoints.Count)].transform.position;
            return true;
        }

        // Absolute Last Resort: Just throw them exactly where the Geist is (a shove)
        throwLocation = transform.position;
        return false;
    }

    #endregion

    // Called by Unity when this collider hits another collider
    private void OnCollisionEnter(Collision collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("PickupObject"))
        {
            Rigidbody objRb = collision.rigidbody;
            if (objRb != null && objRb.linearVelocity.magnitude >= stunVelocityThreshold)
            {
                Debug.Log("Object hit at this speed: " + objRb.linearVelocity.magnitude);
                // StartCoroutine(StunCoroutine());
            }
        }
        else if (other.CompareTag("Player"))
        {
            enemyStateMachine.GrabAttackLogic();

            if (enemyStateMachine.currentSpecificState == SpecificEnemyState.Grab)
            {
                ThrowPlayer();
            }
            else if (enemyStateMachine.currentSpecificState == SpecificEnemyState.Kill)
            {
                KillPlayer();
            }

            // Knockback regardless of state
            /*
            Rigidbody playerRb = other.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                Vector3 forceDirection = (other.transform.position - transform.position).normalized;
                float knockbackStrength = lungeSpeed * 2f;
                playerRb.AddForce(forceDirection * knockbackStrength, ForceMode.Impulse);
            }
            */
        }
    }

    #region Path Finding
    Queue<Waypoint> BFS(Waypoint start, Waypoint goal)
    {
        // GUARD: If start or goal is null, return an empty path instead of crashing
        if (start == null || goal == null)
        {
            Debug.LogWarning($"BFS stopped: Start is {(start == null ? "NULL" : "Valid")}, Goal is {(goal == null ? "NULL" : "Valid")}");
            return new Queue<Waypoint>();
        }

        Queue<Waypoint> queue = new Queue<Waypoint>();
        Dictionary<Waypoint, Waypoint> cameFrom = new Dictionary<Waypoint, Waypoint>();

        queue.Enqueue(start);
        cameFrom[start] = null;

        while (queue.Count > 0)
        {
            Waypoint current = queue.Dequeue();
            if (current == goal) break;

            foreach (Waypoint neighbor in current.neighbors)
            {
                // GUARD: Skip neighbors that might be missing in the Inspector
                if (neighbor == null) continue;

                if (!cameFrom.ContainsKey(neighbor))
                {
                    queue.Enqueue(neighbor);
                    cameFrom[neighbor] = current;
                }
            }
        }

        Queue<Waypoint> path = new Queue<Waypoint>();

        // If goal was never reached (no path exists)
        if (!cameFrom.ContainsKey(goal)) return path;

        Stack<Waypoint> reversePath = new Stack<Waypoint>();
        for (Waypoint at = goal; at != null; at = cameFrom[at])
        {
            reversePath.Push(at);
        }

        if (reversePath.Count > 0 && Vector3.Distance(transform.position, reversePath.Peek().transform.position) < 0.1f)
        {
            reversePath.Pop();
        }

        while (reversePath.Count > 0)
        {
            path.Enqueue(reversePath.Pop());
        }

        return path;
    }

    public Waypoint FindClosestWaypoint(Vector3 position)
    {
        // Use the cached list from Start() — zero allocation:
        Waypoint closest = null;
        float minSqrDist = Mathf.Infinity;

        foreach (Waypoint waypoint in allWaypoints)
        {
            float sqrDist = (position - waypoint.transform.position).sqrMagnitude;
            if (sqrDist < minSqrDist)
            {
                closest = waypoint;
                minSqrDist = sqrDist;
            }
        }
        return closest;
    }

    void UpdateCurrentWaypointToClosest()
    {
        // FIX: If we have no waypoint at all, find the absolute closest one in the world
        if (currentWaypoint == null)
        {
            currentWaypoint = FindClosestWaypoint(transform.position);

            // If it's STILL null (meaning no waypoints exist in the scene), stop here
            if (currentWaypoint == null) return;
        }

        Waypoint closest = currentWaypoint;
        float minSqrDist = (transform.position - currentWaypoint.transform.position).sqrMagnitude;

        foreach (Waypoint neighbor in currentWaypoint.neighbors)
        {
            // Safety check for individual neighbor slots in the Inspector
            if (neighbor == null) continue;

            float sqrDist = (transform.position - neighbor.transform.position).sqrMagnitude;
            if (sqrDist < minSqrDist)
            {
                closest = neighbor;
                minSqrDist = sqrDist;
            }
        }

        if (closest != currentWaypoint)
        {
            currentWaypoint = closest;
        }
    }

    void SpawnTendril()
    {
        if (tendrilOrigins.Count == 0 || tendrilPrefab == null) return;
        if (availableOrigins.Count == 0) return;

        // Pick random available origin
        int index = Random.Range(0, availableOrigins.Count);
        TendrilOrigin origin = availableOrigins[index];

        // Spawn the tendril as a child of the origin
        GameObject t = Instantiate(tendrilPrefab, origin.transform.position, origin.transform.rotation, origin.transform);
        TendrilBehavior tb = t.GetComponent<TendrilBehavior>();
        if (tb != null)
        {
            tb.Initialize(origin, this, false);  // Pass the origin and owner
            origin.activeTendril = tb;
        }

        // Remove the used origin from available list
        availableOrigins.RemoveAt(index);
    }

    void CalculateDirection()
    {
        Vector3 displacement = transform.position - lastPosition;

        // Only update direction if moved more than threshold
        if (displacement.sqrMagnitude > directionUpdateThreshold * directionUpdateThreshold)
        {
            currentDirection = displacement.normalized;
            lastPosition = transform.position;
        }
    }

    void RotateTowardsDirection()
    {
        if (currentDirection.sqrMagnitude < 0.001f) return; // No direction to face

        Quaternion targetRotation = Quaternion.LookRotation(currentDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    #endregion

    /// <summary>
    /// Resets the alien to its original position and state.
    /// </summary>
    public void ResetToStart()
    {
        Debug.Log("Alien Reset called");

        // Stop everything
        StopAllCoroutines();
        //isAwake = false;
        isStunned = false;
        isChasingPlayer = false;

        // Reset cooldown to block Update logic for 0.1s
        resetCooldown = 0.2f;

        // Clear movement and reset position
        transform.position = startingWaypoint.transform.position;
        transform.rotation = initialRotation;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            //rb.MovePosition(startingWaypoint.transform.position);
        }

        // Reset AI/pathing state
        path.Clear();
        currentWaypoint = startingWaypoint;
        targetWaypoint = null;

        // Kill any tendrils
        foreach (var origin in tendrilOrigins)
        {
            if (origin.activeTendril != null)
            {
                origin.activeTendril.Retract();
                origin.activeTendril = null;
            }
        }

        availableOrigins.Clear();
        availableOrigins.AddRange(tendrilOrigins);

        // Start line of sight tracking after a short delay
        //StartCoroutine(DelayedWake());
    }
    /*
    IEnumerator DelayedWake()
    {
        yield return null; // wait 1 frame to ensure position is stable
        StartCoroutine(UpdateLineOfSight());
    }
    */
    void OnDrawGizmosSelected()
    {
        // Only draw when selected in the editor
        Gizmos.color = Color.yellow;

        // Draw a forward-facing line from the transform
        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * 2f; // adjust length as needed
        Gizmos.DrawLine(start, end);

        // Wake distance (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wakeDistance);
    }

    IEnumerator AlienHit()
    {
        //Debug.Log("waiting");
        shouldPlaySting = false;
        hitSource.clip = hitAudioBank[Random.Range(0, hitAudioBank.Length)];
        var waitTime = Random.Range(5, 15);
        yield return new WaitForSeconds(waitTime);
        //Debug.Log("playing");
        hitSource.Play();
        shouldPlaySting = true;
    }
}
