using UnityEngine;
using UnityEngine.Rendering;

public class MapDoors : MonoBehaviour
{
    const float SearchInterval = 1f;

    public Material[] working = new Material[0];
    public Material[] broken = new Material[0];
    public Material[] locked = new Material[0];
    [Range(0f, 1f)] public float depthDimming = 0.9f;
    public bool debugLogging;
    public string levelScene = "";

    private DoorManager manager;
    private DoorScript[] doors;
    private MapDoor[] mapDoors;
    private float nextSearch;
    private MapLevel level;
    private float nextLevelSearch;
    private Transform[] panels;

    // Per-piece corners fit far tighter than one box around the map
    private Vector3[] mapCorners;
    private MapFaceCamera labels;
    private static readonly int NearId = Shader.PropertyToID("_HoloMapNear");
    private static readonly int FarId = Shader.PropertyToID("_HoloMapFar");
    private static readonly int DimId = Shader.PropertyToID("_HoloMapDepthDim");
    private float nextLog;
    private int logFrame = -1;

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += SetDepthRange;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= SetDepthRange;

        // Global shader values outlive Play mode, so reset them
        Shader.SetGlobalFloat(NearId, 0f);
        Shader.SetGlobalFloat(FarId, 0f);
        Shader.SetGlobalFloat(DimId, 0f);
    }

    private void SetDepthRange(ScriptableRenderContext context, Camera camera)
    {
        if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;
        SetDepthRange(camera);
    }

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
        Shader.SetGlobalFloat(DimId, depthDimming);

        if (labels == null) labels = map.GetComponentInChildren<MapFaceCamera>(true);
        if (labels != null) labels.ApplyDepth(camera, Mathf.Max(near, 0f), far, depthDimming);

        if (debugLogging)
        {
            if (Time.frameCount != logFrame && Time.unscaledTime >= nextLog)
            {
                logFrame = Time.frameCount;
                nextLog = Time.unscaledTime + 1f;
            }
            if (Time.frameCount == logFrame)
            {
                Debug.Log($"[HoloMap] frame {logFrame}, camera '{camera.name}' (depth {camera.depth}, target {(camera.targetTexture != null ? camera.targetTexture.name : "screen")}): "
                    + $"map near {near:F2} m, far {far:F2} m, dimming {depthDimming:F2}, measured from {mapCorners.Length / 8} pieces", this);
            }
        }
    }

    private static Vector3[] MeasureCorners(Transform map)
    {
        Matrix4x4 toMap = map.worldToLocalMatrix;
        var corners = new System.Collections.Generic.List<Vector3>();
        foreach (Renderer part in map.GetComponentsInChildren<Renderer>())
        {
            if (!part.enabled || part.GetComponent<MapPlayerMarker>() != null) continue;

            Matrix4x4 pieceToMap = toMap * part.transform.localToWorldMatrix;
            Bounds own = part.localBounds;
            for (int i = 0; i < 8; i++) corners.Add(pieceToMap.MultiplyPoint3x4(Corner(own, i)));
        }
        return corners.Count > 0 ? corners.ToArray() : new[] { Vector3.zero };
    }

    private static Vector3 Corner(Bounds bounds, int i)
    {
        return bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
    }

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
            // JoltOpen shows as broken, like the game
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
