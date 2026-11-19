using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Keeps the map's doors colored by their real state while the game runs: blue when working, yellow when
/// broken, red when locked. Each door's panel also moves with the real one, so doors open and close on the map. It also tells each camera how near and far the map reaches, so working doors
/// near the front show through walls and ones further back don't. The Map Builder puts this on the baked map's Doors group.
/// </summary>
public class MapDoors : MonoBehaviour
{
    // Looking for the DoorManager is slow, so when there isn't one it's tried again only now and then
    const float SearchInterval = 1f;

    [Tooltip("Materials for each state: lines, invisible depth, fill.")]
    public Material[] working = new Material[0];
    public Material[] broken = new Material[0];
    public Material[] locked = new Material[0];

    [Tooltip("The scene of the level this map was built from, for finding its level root.")]
    public string levelScene = "";

    private DoorManager manager;
    private DoorScript[] doors;
    private MapDoor[] mapDoors;
    private float nextSearch;
    private MapLevel level;
    private float nextLevelSearch;

    // The real panel each map door follows, found with the doors
    private Transform[] panels;

    // The map's corners in its own space, measured once, for working out its depth from each camera
    private Vector3[] mapCorners;

    private static readonly int NearId = Shader.PropertyToID("_HoloMapNear");
    private static readonly int FarId = Shader.PropertyToID("_HoloMapFar");

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += SetDepthRange;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= SetDepthRange;
    }

    private void SetDepthRange(ScriptableRenderContext context, Camera camera)
    {
        // Cameras that can't see the map don't need it
        if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;
        SetDepthRange(camera);
    }

    /// <summary>
    /// Tells the map's shaders how near and far the map reaches from this camera.
    /// </summary>
    public void SetDepthRange(Camera camera)
    {
        Transform map = transform.parent != null ? transform.parent : transform;
        if (mapCorners == null) mapCorners = MeasureCorners(map);

        Matrix4x4 toView = camera.worldToCameraMatrix * map.localToWorldMatrix;
        float near = float.MaxValue, far = float.MinValue;
        foreach (Vector3 corner in mapCorners)
        {
            float depth = -toView.MultiplyPoint3x4(corner).z;
            near = Mathf.Min(near, depth);
            far = Mathf.Max(far, depth);
        }
        Shader.SetGlobalFloat(NearId, Mathf.Max(near, 0f));
        Shader.SetGlobalFloat(FarId, far);
    }

    private static Vector3[] MeasureCorners(Transform map)
    {
        Renderer[] parts = map.GetComponentsInChildren<Renderer>(true);
        if (parts.Length == 0) return new[] { Vector3.zero };

        Matrix4x4 toMap = map.worldToLocalMatrix;
        var bounds = new Bounds(toMap.MultiplyPoint3x4(parts[0].bounds.center), Vector3.zero);
        foreach (Renderer part in parts)
        {
            Bounds world = part.bounds;
            for (int i = 0; i < 8; i++) bounds.Encapsulate(toMap.MultiplyPoint3x4(Corner(world, i)));
        }

        var corners = new Vector3[8];
        for (int i = 0; i < 8; i++) corners[i] = Corner(bounds, i);
        return corners;
    }

    private static Vector3 Corner(Bounds bounds, int i)
    {
        return bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
    }

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

        if (level == null)
        {
            if (Time.unscaledTime < nextLevelSearch) return;
            nextLevelSearch = Time.unscaledTime + SearchInterval;
            level = MapPlayerMarker.FindLevel(levelScene);
            if (level == null) return;
        }
        FollowPanels(level.transform);
    }

    /// <summary>
    /// Moves each map door's panel to where the real one is. The map is baked relative to the level root,
    /// so the panel's spot relative to the level root is its spot inside the map.
    /// </summary>
    public void FollowPanels(Transform levelRoot)
    {
        if (panels == null) return;
        Quaternion toLevel = Quaternion.Inverse(levelRoot.rotation);
        for (int i = 0; i < mapDoors.Length; i++)
        {
            Transform panel = panels[i];
            if (panel == null) continue;
            mapDoors[i].transform.SetLocalPositionAndRotation(levelRoot.InverseTransformPoint(panel.position), toLevel * panel.rotation);
        }
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
            panels = new Transform[mapDoors.Length];
            for (int i = 0; i < mapDoors.Length; i++)
            {
                MapDoor mapDoor = mapDoors[i];
                bool listed = mapDoor.index >= 0 && mapDoor.index < doors.Length && doors[mapDoor.index] != null;
                if (listed && !string.IsNullOrEmpty(mapDoor.panelPath)) panels[i] = doors[mapDoor.index].transform.Find(mapDoor.panelPath);
            }
        }

        foreach (MapDoor mapDoor in mapDoors)
        {
            if (mapDoor.index < 0 || mapDoor.index >= doors.Length || doors[mapDoor.index] == null) continue;

            MapDoorState state = GetState(doors[mapDoor.index].DoorState);
            if (state != mapDoor.state) mapDoor.Show(state, GetMaterials(state));
        }
    }
}
