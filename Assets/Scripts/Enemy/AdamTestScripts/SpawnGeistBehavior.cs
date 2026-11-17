using NUnit.Framework;
using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;

public class SpawnGeistBehavior : MonoBehaviour
{
    [SerializeField] private PersistantManager persistant;
    [SerializeField] private SpawnGeistTrigger trigger;

    //needs BehaviorGraphAgent and GeistDirectorBrain
    [SerializeField] private GameObject geist3PreFab;
    //name of Geistenums variable on blackboard
    [SerializeField] private string geistModeVariable = "New GeistEnums";

    [SerializeField] private IEnumerator geistSpawnWait;
    [SerializeField] private List<Waypoint> SpawnLocations;

    [SerializeField] private bool useSpawnTimer;
    [SerializeField] private float waitTimer;
    [SerializeField] private Transform LevelWaypointGroup;
    [SerializeField] private Waypoint startingWaypoint;

    public GameObject SpawnedGeist { get; private set; }
    public BehaviorGraphAgent GeistAgent { get; private set; }
    public GeistDirectorBrain GeistBrain { get; private set; }
    public CutSceneStates Cutscene { get; private set; } //update this later as a state of the behavior graph

    public bool GeistIsWoken { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (persistant == null)
        {
            persistant = FindFirstObjectByType<PersistantManager>();
            useSpawnTimer = persistant.GeistAlrSpawned;
        }

        Debug.Log("GeistSpawner start");
        //always exists from the start of the level, but dormant
        SpawnDormantGeist();

        if (useSpawnTimer)
        {
            geistSpawnWait = StartCountdown(waitTimer);
            StartCoroutine(geistSpawnWait);
        }
        else
        {
            if (trigger == null)
                trigger = FindFirstObjectByType<SpawnGeistTrigger>();

            if (trigger != null && !trigger.triggerEntered)
            {
                trigger.OnGeistTriggerEnter += HandleGeistTriggerEnter;
            }
        }
    }

    private void OnDestroy()
    {
        if(trigger != null) 
            trigger.OnGeistTriggerEnter -= HandleGeistTriggerEnter;
    }
 
    private Waypoint GetRandomSpawnWayPoint()
    {
        if(SpawnLocations == null || SpawnLocations.Count == 0)
        {
            Debug.LogWarning("SpawnGeist: no SpawnLocations assigned, using starting waypoint");
            return startingWaypoint;
        }

        return SpawnLocations[Random.Range(0, SpawnLocations.Count)];
    }

    /// <summary>
    /// Every waypoint the Geist is allowed to roam. Uses the level group: falls back to the spawn list if no group assigned. 
    /// </summary>
    /// <returns></returns>
    private IEnumerable<Waypoint> GetLevelWaypoints()
    {
        if(LevelWaypointGroup != null)
            return LevelWaypointGroup.GetComponentsInChildren<Waypoint>();

        Debug.LogWarning("SpawnGeist: no LevelWaypointGroup assigned, using SpawnLocations as the waypoint list");
        return SpawnLocations;
    }

    private void SetGeistMode(GeistEnums newMode)
    {
        if (GeistAgent == null) return;
        GeistAgent.SetVariableValue(geistModeVariable, newMode);
    }

    private void SpawnDormantGeist()
    {
        startingWaypoint = GetRandomSpawnWayPoint();
        Vector3 pos = startingWaypoint != null ? startingWaypoint.transform.position : transform.position;

        SpawnedGeist = Instantiate(geist3PreFab, pos, Quaternion.identity, this.transform);
        if (SpawnedGeist == null) return;

        GeistAgent = SpawnedGeist.GetComponent<BehaviorGraphAgent>();
        GeistBrain = SpawnedGeist.GetComponent<GeistDirectorBrain>();
        Cutscene = SpawnedGeist.GetComponent<CutSceneStates>();
        if (Cutscene == null) Cutscene = SpawnedGeist.AddComponent<CutSceneStates>();

        if(GeistAgent == null || GeistBrain == null)
        {
            Debug.LogError("SpawnGeist: GeistAgent or GeistBrain is missing on the spawned Geist prefab");
            return;
        }

        //hand the spawned geist the references it can't store itself
        if(GeistBrain != null)
        {
            GeistBrain.Initialize(persistant);
            GeistBrain.InitializeNavigation(GetLevelWaypoints(), startingWaypoint);
        }

        SetGeistMode(GeistEnums.Dormant);
        SpawnedGeist.SetActive(false);
    }

    public void WakeGeist()
    {
        if (SpawnedGeist == null || GeistIsWoken) return;
        GeistIsWoken = true;

        if(Cutscene != null && Cutscene.IsPlaying) Cutscene.Cancel();

        startingWaypoint = GetRandomSpawnWayPoint();
        SpawnedGeist.SetActive(true);

        if(GeistBrain != null) GeistBrain.InitializeNavigation(GetLevelWaypoints(), startingWaypoint);

        SetGeistMode(GeistEnums.ComplexGeistAI);
        Debug.Log("Geist has been woken");
    }

    #region spawn timer and trigger
    private IEnumerator StartCountdown(float countdownTime)
    {
        float currentTime = countdownTime;

        while (currentTime > 0)
        {
            currentTime -= Time.deltaTime;

            yield return null; 
        }

        WakeGeist();

        Debug.Log("Geist Has been Spawned");
    }

    private void HandleGeistTriggerEnter(bool entered)
    {
        if (!entered) return;

        WakeGeist();

        // one-shot: stop listening once spawned
        trigger.OnGeistTriggerEnter -= HandleGeistTriggerEnter;
    }

    #endregion
}
