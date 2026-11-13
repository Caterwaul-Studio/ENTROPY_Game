using UnityEngine;

/// <summary>
/// Keeps the map's doors colored by their real state while the game runs: blue when working, yellow when
/// broken, red when locked. The Map Builder puts this on the baked map's Doors group.
/// </summary>
public class MapDoors : MonoBehaviour
{
    // Looking for the DoorManager is slow, so when there isn't one it's tried again only now and then
    const float SearchInterval = 1f;

    [Tooltip("Materials for each state: lines, invisible depth, fill.")]
    public Material[] working = new Material[0];
    public Material[] broken = new Material[0];
    public Material[] locked = new Material[0];

    private DoorManager manager;
    private DoorScript[] doors;
    private MapDoor[] mapDoors;
    private float nextSearch;

    /// <summary>
    /// The DoorManager's doors in its own order, found the same way DoorManager.Start finds them,
    /// which is also the order its save data uses.
    /// </summary>
    public static DoorScript[] FindDoors(DoorManager doorManager)
    {
        Transform group = doorManager.transform.Find("DoorGroup");
        return group != null ? group.GetComponentsInChildren<DoorScript>() : new DoorScript[0];
    }

    public static MapDoorState GetState(DoorScript.States doorState)
    {
        switch (doorState)
        {
            case DoorScript.States.Locked:
                return MapDoorState.Locked;
            // JoltOpen is a broken door forced open, and the game lights it as broken too
            case DoorScript.States.Broken:
            case DoorScript.States.BrokenShort:
            case DoorScript.States.JoltOpen:
                return MapDoorState.Broken;
            default:
                return MapDoorState.Working;
        }
    }

    public Material[] GetMaterials(MapDoorState state)
    {
        return state == MapDoorState.Locked ? locked : state == MapDoorState.Broken ? broken : working;
    }

    private void Update()
    {
        if (manager == null)
        {
            if (Time.unscaledTime < nextSearch) return;
            nextSearch = Time.unscaledTime + SearchInterval;
            manager = FindFirstObjectByType<DoorManager>();
            if (manager == null) return;
            doors = null;
        }
        Refresh(manager);
    }

    /// <summary>
    /// Colors each map door by its real door's state.
    /// </summary>
    public void Refresh(DoorManager doorManager)
    {
        if (doors == null || manager != doorManager)
        {
            manager = doorManager;
            doors = FindDoors(doorManager);
            mapDoors = GetComponentsInChildren<MapDoor>(true);
        }

        foreach (MapDoor mapDoor in mapDoors)
        {
            if (mapDoor.index < 0 || mapDoor.index >= doors.Length || doors[mapDoor.index] == null) continue;

            MapDoorState state = GetState(doors[mapDoor.index].DoorState);
            if (state != mapDoor.state) mapDoor.Show(state, GetMaterials(state));
        }
    }
}
