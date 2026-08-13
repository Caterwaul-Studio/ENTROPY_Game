using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnGeist : MonoBehaviour
{
    [SerializeField] private SpawnGeistTrigger trigger;

    [SerializeField] private GameObject geist2PreFab;

    private GameObject SpawnedGeist;

    [SerializeField] private IEnumerator geistSpawnWait;
    [SerializeField] private List<Waypoint> SpawnLocations;

    [SerializeField] private bool useSpawnTimer;
    [SerializeField] private float waitTimer;
    [SerializeField] private Transform LevelWaypointGroup;
    [SerializeField] private Waypoint startingWaypoint;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(trigger == null)
            trigger = FindFirstObjectByType<SpawnGeistTrigger>();

        if (useSpawnTimer)
        {
            geistSpawnWait = StartCountdown(waitTimer);
            StartCoroutine(geistSpawnWait);
        }

        if (!trigger.triggerEntered)
        {
            trigger.OnGeistTriggerEnter += HandleGeistTriggerEnter;
        }
    }

    private void OnDestroy()
    {
        if(trigger != null) 
            trigger.OnGeistTriggerEnter -= HandleGeistTriggerEnter;
    }

    private void HandleGeistTriggerEnter(bool entered)
    {
        if (!entered) return;

        HandleSpawnGeist();

        // one-shot: stop listening once spawned
        trigger.OnGeistTriggerEnter -= HandleGeistTriggerEnter;
    }

    private void HandleSpawnGeist()
    {
        SpawnedGeist = Instantiate(geist2PreFab, (startingWaypoint = GetRandomSpawnWayPoint()).transform.position, Quaternion.identity, this.transform);

        if (SpawnedGeist == null) return; 

        SpawnedGeist.GetComponent<ComplexEnemyAI>().waypointGroup = LevelWaypointGroup;
        SpawnedGeist.GetComponent<ComplexEnemyAI>().startingWaypoint = startingWaypoint;
        //SpawnedGiest.GetComponent<ComplexEnemyAI>().playerController = zeroGravity;
    }

    private Waypoint GetRandomSpawnWayPoint()
    {
        return SpawnLocations[Random.Range(0, SpawnLocations.Count)];
    }

    private IEnumerator StartCountdown(float countdownTime)
    {
        float currentTime = countdownTime;

        while (currentTime > 0)
        {
            currentTime -= Time.deltaTime;

            yield return null; 
        }

        HandleSpawnGeist();

        Debug.Log("Geist Has been Spawned");
    }
}
