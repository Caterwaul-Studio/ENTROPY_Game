using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.AppUI.Core;
using Unity.Behavior;
using UnityEngine;


public class GeistDirectorBrain : MonoBehaviour
{
    [Header("Gizmos")]
    [SerializeField] private bool showDetectionRadius;
    [SerializeField] private bool showLOSCone;
    [SerializeField] private bool showRetreatRadius;

    [Header("References")]
    [SerializeField] private PersistantManager persistantManager;
    [SerializeField] private GameObject playerObj;
    [SerializeField] private ZeroGravity player;

    [Header("GeistObjs")]
    [SerializeField] private BehaviorGraphAgent geist;
    [SerializeField] private GameObject grabPlayerPos;
    [SerializeField] public GeistStepper GeistRig;
    [Header("Vision Cone (passed from GeistLookAt.cs: editing these values does nothing)")]
    [SerializeField] public Transform GeistEye;
    public Transform target;
    public float coneHalfAngle;
    [SerializeField] public GeistLookAt GeistEyeRig;
    [SerializeField] public float geistEyeRangeMax;
    [SerializeField] private string playerTag = "Player";

    private string modeVariableName = "New GeistEnums";
    BlackboardVariable<bool> playerInAOE,
        newPlayerInAOE,
        playerInLOS,
        playerInGrabRange,
        playerHealthAboveOne,
        playerEnteredPoweredArea,
        playerWithinChaseDistance,
        playerShineFlashlightAtGeist,
        geistBlinded,
        geistHeardSoundOfInterest,
        newElectricalZoneActivated,
        geistInPoweredZone,
        newInterestPt;
    [Header("NewActiveElectricalZone")]
    BlackboardVariable<Transform> newActiveElectricalZone;

    [Header("Waypoints")]
    [SerializeField] private Transform wayPointGroup;
    [SerializeField] private Waypoint startingWaypoint;

    [Header("Phasing")]
    [SerializeField] private List<Waypoint> phasingWaypoints = new List<Waypoint>();

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 2.5f;
    [SerializeField] private float arriveThreshold = 0.5f; //Distance at which a waypoint counts as reached (original used 0.5)

    //ported from EnemyStateMachine
    [Header("Detection Settings")]
    BlackboardVariable<Transform> lastKnownPlayerPos;
    [SerializeField] private float closeSightDistance = 2f; // point blank, the Geist can't "lose" the player
    public LayerMask detectionMask;
    public float detectionRadius;
    public float defaultDetectionRadius = 10f;
    public float chaseDetectionRadius = 15f;
    public float playerLocationHintDelay = 5f;
    public float AOETimer;
    public bool PlayerLocationHintReady => AOETimer >= playerLocationHintDelay;


    [Header("Chase")]
    [SerializeField] private float minChaseSpeed = 3f;
    [SerializeField] private float maxChaseSpeed = 6f;
    [SerializeField] private float maxChaseDistance = 10f;

    [Header("Grab")]
    [SerializeField] private float forceLookDuration = 0.5f;

    [Header("Grab Trigger")]
    [SerializeField] private LayerMask wallLayer;          // copy from the old EnemyStateMachine
    [SerializeField] private float grabWallCheckRadius = 0.5f;
    [SerializeField] private float grabCooldown = 5f;
    private float nextGrabTime;

    [Header("Throw / Kill")]
    [SerializeField] private float minThrowDistance = 5f;
    [SerializeField] private float throwForce = 20f;
    [SerializeField] private float grabDuration = 5.5f;     // time spent turning toward the throw target
    [SerializeField] private float killHoldDuration = 3f;   // old GrabAndWait hold time
    private Vector3 throwLocation;

    //Shared state (read/written by the behvaior graph nodes)
    public Waypoint CurrentWaypoint { get; set; }
    public Queue<Waypoint> Path { get; set; } = new Queue<Waypoint>();
    public Transform PathTarget { get; set; } //The interest point the current Path was calculated for.
    public IReadOnlyList<Waypoint> AllWaypoints => allWaypoints;
    [Header("Runtime (filled by the spawner)")]
    [SerializeField] private List<Waypoint> allWaypoints = new List<Waypoint>();

    void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        if (wayPointGroup != null)
            InitializeNavigation(wayPointGroup.GetComponentsInChildren<Waypoint>(), startingWaypoint, false);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        geist.GetVariable("PlayerInAOE", out playerInAOE);
        geist.GetVariable("NewPlayerInAOE", out newPlayerInAOE);
        geist.GetVariable("PlayerInLOS", out playerInLOS);
        geist.GetVariable("PlayerInGrabRange", out playerInGrabRange);
        geist.GetVariable("PlayerHealthAboveOne", out playerHealthAboveOne);
        geist.GetVariable("PlayerEnteredPoweredArea", out playerEnteredPoweredArea);
        geist.GetVariable("PlayerWithinChaseDistance", out playerWithinChaseDistance);
        geist.GetVariable("PlayerShineFlashlightAtGeist", out playerShineFlashlightAtGeist);
        geist.GetVariable("GeistBlinded", out geistBlinded);
        geist.GetVariable("GeistHeardSoundOfInterest", out geistHeardSoundOfInterest);
        geist.GetVariable("NewElectricalZoneActivated", out newElectricalZoneActivated);
        geist.GetVariable("GeistInPoweredZone", out geistInPoweredZone);
        geist.GetVariable("NewInterestPt", out newInterestPt);
        geist.GetVariable("NewActiveElectricalZone", out newActiveElectricalZone);
        geist.GetVariable("LastKnownPlayerPos", out lastKnownPlayerPos);
        if (newActiveElectricalZone == null)
            Debug.LogWarning("[GeistBrain] No Transform variable named 'NewElectricalZone' on the blackboard", this);

       
    }
    // Update is called once per frame
    void Update()
    {
        if (playerInAOE == null || playerInLOS == null || geistBlinded == null) return;

        bool inAoe = PlayerInAOE();
        if (playerInAOE.Value != inAoe) playerInAOE.Value = inAoe;

        // background hint timer: only runs while the player is in AOE, resets when they leave
        if (inAoe) AOETimer += Time.deltaTime;
        else AOETimer = 0f;

        bool inLOS = PlayerInLOS(out bool blinded);
        if (playerInLOS.Value != inLOS) playerInLOS.Value = inLOS;
        if (geistBlinded.Value != blinded) geistBlinded.Value = blinded;

        if (playerWithinChaseDistance != null)
        {
            bool within = PlayerWithinChaseDistance();
            if (playerWithinChaseDistance.Value != within) playerWithinChaseDistance.Value = within;
        }
    }

    #region Setup (called by SpawnGeistBehavior)

    /// <summary>
    /// spawner hands down player referencs to the brain, so it can be used in the behavior graph and pathfinding
    /// </summary>
    /// <param name="persistant"></param>
    public void Initialize(PersistantManager persistant)
    {
        persistantManager = persistant;
        if (persistant == null) return;
        playerObj = persistant.PlayerObject;
        player = persistant.Player;
    }

    public void InitializeNavigation(IEnumerable<Waypoint> waypoints, Waypoint startingWaypoint, bool snapToStart = true)
    {
        allWaypoints = waypoints != null ? waypoints.Where(w => w != null).ToList() : new List<Waypoint>();

        Path.Clear();
        PathTarget = null;
        CurrentWaypoint = startingWaypoint != null ? startingWaypoint : FindClosestWaypoint(transform.position);

        if (snapToStart && startingWaypoint != null)
            transform.position = startingWaypoint.transform.position;

        RebuildZoneMembership(); //initialize the electrical zones

        //Debug.Log($"[GeistBrain] InitializeNavigation: {allWaypoints.Count} waypoints, start = {(CurrentWaypoint != null ? CurrentWaypoint.name : "NONE")}", this);
    }
    /// <summary>
    /// the graph's top level switch (ComplexGeisAI / Dormant / Cutscene)
    /// </summary>
    /// <param name="mode"></param>
    public void SetMode(GeistEnums mode)
    {
        if(geist == null) return;
        geist.SetVariableValue(modeVariableName, mode);
    }

    public void ReanchorToClosestWaypoint()
    {
        Path.Clear();
        PathTarget = null;
        CurrentWaypoint = FindClosestWaypoint(transform.position);
    }

    #endregion

    #region Cutscene (ported from CutSceneStates)
    // The route graph's cutscene branch is currently playing (null = none)
    public GeistsCutsceneRoute ActiveCutsceneRoute { get; private set; }
    public bool IsCutscenePlaying => ActiveCutsceneRoute != null;
    private System.Action cutsceneCallback;

    /// <summary>
    /// Hands a route to the graph and switches it into Cutscene mode
    /// </summary>
    /// <param name="route"></param>
    /// <param name="onFinished"></param>
    public void BeginCutscene(GeistsCutsceneRoute route, System.Action onFinished = null)
    {
        ActiveCutsceneRoute = route;
        cutsceneCallback = onFinished;
        SetMode(GeistEnums.CutsceneMode);
    }

    /// <summary>
    /// Stops the route without onFinish or the callback
    /// </summary>
    public void CancelCutscene()
    {
        ActiveCutsceneRoute = null;
        cutsceneCallback = null;
    }

    public void FinishCutscene()
    {
        GeistsCutsceneRoute route = ActiveCutsceneRoute;
        System.Action callback = cutsceneCallback;
        ActiveCutsceneRoute = null;
        cutsceneCallback = null;

        switch (route.onFinish)
        {
            case GeistsCutsceneEnd.GoDormant:
                SetMode(GeistEnums.Dormant);
                break;
            case GeistsCutsceneEnd.HandOffToAI:
                ReanchorToClosestWaypoint();
                SetMode(GeistEnums.ComplexGeistAI);
                break;
            case GeistsCutsceneEnd.StayInPlace:
                break;
        }
        callback?.Invoke();
    }

    #endregion

    #region Sensing (some ported from EnemyStateMachine)
    /// <summary>
    /// ported from EnemyStateMachine
    /// </summary>
    /// <returns></returns>
    public bool PlayerInAOE()
    {
        if (player == null) return false;

        Vector3 origin = transform.position;
        Vector3 target = player.transform.position;

        Vector3 toPlayer = target - origin;
        float distToPlayer = toPlayer.magnitude;

        if (distToPlayer > detectionRadius) return false;
        if (distToPlayer < 0.0001f) return true;

        return true;
    }

    public bool PlayerInLOS(out bool blinded)
    {
        blinded = false;
        if (player == null || GeistEye == null || GeistEyeRig == null) return false;

        Vector3 origin = transform.position;
        Vector3 target = player.transform.position;

        Vector3 toPlayer = target - origin;
        float distToPlayer = toPlayer.magnitude;

        // holding the player, or a collision just flagged a grab: they're in contact, so no wall in between
        if (IsGrabbingPlayer || GrabRangeFlagged) return true;

        // Point blank skips the cone and range checks, because the player can sit beside the eye.
        // It does NOT skip the barrier check below.
        bool pointBlank = distToPlayer <= closeSightDistance;
        if (!pointBlank)
        {
            if (distToPlayer > GeistEyeRig.detectionRange) return false;
            if (Vector3.Angle(GeistEye.up, toPlayer) > GeistEyeRig.coneHalfAngle) return false;
        }

        if (distToPlayer < 0.0001f) return true;
        Vector3 dir = toPlayer / distToPlayer;

        foreach (RaycastHit h in Physics.RaycastAll(origin, dir, distToPlayer, ~0, QueryTriggerInteraction.Collide))
        {
            if (h.collider.CompareTag("GeistBlind"))
            {
                blinded = true;
                return false;
            }
        }
        if (Physics.Raycast(origin, dir, out RaycastHit hit, distToPlayer, detectionMask, QueryTriggerInteraction.Ignore))
        {
            bool tf = (hit.collider.GetComponentInParent<ZeroGravity>() != null);
            Debug.Log($"LOS check: {tf}");
            if (tf)
            {
                //update known player Pos so when LOS is lost
                lastKnownPlayerPos.Value = player.transform;
                return true;
            }
        }
        //return an average of the distances
        return true;
    }

    /// <summary>
    /// Returns the waypoint nearest the player if the hint timer has finished, otherwise null.
    /// Consuming the hint restarts the timer.
    /// </summary>
    public Waypoint TryConsumePlayerLocationHint()
    {
        if (!PlayerLocationHintReady || playerObj == null) return null;

        Waypoint hint = FindClosestWaypoint(playerObj.transform.position);
        if (hint != null) AOETimer = 0f;
        return hint;
    }

    #endregion

    #region Chasing (some ported from ComplexEnemyAI)
    public bool IsGrabbingPlayer { get; private set; }
    public bool CanGrabPlayer => player != null && persistantManager != null && !IsGrabbingPlayer && Time.time >= nextGrabTime;
    public float ForceLookDuration => forceLookDuration;
    public Transform PlayerCamera => player != null ? player.cam.transform : null;
    public bool HasPlayer => playerObj != null;

    public bool PlayerWithinChaseDistance()
    {
        if (player == null) return false;
        float r = chaseDetectionRadius;
        return (player.transform.position - transform.position).sqrMagnitude <= r * r;
    }

    public void ChaseStep()
    {
        if (playerObj == null) return;

        Vector3 toPlayer = playerObj.transform.position - transform.position;
        float dist = toPlayer.magnitude;

        // faster the farther away it is, same lerp as EnemyStateMachine.DetermineGeistSpeedChange
        float chaseSpeed = Mathf.Lerp(minChaseSpeed, maxChaseSpeed, Mathf.Clamp01(dist / maxChaseDistance));
        transform.position = Vector3.MoveTowards(transform.position, playerObj.transform.position, chaseSpeed * Time.deltaTime);

        if (toPlayer.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, rotationSpeed * Time.deltaTime);
        }

        //recheck lign of sight to see if is lost
        bool l = PlayerInLOS(out bool geistBlinded);
        playerInLOS.Value = l;

        UpdateCurrentWaypointToClosest();
    }

    // Ported from ComplexEnemyAI: keeps CurrentWaypoint near the Geist while it flies free of the waypoint graph
    public void UpdateCurrentWaypointToClosest()
    {
        if (CurrentWaypoint == null)
        {
            CurrentWaypoint = FindClosestWaypoint(transform.position);
            if (CurrentWaypoint == null) return;
        }

        Waypoint closest = CurrentWaypoint;
        float minSqr = (transform.position - CurrentWaypoint.transform.position).sqrMagnitude;

        foreach (Waypoint neighbor in CurrentWaypoint.neighbors)
        {
            if (neighbor == null) continue;
            float sqr = (transform.position - neighbor.transform.position).sqrMagnitude;
            if (sqr < minSqr)
            {
                closest = neighbor;
                minSqr = sqr;
            }
        }
        CurrentWaypoint = closest;
    }

    // Ported from EnemyStateMachine.LockPlayerInputs
    public void LockPlayerInputs()
    {
        player.transform.SetParent(grabPlayerPos.transform, true);

        persistantManager.PlayerUIManager.HideInteractables();
        persistantManager.PlayerUIManager.HideGrabber();
        player.ReleaseBar();

        player.CanMove = false;
        player.CanGrab = false;
        player.CanPushOff = false;
        player.StopRollingQuickly();

        // kill all momentum and freeze physics
        player.RB.linearVelocity = Vector3.zero;
        player.RB.angularVelocity = Vector3.zero;
        player.RB.isKinematic = true;

        player.IsBeingGrabbed = true;
        IsGrabbingPlayer = true;
    }

    // Ported from EnemyStateMachine.UnlockPlayerInputs. The throw and kill actions will call this.
    public void UnlockPlayerInputs()
    {
        player.transform.SetParent(persistantManager.PlayerParent.transform, true);

        player.CanMove = true;
        player.CanGrab = true;
        player.CanPushOff = true;
        player.IsBeingGrabbed = false;
        player.RB.isKinematic = false;
        IsGrabbingPlayer = false;
        nextGrabTime = Time.time + grabCooldown;
        ClearPlayerInGrabRange();
    }
    #endregion

    #region Grab (some ported from ComplexEnemyAI)

    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log($"[GeistBrain] collision with {collision.gameObject.name} (tag {collision.gameObject.tag})", this); // remove once it works
        HandlePlayerContact(collision.gameObject);
    }

    // Enter fires only on first contact. Stay keeps trying while touching, so a grab that was
    // blocked (cooldown, wall, still throwing) isn't lost.
    private void OnCollisionStay(Collision collision)
    {
        HandlePlayerContact(collision.gameObject);
    }

    private void HandlePlayerContact(GameObject other)
    {
        // the collider can be on a child of the player, so check the hierarchy as well as the tag
        if (!other.CompareTag("Player") && other.GetComponentInParent<ZeroGravity>() == null) return;

        if (IsGrabbingPlayer) return;
        if (Time.time < nextGrabTime) return;

        // phasing through a wall: back out of the grab
        if (IsInsideWall())
        {
            Debug.Log("Geist Phasing while trying to Grab");
            BounceAwayFromGeist();
            return;
        }

        if (playerInGrabRange != null) playerInGrabRange.Value = true;
    }

    public bool GrabRangeFlagged => playerInGrabRange != null && playerInGrabRange.Value;

    public bool IsInsideWall()
    {
        return Physics.CheckSphere(transform.position, grabWallCheckRadius, wallLayer);
    }

    public void ClearPlayerInGrabRange()
    {
        if (playerInGrabRange != null) playerInGrabRange.Value = false;
    }

    public void BounceAwayFromGeist()
    {
        if (player == null) return;

        Vector3 geistNormal = (player.transform.position - transform.position).normalized;
        Vector3 incoming = player.RB.linearVelocity;
        Vector3 reflect = incoming.sqrMagnitude > 0.001f
            ? Vector3.Reflect(incoming.normalized, geistNormal)
            : geistNormal;

        player.transform.position += geistNormal * 0.3f; // small offset to prevent overlap

        player.RB.linearVelocity = Vector3.zero;
        player.RB.angularVelocity = Vector3.zero;
        player.RB.linearVelocity = reflect * (minChaseSpeed * 1.3f);
    }

    #endregion

    #region Throw & Kill (some ported from ComplexEnemyAI)
    public Vector3 ThrowLocation => throwLocation;
    public float GrabDuration => grabDuration;
    public float KillHoldDuration => killHoldDuration;
    public bool HasPlayerRefs => player != null && persistantManager != null;

    // Ported from ComplexEnemyAI.ForceLookAtPlayer
    public void FacePlayerStep()
    {
        if (player == null) return;
        Vector3 toPlayer = player.transform.position - transform.position;
        if (toPlayer.sqrMagnitude < 0.01f) return;

        Quaternion look = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, rotationSpeed * Time.deltaTime);
    }

    // Ported from ComplexEnemyAI.DetermineThrowTarget
    public bool DetermineThrowTarget()
    {
        Vector3 origin = player.transform.position;
        Vector3 primaryDir = (player.transform.position - transform.position).normalized;

        // 1. straight away from the Geist
        if (IsSpaceClear(origin, primaryDir, minThrowDistance, out throwLocation)) return true;

        // 2. five random offsets around that direction
        for (int i = 0; i < 5; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.4f, 0.4f), Random.Range(-0.4f, 0.4f));
            if (IsSpaceClear(origin, (primaryDir + offset).normalized, minThrowDistance, out throwLocation)) return true;
        }

        // 3. a waypoint that's far enough but close enough
        return FindFallbackWaypoint();
    }

    private bool IsSpaceClear(Vector3 origin, Vector3 direction, float minDistance, out Vector3 hitPoint)
    {
        // sphere cast so the player fits through the gap, not just a thin line
        if (Physics.SphereCast(origin, 0.5f, direction, out RaycastHit hit, minDistance, wallLayer, QueryTriggerInteraction.Ignore))
        {
            hitPoint = hit.point;
            return false;
        }
        hitPoint = origin + direction * minDistance;
        return true;
    }

    private bool FindFallbackWaypoint()
    {
        var valid = allWaypoints.Where(wp =>
        {
            float d = Vector3.Distance(player.transform.position, wp.transform.position);
            return d >= 5f && d <= 15f;
        }).ToList();

        if (valid.Count > 0)
        {
            throwLocation = valid[Random.Range(0, valid.Count)].transform.position;
            return true;
        }

        // last resort: a shove from where the Geist is
        throwLocation = transform.position;
        return false;
    }

    // Ported from ComplexEnemyAI.ExecuteKinematicThrow (+ the release that preceded it)
    public void ExecuteKinematicThrow()
    {
        if (player == null) return;

        Vector3 dir = (throwLocation - player.transform.position).normalized;
        UnlockPlayerInputs();            // re-parents the player and restores physics and controls
        player.GetThrown(dir, throwForce);
    }

    // End of the old KillSequence. Doesn't restore CanMove etc.; IsDead sets the rest of the flags.
    public void KillPlayer()
    {
        if (player == null) return;

        player.IsBeingGrabbed = false;
        player.RB.isKinematic = false;

        if (!player.IsDead)
        {
            player.transform.SetParent(persistantManager.PlayerParent.transform, true);
            player.IsDead = true;
        }
        IsGrabbingPlayer = false;
    }

    #endregion

    #region Electrical zones (waypoint groups that can be switched in/out of pathfinding)

    private class Zone
    {
        public UnityEngine.Object key;     // the ElectricalZoneManager
        public List<Collider> bounds;      // its box colliders
        public bool powered = true;
    }

    private readonly List<Zone> zones = new List<Zone>();
    private readonly Dictionary<Waypoint, List<Zone>> waypointZones = new Dictionary<Waypoint, List<Zone>>();

    public bool IsPositionInPoweredZone(Vector3 position)
    {
        foreach (Zone zone in zones)
        {
            if (!zone.powered) continue;
            if (zone.bounds.Any(c => ContainsPoint(c, position))) return true;
        }
        return false;
    }

    /// <summary>
    /// Registers a zone and assigns every waypoint inside its colliders to it.
    /// Waypoints that aren't inside any zone are always available.
    /// </summary>
    public void RegisterZone(UnityEngine.Object key, IEnumerable<Collider> bounds, bool powered)
    {
        Zone zone = zones.Find(z => z.key == key);
        if (zone == null)
        {
            zone = new Zone { key = key };
            zones.Add(zone);
        }
        zone.bounds = bounds.Where(c => c != null).ToList();
        zone.powered = powered;
        RebuildZoneMembership();
    }

    /// <summary>Zone turned on (waypoints rejoin A*) or off (waypoints leave A*).</summary>
    public void SetZonePowered(UnityEngine.Object key, bool powered)
    {
        Zone zone = zones.Find(z => z.key == key);
        if (zone == null || zone.powered == powered) return;

        zone.powered = powered;
        //Debug.Log($"[GeistBrain] Zone {key.name} powered = {powered}", this);

        if (powered)
        {
            Transform zoneTransform = (key as Component)?.transform;
            if (zoneTransform != null)
            {
                SaveZoneHint(zoneTransform);
                if (newActiveElectricalZone != null)
                    newActiveElectricalZone.Value = zoneTransform; // set before the flag so the guard sees it
            }

            if (newElectricalZoneActivated != null)
                newElectricalZoneActivated.Value = true;
        }

        // If the route we're walking crosses powered zone. ditch it.
        if (powered && PathIsBlocked())
            Path.Clear();
    }

    // Where the most recent zone came on. The Geist can use this as a hint.
    public Transform ZoneHint { get; private set; }
    public Vector3 ZoneHintPosition { get; private set; }

    public void SaveZoneHint(Transform zone)
    {
        ZoneHint = zone;

        // centre of the zone's colliders; the manager object's own pivot can sit off to the side
        Collider[] cols = zone.GetComponents<Collider>();
        if (cols.Length > 0)
        {
            Bounds b = cols[0].bounds;
            for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
            ZoneHintPosition = b.center;
        }
        else
        {
            ZoneHintPosition = zone.position;
        }

        Debug.Log($"[GeistBrain] Zone hint saved: {zone.name} at {ZoneHintPosition}", this);
    }

    public bool IsWaypointActive(Waypoint wp)
    {
        if (!waypointZones.TryGetValue(wp, out List<Zone> inside)) return true;
        foreach (Zone z in inside)
            if (z.powered) return false;
        return true;
    }

    private void RebuildZoneMembership()
    {
        waypointZones.Clear();
        foreach (Waypoint wp in allWaypoints)
        {
            if (wp == null) continue;
            foreach (Zone zone in zones)
            {
                if (!zone.bounds.Any(c => ContainsPoint(c, wp.transform.position))) continue;
                if (!waypointZones.TryGetValue(wp, out List<Zone> list))
                    waypointZones[wp] = list = new List<Zone>();
                list.Add(zone);
            }
        }
        //Debug.Log($"[GeistBrain] {waypointZones.Count}/{allWaypoints.Count} waypoints are inside electrical zones", this);
    }

    public static bool ContainsPoint(Collider c, Vector3 point)
    {
        if (c == null) return false;

        // box test in local space: handles rotation and scale, and works while collider is disabled
        if (c is BoxCollider box)
        {
            Vector3 local = box.transform.InverseTransformPoint(point) - box.center;
            Vector3 half = box.size * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }
        return c.bounds.Contains(point);
    }

    private bool SegmentBlocked(Vector3 a, Vector3 b)
    {
        foreach (Zone zone in zones)
        {
            if (!zone.powered) continue;
            // an endpoint already inside this zone is handled elsewhere (and lets the Geist leave it)
            if (zone.bounds.Any(c => ContainsPoint(c, a) || ContainsPoint(c, b))) continue;

            foreach (Collider c in zone.bounds)
                if (c is BoxCollider box && SegmentIntersectsBox(box, a, b)) return true;
        }
        return false;
    }

    private static bool SegmentIntersectsBox(BoxCollider box, Vector3 a, Vector3 b)
    {
        Vector3 p0 = box.transform.InverseTransformPoint(a) - box.center;
        Vector3 p1 = box.transform.InverseTransformPoint(b) - box.center;
        Vector3 half = box.size * 0.5f;
        Vector3 d = p1 - p0;
        float tMin = 0f, tMax = 1f;

        for (int i = 0; i < 3; i++)
        {
            if (Mathf.Abs(d[i]) < 1e-6f)
            {
                if (p0[i] < -half[i] || p0[i] > half[i]) return false;
            }
            else
            {
                float t1 = (-half[i] - p0[i]) / d[i];
                float t2 = (half[i] - p0[i]) / d[i];
                if (t1 > t2) { float tmp = t1; t1 = t2; t2 = tmp; }
                tMin = Mathf.Max(tMin, t1);
                tMax = Mathf.Min(tMax, t2);
                if (tMin > tMax) return false;
            }
        }
        return true;
    }

    private bool PathIsBlocked()
    {
        Vector3 prev = transform.position;
        foreach (Waypoint w in Path)
        {
            if (!IsWaypointActive(w) || SegmentBlocked(prev, w.transform.position)) return true;
            prev = w.transform.position;
        }
        return false;
    }

    #endregion

    #region Phasing waypoints (edge case when the Geist needs to bail from electrical zone)
    public void SetPhasingWaypoints(IEnumerable<Waypoint> waypoints)
    {
        phasingWaypoints = waypoints != null ? waypoints.Where(w => w != null).ToList() : new List<Waypoint>();
        //Debug.Log($"[GeistBrain] {phasingWaypoints.Count} phasing waypoints", this);
    }

    public Waypoint GetClosestPhasingWaypoint(Vector3 position)
    {
        Waypoint closest = null;
        float closestDistSqr = float.MaxValue;
        foreach (Waypoint wp in phasingWaypoints)
        {
            float distSqr = (position - wp.transform.position).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = wp;
            }
        }
        return closest;
    }

    /// <summary>
    /// Straight line to one waypoint, no A*. Move follows it like any other path.
    /// </summary>
    /// <param name="target"></param>
    public void SetDirectPath(Waypoint target)
    {
        Path.Clear();
        Path.Enqueue(target);
        PathTarget = target.transform;
    }

    #endregion

    #region Navigation (ported from ComplexEnemyAI)

    /// <summary>
    /// Moves one frame along the path. Returns true once path is finished
    /// </summary>
    /// <returns></returns>
    public bool Step()
    {
        if (Path.Count == 0) return true;
        
        Waypoint target = Path.Peek();
        Vector3 targetPos = target.transform.position;

        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        Vector3 dir = targetPos - transform.position;
        if(dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        if(Vector3.Distance(transform.position, targetPos) < arriveThreshold)
            CurrentWaypoint = Path.Dequeue();

        return Path.Count == 0;
    }

    /// <summary>
    /// Waypoint lookup
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public Waypoint FindClosestWaypoint(Vector3 position)
    {
        Waypoint closest = null;
        float closestDistSqr = float.MaxValue;
        foreach (Waypoint wp in allWaypoints)
        {
            if (wp == null) continue;
            float distSqr = (position - wp.transform.position).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = wp;
            }
        }
        return closest;
    }
    /// <summary>
    /// From RoamArea: any waypoint except the one we are standing on
    /// </summary>
    /// <returns></returns>
    public Waypoint GetRandomWaypoint()
    {
        List<Waypoint> options = allWaypoints.Where(wp => wp != CurrentWaypoint && IsWaypointActive(wp)).ToList();
        return options.Count > 0 ? options[Random.Range(0, options.Count)] : null;
    }


    public Waypoint GetRandomWaypointInAOE()
    {
        float radius = detectionRadius > 0f ? detectionRadius : defaultDetectionRadius;
        float sqrRadius = radius * radius;

        List<Waypoint> options = allWaypoints.Where(wp =>
        wp != CurrentWaypoint &&
        IsWaypointActive(wp) && 
        (wp.transform.position - transform.position).sqrMagnitude <= sqrRadius).ToList();

        //Debug.Log($"[AOE] f{Time.frameCount} picker: options={options.Count}, radius={radius}, current={(CurrentWaypoint != null ? CurrentWaypoint.name : "NULL")}", this);

        return options.Count > 0 ? options[Random.Range(0, options.Count)] : null;
    }
    /// <summary>
    /// A* pathfinding hat finds paths to goal waypoint, The InterestPt
    /// </summary>
    /// <param name="start"></param>
    /// <param name="goal"></param>
    /// <returns></returns>
    public Queue<Waypoint> FindPath(Waypoint start, Waypoint goal)
    {
        var result = new Queue<Waypoint>();
        if(start == null || goal == null)
        {
            Debug.LogWarning($"A* stopped: start {(start == null ? "Null" : "Valid")}, goal {(goal == null ? "Null" : "Valid")}");
            return result;
        }
        if (!IsWaypointActive(goal)) return result; // no route to powered zone

        var open = new List<Waypoint> { start };
        var closed = new HashSet<Waypoint>();
        var cameFrom = new Dictionary<Waypoint, Waypoint>();
        var g = new Dictionary<Waypoint, float> { [start] = 0f };
        var f = new Dictionary<Waypoint, float> { [start] = Heuristic(start, goal) };

        while (open.Count > 0)
        {
            int best = 0;
            for (int i=1; i < open.Count; i++)
            {
                if (f[open[i]] < f[open[best]])
                    best = i;
            }

            Waypoint current = open[best];
            if(current == goal)
                return Reconstruct(cameFrom, current);

            open.RemoveAt(best);
            closed.Add(current);

            foreach (Waypoint neighbor in current.neighbors)
            {
                if (neighbor == null || closed.Contains(neighbor)) continue;
                if(!IsWaypointActive(neighbor)) continue; // powered zone? not part of the graph
                if (SegmentBlocked(current.transform.position, neighbor.transform.position)) continue;

                float tentative = g[current] + Vector3.Distance(current.transform.position, neighbor.transform.position);
                if(!g.TryGetValue(neighbor, out float existing) || tentative < existing)
                {
                    cameFrom[neighbor] = current;
                    g[neighbor] = tentative;
                    f[neighbor] = tentative + Heuristic(neighbor, goal);
                    if (!open.Contains(neighbor))
                        open.Add(neighbor);
                }
            }
        }
        return result; // Return empty if no path found
    }

    private float Heuristic(Waypoint a, Waypoint b)
    {
        return Vector3.Distance(a.transform.position, b.transform.position);
    }

    private Queue<Waypoint> Reconstruct(Dictionary<Waypoint, Waypoint> cameFrom, Waypoint goal)
    {
        var reversed = new List<Waypoint>();
        for (Waypoint at = goal; at != null; at = cameFrom.TryGetValue(at, out var prev) ? prev : null)
            reversed.Add(at);
        reversed.Reverse();

        if(reversed.Count > 0 && Vector3.Distance(transform.position, reversed[0].transform.position) < 0.1f)
            reversed.RemoveAt(0);

        return new Queue<Waypoint>(reversed);
    }

    #endregion

    #region Gizmos
    private void OnDrawGizmos()
    {
        if (showDetectionRadius)
        {
            Gizmos.color = playerInAOE ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            if (player != null)
                Gizmos.DrawLine(transform.position, player.transform.position);
        }
        if (showLOSCone)
        {
            if (GeistEye == null) return;

            DrawWireCone(GeistEye.position, GeistEye.up, coneHalfAngle, geistEyeRangeMax,
                         playerInLOS ? Color.green : Color.yellow);
        }
    }

    private static void DrawWireCone(Vector3 origin, Vector3 forward,
                                    float halfAngleDeg, float range, Color color)
    {
        Gizmos.color = color;
        const int segments = 20;

        Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
        Vector3 perpStart = Vector3.Cross(forward, up).normalized;
        Vector3 prevRim = Vector3.zero;

        float cosA = Mathf.Cos(halfAngleDeg * Mathf.Deg2Rad);
        float sinA = Mathf.Sin(halfAngleDeg * Mathf.Deg2Rad);

        for (int i = 0; i <= segments; i++)
        {
            Vector3 perp = Quaternion.AngleAxis((float)i / segments * 360f, forward) * perpStart;
            Vector3 rimPoint = origin + (forward * cosA + perp * sinA).normalized * range;

            if (i > 0) Gizmos.DrawLine(prevRim, rimPoint);
            if (i % (segments / 4) == 0) Gizmos.DrawLine(origin, rimPoint);

            prevRim = rimPoint;
        }
    }
    #endregion
}
