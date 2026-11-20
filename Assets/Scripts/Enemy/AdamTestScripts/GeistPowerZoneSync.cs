using System.Collections.Generic;
using UnityEngine;

public class GeistPowerZoneSync : MonoBehaviour
{
    [SerializeField] private float pollInterval = 0.25f;

    private GeistDirectorBrain brain;
    private readonly List<ElectricalZoneManager> zones = new List<ElectricalZoneManager>();
    private readonly Dictionary<ElectricalZoneManager, bool> lastState = new Dictionary<ElectricalZoneManager, bool>();
    private float nextPoll;

    void Awake()
    {
        brain = GetComponent<GeistDirectorBrain>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        zones.AddRange(FindObjectsByType<ElectricalZoneManager>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        foreach (ElectricalZoneManager zone in zones)
        {
            bool powered = ReadPowered(zone);
            lastState[zone] = powered;
            brain.RegisterZone(zone, zone.GetComponents<Collider>(), powered);
        }
        //Debug.Log($"[GeistZoneSync] Registered {zones.Count} electrical zones", this);
    }

    void OnEnable()
    {
        nextPoll = 0f; // resync right away after being re-enabled
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time < nextPoll) return;
        nextPoll = Time.time + pollInterval;

        foreach (ElectricalZoneManager zone in zones)
        {
            if (zone == null) continue;
            bool powered = ReadPowered(zone);
            if (lastState.TryGetValue(zone, out bool previous) && previous == powered) continue;

            lastState[zone] = powered;
            brain.SetZonePowered(zone, powered);
        }
    }

    private bool ReadPowered(ElectricalZoneManager zone)
    {
        return zone.CurrentPowerState == PowerObject.PowerStates.Powered;
    }
}
