using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnGeist : MonoBehaviour
{
    [SerializeField] private PersistantManager persistant;
    [SerializeField] private SpawnGeistTrigger trigger;

    [SerializeField] private GameObject geist2PreFab;

    [SerializeField] private IEnumerator geistSpawnWait;
    [SerializeField] private List<Waypoint> SpawnLocations;

    [SerializeField] private bool useSpawnTimer;
    [SerializeField] private float waitTimer;
    [SerializeField] private Transform LevelWaypointGroup;
    [SerializeField] private Waypoint startingWaypoint;

    public GameObject SpawnedGeist { get; private set; }
    public ComplexEnemyAI GeistAI { get; private set; }
    public CutSceneStates Cutscene { get; private set; }

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

    private void SpawnDormantGeist()
    {
        startingWaypoint = GetRandomSpawnWayPoint();
        Vector3 pos = startingWaypoint != null ? startingWaypoint.transform.position : transform.position;

        SpawnedGeist = Instantiate(geist2PreFab, pos, Quaternion.identity, this.transform);
        if (SpawnedGeist == null) return;

        GeistAI = SpawnedGeist.GetComponent<ComplexEnemyAI>();
        Cutscene = SpawnedGeist.GetComponent<CutSceneStates>();
        if (Cutscene == null) Cutscene = SpawnedGeist.AddComponent<CutSceneStates>();


        GeistAI.waypointGroup = LevelWaypointGroup;
        GeistAI.startingWaypoint = startingWaypoint;

        GeistAI.SetControlMode(ComplexEnemyAI.ControlMode.Dormant);
        SpawnedGeist.SetActive(false);
    }

    public void WakeGeist()
    {
        if (SpawnedGeist == null || GeistIsWoken) return;
        GeistIsWoken = true;

        if(Cutscene != null && Cutscene.IsPlaying) Cutscene.Cancel();

        startingWaypoint = GetRandomSpawnWayPoint();
        GeistAI.startingWaypoint = startingWaypoint;
        SpawnedGeist.SetActive(true);

        //if it alr ran Start (used in cutscene), reset it
        if(GeistAI.HasStarted) GeistAI.ResetToStart();

        GeistAI.SetControlMode(ComplexEnemyAI.ControlMode.AI);
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

    //depreciated by new "wake dormant geist" that is loaded at the beginning of level to prevent lag spikes
    //private void HandleSpawnGeist()
    //{
    //    SpawnedGeist = Instantiate(geist2PreFab, (startingWaypoint = GetRandomSpawnWayPoint()).transform.position, Quaternion.identity, this.transform);

    //    if (SpawnedGeist == null) return;

    //    SpawnedGeist.GetComponent<ComplexEnemyAI>().waypointGroup = LevelWaypointGroup;
    //    SpawnedGeist.GetComponent<ComplexEnemyAI>().startingWaypoint = startingWaypoint;
    //    //SpawnedGiest.GetComponent<ComplexEnemyAI>().playerController = zeroGravity;
    //}
    #endregion
}
