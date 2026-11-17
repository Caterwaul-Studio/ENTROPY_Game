using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Behavior;
using UnityEngine;

public class GeistDirectorBrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PersistantManager persistantManager;
    [SerializeField] private GameObject playerObj;
    [SerializeField] private ZeroGravity player;

    [Header("GeistObjs")]
    [SerializeField] private BehaviorGraphAgent geist;
    [SerializeField] private GameObject grabPlayerPos;
    [SerializeField] private string modeVariableName = "New GeistEnums";
    BlackboardVariable<bool> playerInAOE,
        playerInLOS,
        playerInGrabRange,
        playerHealthAboveOne,
        playerEnteredPoweredArea,
        playerWithinChaseDistance,
        playerShineFlashlightAtGeist,
        geistBlinded,
        geistHeardSoundOfInterest,
        newElectricalZoneActivated,
        newInterestPt;

    [Header("Waypoints")]
    [SerializeField] private Transform wayPointGroup;
    [SerializeField] private Waypoint startingWaypoint;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 2.5f;
    [SerializeField] private float arriveThreshold = 0.5f; //Distance at which a waypoint counts as reached (original used 0.5)

    //Shared state (read/written by the behvaior graph nodes)
    public Waypoint CurrentWaypoint { get; set; }
    public Queue<Waypoint> Path { get; set; } = new Queue<Waypoint>();
    public Transform PathTarget { get; set; } //The interest point the current Path was calculated for.
    public IReadOnlyList<Waypoint> AllWaypoints => allWaypoints;
    private List<Waypoint> allWaypoints = new List<Waypoint>();

    void Awake()
    {
        if(wayPointGroup != null)
            InitializeNavigation(wayPointGroup.GetComponentsInChildren<Waypoint>(), startingWaypoint, false);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        geist.GetVariable("PlayerInAOE", out playerInAOE);
        geist.GetVariable("PlayerInLOS", out playerInLOS);
        geist.GetVariable("PlayerInGrabRange", out playerInGrabRange);
        geist.GetVariable("PlayerHealthAboveOne", out playerHealthAboveOne);
        geist.GetVariable("PlayerEnteredPoweredArea", out playerEnteredPoweredArea);
        geist.GetVariable("PlayerWithinChaseDistance", out playerWithinChaseDistance);
        geist.GetVariable("PlayerShineFlashlightAtGeist", out playerShineFlashlightAtGeist);
        geist.GetVariable("GeistBlinded", out geistBlinded);
        geist.GetVariable("GeistHeardSoundOfInterest", out geistHeardSoundOfInterest);
        geist.GetVariable("NewElectricalZoneActivated", out newElectricalZoneActivated);
        geist.GetVariable("NewInterestPt", out newInterestPt);
    }
    // Update is called once per frame
    void Update()
    {

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
        List<Waypoint> options = allWaypoints.Where(wp => wp != CurrentWaypoint).ToList();
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
}
