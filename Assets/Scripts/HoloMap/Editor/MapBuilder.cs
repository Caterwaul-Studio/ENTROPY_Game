using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MapBuilder
{
    const string MapFolder = "Assets/Prefabs/Map";
    const string ProxyFolder = MapFolder + "/Proxies";
    const string GeneratedFolder = MapFolder + "/Generated";
    const string LibraryPath = MapFolder + "/MapProxyLibrary.asset";
    const string WireFolder = GeneratedFolder + "/Wire";
    const string BoxMeshPath = GeneratedFolder + "/MapBoxFallback.asset";
    const string LineMaterialPath = GeneratedFolder + "/MapLines.mat";
    const string FaceDepthMaterialPath = GeneratedFolder + "/MapFaceDepth.mat";
    const string FaceTintMaterialPath = GeneratedFolder + "/MapFaceTint.mat";
    const string TerminalLineMaterialPath = GeneratedFolder + "/MapTerminalLines.mat";
    const string TerminalFaceTintMaterialPath = GeneratedFolder + "/MapTerminalFaceTint.mat";
    const string DoorFolder = GeneratedFolder + "/Doors";
    const string LabelMaterialPath = GeneratedFolder + "/MapLabel.mat";
    const string LabelMaskMaterialPath = GeneratedFolder + "/MapLabel Mask.mat";
    const string PlayerMaterialPath = GeneratedFolder + "/MapPlayer.mat";
    const string LabelFolder = GeneratedFolder + "/Labels";
    const string LineShader = "Entropy/HoloMap Lines";
    const string FaceShader = "Entropy/HoloMap Faces";
    const string SeeThroughLineShader = "Entropy/HoloMap Lines XRay";

    // Raise MapVersion to rebuild every map, WireVersion to remake every line mesh
    const int MapVersion = 25;

    static readonly Color PlayerColor = new Color(0.3f, 1f, 0.45f, 0.95f);

    static readonly Color[] DoorColors =
    {
        new Color(0.35f, 0.85f, 1f, 0.6f),
        new Color(0.9f, 0.74f, 0.14f, 0.75f),
        new Color(0.9f, 0.2f, 0.16f, 0.75f),
    };
    static readonly float[] DoorFillAlphas = { 0.06f, 0.12f, 0.12f };

    // Door parts smaller than this share of the door are left out
    const float DoorPartShare = 0.25f;
    const float DoorMergeDistance = 0.15f;

    const string DoorBarsName = "DoorBars";

    const float TerminalMergeDistance = 0.02f;
    const int WireVersion = 3;

    public static readonly Color RoomColor = new Color(0.35f, 0.85f, 1f, 1f);
    public static readonly Color TerminalColor = new Color(1f, 0.55f, 0.15f, 1f);
    static readonly Color LabelColor = new Color(0.85f, 0.95f, 1f, 1f);
    const float TerminalTintAlpha = 0.6f;

    // Draw order: fills and label masks, then labels, then terminal lines, then room faces and lines
    const int TerminalFillQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 95;
    const int LabelMaskQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 96;
    const int LabelQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 97;
    const int InFrontOfRoomsQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 98;
    const int FaceDepthQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 99;

    const int RoomTintQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 100;
    const int RoomLinesQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 101;

    // Stencil: label letters mark 1, fills mark 2 (URP uses 4 and 8)
    const int LabelStencilBit = 1;
    const int FillStencilBit = 2;

    public const float LabelGap = 0.5f;

    public class WirePiece
    {
        public Mesh mesh;
        public Matrix4x4 toRoom;
    }

    public class Placement
    {
        public string name;
        public Transform source;
        public MapEntryKind kind;
        public GameObject proxy;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Bounds box;
        public List<WirePiece> wires = new List<WirePiece>();
        public Vector3 worldScale;
        public float mergeDistance;
        public string wireKey;

        public int index = -1;
        public bool active = true;

        public MapDoorState doorState;

        public string panelPath = "";

        // Empty means always powered
        public string sector = "";

        public Placement CopyWith(List<WirePiece> someWires)
        {
            var copy = (Placement)MemberwiseClone();
            copy.wires = someWires;
            return copy;
        }
    }

    public class LabelPlacement
    {
        public string text;
        public Vector3 position;

        public TMP_FontAsset font;

        public bool active = true;

        public GameObject anchor;
        public string sector = "";
    }

    public class ScanResult
    {
        public List<Placement> placements = new List<Placement>();
        public List<LabelPlacement> labels = new List<LabelPlacement>();
        public float labelSize;
        public Color labelColor = Color.white;
        public float playerMarkerSize;
        public float depthDimming;
        public float unpoweredBrightness;
        public float unpoweredSaturation;

        public List<string> sectorNames = new List<string>();
        public List<string> unsectored = new List<string>();
        public List<string> emptySectors = new List<string>();
        public List<string> lostAnchors = new List<string>();
        public List<string> duplicateTerminals = new List<string>();
        public List<string> missingMeshes = new List<string>();
        public List<string> hidden = new List<string>();
        public List<GameObject> unlisted = new List<GameObject>();
        public List<string> notPrefabs = new List<string>();
        public List<string> disabled = new List<string>();
        public List<string> noGeometry = new List<string>();
        public Dictionary<GameObject, MapEntryKind> handAdded = new Dictionary<GameObject, MapEntryKind>();
        public HashSet<Transform> placed = new HashSet<Transform>();
        public Dictionary<Mesh, string> meshHashes = new Dictionary<Mesh, string>();
    }

    static MapBuilder()
    {
        EditorSceneManager.sceneSaved += OnSceneSaved;
    }

    static void OnSceneSaved(Scene scene)
    {
        if (BuildPipeline.isBuildingPlayer) return;

        MapLevel level = FindLevel(scene);
        MapProxyLibrary library = AssetDatabase.LoadAssetAtPath<MapProxyLibrary>(LibraryPath);
        if (level != null && library != null)
        {
            Build(level, library);
        }
    }

    public static MapLevel FindLevel(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            MapLevel level = root.GetComponentInChildren<MapLevel>(true);
            if (level != null) return level;
        }
        return null;
    }

    public static MapProxyLibrary GetOrCreateLibrary()
    {
        MapProxyLibrary library = AssetDatabase.LoadAssetAtPath<MapProxyLibrary>(LibraryPath);
        if (library != null) return library;

        EnsureFolder(ProxyFolder);
        library = ScriptableObject.CreateInstance<MapProxyLibrary>();
        AssetDatabase.CreateAsset(library, LibraryPath);
        return library;
    }

    public static ScanResult Collect(MapLevel level, MapProxyLibrary library)
    {
        var result = new ScanResult();

        foreach (MapLevel.SceneEntry entry in level.sceneEntries)
        {
            if (entry.target == null) continue;

            result.handAdded[entry.target] = entry.kind;
            if (HasShape(entry.kind))
            {
                GameObject proxy = entry.kind == MapEntryKind.Door ? null : entry.proxy;
                AddShape(entry.target.transform, level.transform, entry.kind, proxy, entry.useMeshes ? entry : null, result);
            }
            else if (entry.kind == MapEntryKind.Icon)
            {
                result.placements.Add(CreatePlacement(entry.target.transform, level.transform, entry.kind, entry.proxy));
            }
        }

        foreach ((Terminal terminal, int index) in FindTerminals(level))
        {
            GameObject go = terminal.gameObject;
            if (result.handAdded.TryGetValue(go, out MapEntryKind addedKind))
            {
                Placement own = addedKind == MapEntryKind.Terminal ? result.placements.Find(p => p.source == terminal.transform) : null;
                if (own != null) SetTerminalState(own, terminal, index, result);
                continue;
            }

            result.handAdded[go] = MapEntryKind.Terminal;
            AddTerminal(terminal.transform, level.transform, index, result);
        }

        foreach ((DoorScript door, int index) in FindDoors(level))
        {
            GameObject go = door.gameObject;
            if (result.handAdded.TryGetValue(go, out MapEntryKind addedKind))
            {
                Placement own = addedKind == MapEntryKind.Door ? result.placements.Find(p => p.source == door.transform) : null;
                if (own != null) SetDoorState(own, door, index, result);
                continue;
            }

            result.handAdded[go] = MapEntryKind.Door;
            AddDoor(door, level.transform, index, result);
        }

        foreach (Transform child in level.transform)
        {
            Walk(child, child, level.transform, false, library, result);
        }

        var walked = new HashSet<GameObject>();
        foreach (GameObject extra in level.alsoScan)
        {
            if (!CanAlsoScan(extra, level) || !walked.Add(extra)) continue;
            if (!extra.activeInHierarchy)
            {
                result.disabled.Add(extra.name);
                continue;
            }
            foreach (Transform child in extra.transform)
            {
                Walk(child, child, level.transform, false, library, result);
            }
        }

        foreach (MapLevel.SceneEntry entry in level.sceneEntries)
        {
            if (entry.target == null || !entry.target.activeInHierarchy) continue;

            Transform t = entry.target.transform;
            if (t.IsChildOf(level.transform) || level.transform.IsChildOf(t)) continue;
            if (walked.Any(root => t.IsChildOf(root.transform))) continue;
            Walk(t, t, level.transform, false, library, result);
        }

        CollectLabels(level, library, result);
        AssignSectors(level, result);

        // Terminals are matched by name, so names must be unique
        var terminalNames = new HashSet<string>();
        foreach (Placement placement in result.placements)
        {
            if (placement.kind != MapEntryKind.Terminal || terminalNames.Add(placement.name)) continue;
            if (!result.duplicateTerminals.Contains(placement.name)) result.duplicateTerminals.Add(placement.name);
        }
        return result;
    }

    public static List<(Terminal terminal, int index)> FindTerminals(MapLevel level)
    {
        var found = new List<(Terminal terminal, int index)>();
        GameObject[] roots = level.gameObject.scene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            foreach (TerminalManager manager in root.GetComponentsInChildren<TerminalManager>(true))
            {
                // Private in the game's code, so read the way the Inspector does
                SerializedProperty list = new SerializedObject(manager).FindProperty("terminals");
                if (list == null) continue;
                for (int i = 0; i < list.arraySize; i++)
                {
                    var terminal = list.GetArrayElementAtIndex(i).objectReferenceValue as Terminal;
                    if (terminal != null && !found.Exists(item => item.terminal == terminal)) found.Add((terminal, i));
                }
            }
        }
        foreach (GameObject root in roots)
        {
            foreach (Terminal terminal in root.GetComponentsInChildren<Terminal>(true))
            {
                if (!found.Exists(item => item.terminal == terminal)) found.Add((terminal, -1));
            }
        }
        return found;
    }

    static void AddTerminal(Transform terminal, Transform levelRoot, int index, ScanResult result)
    {
        var shape = new MapShape { useMeshes = true, mergeDistance = TerminalMergeDistance };
        foreach (MeshFilter filter in terminal.GetComponentsInChildren<MeshFilter>(!terminal.gameObject.activeInHierarchy))
        {
            if (filter.sharedMesh != null && !shape.meshes.Contains(filter.sharedMesh)) shape.meshes.Add(filter.sharedMesh);
        }

        Placement placement = AddShape(terminal, levelRoot, MapEntryKind.Terminal, null, shape, result);
        if (placement != null) SetTerminalState(placement, terminal.GetComponent<Terminal>(), index, result);
    }

    public static List<(DoorScript door, int index)> FindDoors(MapLevel level)
    {
        var found = new List<(DoorScript door, int index)>();
        GameObject[] roots = level.gameObject.scene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            foreach (DoorManager manager in root.GetComponentsInChildren<DoorManager>(true))
            {
                DoorScript[] doors = MapDoors.FindDoors(manager);
                for (int i = 0; i < doors.Length; i++)
                {
                    if (!found.Exists(item => item.door == doors[i])) found.Add((doors[i], i));
                }
            }
        }
        foreach (GameObject root in roots)
        {
            foreach (DoorScript door in root.GetComponentsInChildren<DoorScript>(true))
            {
                if (!found.Exists(item => item.door == door)) found.Add((door, -1));
            }
        }
        return found;
    }

    static void AddDoor(DoorScript door, Transform levelRoot, int index, ScanResult result)
    {
        Transform t = door.transform;
        Transform panel = GetDoorPanel(door);

        var frameShape = new MapShape { useMeshes = true, mergeDistance = DoorMergeDistance };
        var panelShape = new MapShape { useMeshes = true, mergeDistance = DoorMergeDistance };
        foreach (MeshFilter filter in GetDoorParts(door))
        {
            MapShape shape = panel != null && filter.transform.IsChildOf(panel) ? panelShape : frameShape;
            if (!shape.meshes.Contains(filter.sharedMesh)) shape.meshes.Add(filter.sharedMesh);
        }
        bool showsState = panel != null && panelShape.meshes.Count > 0;

        if (frameShape.meshes.Count > 0)
        {
            Placement frame = AddShape(t, levelRoot, MapEntryKind.Room, null, frameShape, result, showsState ? panel : null);
            if (frame != null)
            {
                frame.active = t.gameObject.activeInHierarchy;
                if (!showsState && !frame.active) result.hidden.Add(t.name);
            }
        }
        if (!showsState) return;

        Placement panelPiece = AddShape(panel, levelRoot, MapEntryKind.Door, null, panelShape, result);
        if (panelPiece == null) return;
        panelPiece.name = t.name;
        panelPiece.panelPath = AnimationUtility.CalculateTransformPath(panel, t);
        SetDoorState(panelPiece, door, index, result);
    }

    public static bool IsDoorOnMap(MapLevel level, GameObject door)
    {
        return !level.sceneEntries.Exists(item => item.target == door && item.kind == MapEntryKind.Ignore);
    }

    public static void SetDoorOnMap(MapLevel level, GameObject door, bool onMap)
    {
        if (onMap)
        {
            level.sceneEntries.RemoveAll(item => item.target == door && item.kind == MapEntryKind.Ignore);
        }
        else if (!level.sceneEntries.Exists(item => item.target == door))
        {
            level.sceneEntries.Add(new MapLevel.SceneEntry { target = door, kind = MapEntryKind.Ignore });
        }
    }

    public static Transform GetDoorPanel(DoorScript door)
    {
        var panel = new SerializedObject(door).FindProperty("doorPart")?.objectReferenceValue as Transform;
        return panel != null && panel.IsChildOf(door.transform) ? panel : null;
    }

    static List<MeshFilter> GetDoorParts(DoorScript doorScript)
    {
        Transform door = doorScript.transform;
        var parts = new List<MeshFilter>();
        if (!TryGetBounds(door, door, out Bounds whole)) return parts;
        float doorSize = Mathf.Max(whole.size.x, whole.size.y, whole.size.z);

        var holograms = new List<Transform>();
        foreach (MeshRenderer hologram in doorScript.hologramGroup ?? new MeshRenderer[0])
        {
            if (hologram != null) holograms.Add(hologram.transform);
        }

        foreach (MeshFilter filter in door.GetComponentsInChildren<MeshFilter>(!door.gameObject.activeInHierarchy))
        {
            if (filter.sharedMesh == null || !TryGetBounds(filter.transform, door, out Bounds part)) continue;
            if (Mathf.Max(part.size.x, part.size.y, part.size.z) < doorSize * DoorPartShare) continue;
            if (holograms.Exists(hologram => filter.transform.IsChildOf(hologram))) continue;
            if (IsInDoorBars(filter.transform, door)) continue;
            parts.Add(filter);
        }
        return parts;
    }

    static bool IsInDoorBars(Transform part, Transform door)
    {
        for (Transform t = part; t != null && t != door; t = t.parent)
        {
            if (t.name.StartsWith(DoorBarsName)) return true;
        }
        return false;
    }

    static void SetDoorState(Placement placement, DoorScript door, int index, ScanResult result)
    {
        placement.index = index;
        placement.doorState = MapDoors.GetState(door.DoorState);
        placement.active = door.gameObject.activeInHierarchy;
        if (!placement.active) result.hidden.Add(door.name);
    }

    static void SetTerminalState(Placement placement, Terminal terminal, int index, ScanResult result)
    {
        placement.index = index;
        placement.active = terminal.gameObject.activeInHierarchy;
        if (!placement.active) result.hidden.Add(terminal.name);
    }

    public static bool CanAlsoScan(GameObject go, MapLevel level)
    {
        if (go == null || go.scene != level.gameObject.scene) return false;

        Transform t = go.transform;
        if (t.IsChildOf(level.transform) || level.transform.IsChildOf(t)) return false;
        return !level.alsoScan.Exists(other => other != null && other != go && t.IsChildOf(other.transform));
    }

    public static bool HasShape(MapEntryKind kind) => kind == MapEntryKind.Room || kind == MapEntryKind.Terminal || kind == MapEntryKind.Door;
    public static bool IsMarker(MapEntryKind kind) => kind == MapEntryKind.Icon || kind == MapEntryKind.Terminal || kind == MapEntryKind.Door;

    public static int Scan(MapLevel level, MapProxyLibrary library)
    {
        List<GameObject> unlisted = Collect(level, library).unlisted;

        Undo.RecordObject(library, "Scan Level");
        int added = 0;
        foreach (GameObject prefab in unlisted)
        {
            if (HasBaseIn(prefab, unlisted)) continue;

            library.entries.Add(new MapProxyLibrary.Entry { levelPrefab = prefab, kind = GuessKind(prefab) });
            added++;
        }

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssetIfDirty(library);
        return added;
    }

    public static bool Build(MapLevel level, MapProxyLibrary library)
    {
        return Build(level, library, Collect(level, library));
    }

    public static bool Build(MapLevel level, MapProxyLibrary library, ScanResult scan)
    {
        Scene levelScene = level.gameObject.scene;
        if (string.IsNullOrEmpty(levelScene.path))
        {
            Debug.LogWarning("Save the scene before building its hologram map.");
            return false;
        }

        string mapPath = $"{Path.GetDirectoryName(levelScene.path).Replace('\\', '/')}/{levelScene.name}_Map.prefab";
        string signature = GetSignature(scan);

        var importer = AssetImporter.GetAtPath(mapPath);
        bool changed = importer == null || importer.userData != signature;
        if (changed)
        {
            SaveMap(mapPath, levelScene.name, scan);
            importer = AssetImporter.GetAtPath(mapPath);
            importer.userData = signature;
            importer.SaveAndReimport();
            DeleteUnused(WireFolder, "t:Mesh");
            DeleteUnused(LabelFolder, "t:Material");
            Debug.Log($"Hologram map built: {mapPath}");

            foreach (string label in scan.lostAnchors)
            {
                Debug.LogWarning($"Map label \"{label}\" lost the object it sat over, so it was left off the map. Fix it in the Map Builder's Labels list.");
            }
        }
        else
        {
            foreach (LabelPlacement label in scan.labels)
            {
                TMP_FontAsset font = GetLabelFont(label);
                if (font == null) continue;
                GetLabelMaterial(font, label.font != null);
                GetLabelMaskMaterial(font, label.font != null);
            }
        }

        GameObject map = AssetDatabase.LoadAssetAtPath<GameObject>(mapPath);
        if (level.mapPrefab != map)
        {
            level.mapPrefab = map;
            EditorUtility.SetDirty(level);
        }
        return changed;
    }

    static void Walk(Transform t, Transform top, Transform levelRoot, bool inRoom, MapProxyLibrary library, ScanResult result)
    {
        GameObject go = t.gameObject;
        if (result.handAdded.TryGetValue(go, out MapEntryKind handAddedKind))
        {
            if (IsMarker(handAddedKind) || !go.activeSelf) return;

            MapProxyLibrary.Entry entry = FindEntry(go, library);
            if (entry != null && entry.lookInside)
            {
                AddInside(t, levelRoot, entry, library, result);
            }
            inRoom = true;
        }
        else if (!go.activeSelf)
        {
            if (!inRoom) result.disabled.Add(go.name);
            return;
        }
        else if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
        {
            MapProxyLibrary.Entry entry = FindEntry(go, library);
            if (entry == null)
            {
                if (!inRoom)
                {
                    GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
                    if (prefab != null && !result.unlisted.Contains(prefab)) result.unlisted.Add(prefab);
                    inRoom = true;
                }
            }
            else if (entry.kind == MapEntryKind.Terminal || entry.kind == MapEntryKind.Door)
            {
                GameObject proxy = entry.kind == MapEntryKind.Door ? null : entry.proxy;
                AddShape(t, levelRoot, entry.kind, proxy, entry.useMeshes ? entry : null, result);
                return;
            }
            else if (entry.kind == MapEntryKind.Icon)
            {
                if (result.placed.Add(t))
                {
                    result.placements.Add(CreatePlacement(t, levelRoot, entry.kind, entry.proxy));
                }
                return;
            }
            else
            {
                if (entry.kind == MapEntryKind.Room && (!inRoom || entry.placeNested))
                {
                    AddShape(t, levelRoot, entry.kind, entry.proxy, entry.useMeshes ? entry : null, result);
                }
                if (entry.lookInside)
                {
                    AddInside(t, levelRoot, entry, library, result);
                }

                inRoom = true;
            }
        }
        else if (!inRoom && go.TryGetComponent<Renderer>(out _))
        {
            if (!result.notPrefabs.Contains(top.name)) result.notPrefabs.Add(top.name);
        }

        foreach (Transform child in t)
        {
            Walk(child, top, levelRoot, inRoom, library, result);
        }
    }

    static void AddInside(Transform room, Transform levelRoot, MapProxyLibrary.Entry entry, MapProxyLibrary library, ScanResult result)
    {
        foreach (Transform child in room)
        {
            GameObject go = child.gameObject;
            if (!go.activeSelf || result.handAdded.ContainsKey(go)) continue;

            MapProxyLibrary.InsideEntry inside = FindInside(go, entry);
            if (inside == null) continue;

            GameObject proxy = inside.proxy;
            MapShape meshSource = inside.useMeshes ? inside : null;
            MapEntryKind kind = MapEntryKind.Room;

            MapProxyLibrary.Entry listed = FindEntry(go, library);
            if (listed != null)
            {
                if (proxy == null) proxy = listed.proxy;
                if (meshSource == null && listed.useMeshes) meshSource = listed;

                if (listed.kind == MapEntryKind.Terminal || listed.kind == MapEntryKind.Door) kind = listed.kind;
                if (kind == MapEntryKind.Door) proxy = null;
            }
            AddShape(child, levelRoot, kind, proxy, meshSource, result);
        }
    }

    static Placement AddShape(Transform room, Transform levelRoot, MapEntryKind kind, GameObject proxy, MapShape meshSource, ScanResult result, Transform skip = null)
    {
        if (!result.placed.Add(room)) return null;

        Placement placement = CreatePlacement(room, levelRoot, kind, proxy);
        if (proxy == null && meshSource != null)
        {
            placement.wires = FindWires(room, meshSource.meshes, skip);
            placement.worldScale = room.lossyScale;
            placement.mergeDistance = meshSource.mergeDistance;
            placement.wireKey = GetWireKey(placement, result);

            if (meshSource.meshes.Count > 0 && placement.wires.Count == 0) result.missingMeshes.Add(room.name);
        }
        if (proxy == null && placement.wires.Count == 0)
        {
            placement.box = GetLocalBounds(room);
            if (placement.box.size == Vector3.zero)
            {
                result.noGeometry.Add(room.name);
                return null;
            }
        }
        result.placements.Add(placement);
        return placement;
    }

    static List<WirePiece> FindWires(Transform room, List<Mesh> meshes, Transform skip = null)
    {
        var wires = new List<WirePiece>();
        foreach (MeshFilter filter in room.GetComponentsInChildren<MeshFilter>(!room.gameObject.activeInHierarchy))
        {
            if (filter.sharedMesh == null || !meshes.Contains(filter.sharedMesh)) continue;
            if (skip != null && filter.transform.IsChildOf(skip)) continue;

            wires.Add(new WirePiece
            {
                mesh = filter.sharedMesh,
                toRoom = room.worldToLocalMatrix * filter.transform.localToWorldMatrix
            });
        }
        return wires;
    }

    static string GetWireKey(Placement placement, ScanResult result)
    {
        Vector3 scale = placement.worldScale;
        var text = new StringBuilder($"{WireVersion},{GetKeyText(scale.x)},{GetKeyText(scale.y)},{GetKeyText(scale.z)},{GetKeyText(placement.mergeDistance)}");
        foreach (WirePiece wire in placement.wires)
        {
            text.Append($"|{GetMeshHash(wire.mesh, result)}");
            for (int i = 0; i < 16; i++)
            {
                text.Append($",{GetKeyText(wire.toRoom[i])}");
            }
        }
        return Hash128.Compute(text.ToString()).ToString();
    }

    static string GetMeshHash(Mesh mesh, ScanResult result)
    {
        if (!result.meshHashes.TryGetValue(mesh, out string hash))
        {
            hash = $"{Hash128.Compute(mesh.vertices)},{Hash128.Compute(mesh.triangles)}";
            result.meshHashes[mesh] = hash;
        }
        return hash;
    }

    // Rounded so float noise doesn't change the key
    static int GetKeyText(float value)
    {
        return Mathf.RoundToInt(value * 1000f);
    }

    public static List<Vector3> GetWireLines(Placement placement)
    {
        Vector3 scale = placement.worldScale;
        var lines = new List<Vector3>();
        foreach (WirePiece wire in placement.wires)
        {
            Matrix4x4 toRoom = Matrix4x4.Scale(scale) * wire.toRoom;
            foreach (Vector3 point in MapLines.GetHardEdges(wire.mesh))
            {
                lines.Add(toRoom.MultiplyPoint3x4(point));
            }
        }

        if (placement.mergeDistance > 0f)
        {
            lines = MapLines.Merge(lines, placement.mergeDistance);
        }

        var inverse = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
        for (int i = 0; i < lines.Count; i++)
        {
            lines[i] = Vector3.Scale(lines[i], inverse);
        }
        return lines;
    }

    static Placement CreatePlacement(Transform t, Transform levelRoot, MapEntryKind kind, GameObject proxy)
    {
        Vector3 levelScale = levelRoot.lossyScale;
        Vector3 scale = t.lossyScale;
        return new Placement
        {
            name = t.name,
            source = t,
            kind = kind,
            proxy = proxy,
            position = levelRoot.InverseTransformPoint(t.position),
            rotation = Quaternion.Inverse(levelRoot.rotation) * t.rotation,
            scale = new Vector3(scale.x / levelScale.x, scale.y / levelScale.y, scale.z / levelScale.z)
        };
    }

    static IEnumerable<GameObject> GetSources(GameObject go)
    {
        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(go);
        while (source != null)
        {
            yield return source;
            source = PrefabUtility.GetCorrespondingObjectFromSource(source);
        }
    }

    public static MapProxyLibrary.Entry FindEntry(GameObject go, MapProxyLibrary library)
    {
        foreach (GameObject source in GetSources(go))
        {
            MapProxyLibrary.Entry entry = library.Find(source);
            if (entry != null) return entry;
        }
        return null;
    }

    static MapProxyLibrary.InsideEntry FindInside(GameObject go, MapProxyLibrary.Entry entry)
    {
        foreach (GameObject source in GetSources(go))
        {
            MapProxyLibrary.InsideEntry inside = entry.inside.Find(item => item.child == source);
            if (inside != null) return inside;
        }
        return null;
    }

    static bool HasBaseIn(GameObject prefab, List<GameObject> prefabs)
    {
        foreach (GameObject source in GetSources(prefab))
        {
            if (prefabs.Contains(source)) return true;
        }
        return false;
    }

    static MapEntryKind GuessKind(GameObject prefab)
    {
        return prefab.GetComponentInChildren<SkinnedMeshRenderer>(true) != null
            ? MapEntryKind.Ignore
            : MapEntryKind.Room;
    }

    static Bounds GetLocalBounds(Transform room)
    {
        TryGetBounds(room, room, out Bounds bounds);
        return bounds;
    }

    static bool TryGetBounds(Transform target, Transform space, out Bounds bounds)
    {
        bounds = new Bounds();
        bool hasBounds = false;

        bool includeInactive = !target.gameObject.activeInHierarchy;
        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(includeInactive))
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;

            Matrix4x4 toRoom = space.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            Bounds local = renderer.localBounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = local.center + Vector3.Scale(local.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 point = toRoom.MultiplyPoint3x4(corner);

                if (hasBounds)
                {
                    bounds.Encapsulate(point);
                }
                else
                {
                    bounds = new Bounds(point, Vector3.zero);
                    hasBounds = true;
                }
            }
        }
        return hasBounds;
    }

    static string GetSignature(ScanResult scan)
    {
        var text = new StringBuilder(MapVersion.ToString());
        foreach (Placement placement in scan.placements)
        {
            string proxyPath = placement.proxy != null ? AssetDatabase.GetAssetPath(placement.proxy) : "";
            text.Append($"|{placement.name},{placement.kind},{AssetDatabase.AssetPathToGUID(proxyPath)}");
            text.Append($",{placement.position:F3},{placement.rotation:F4},{placement.scale:F3}");
            text.Append($",{placement.box.center:F3},{placement.box.size:F3}");
            text.Append($",{placement.wireKey},{placement.index},{placement.active},{placement.doorState},{placement.panelPath},{placement.sector}");
        }
        text.Append($"|S,{GetKeyText(scan.unpoweredBrightness)},{GetKeyText(scan.unpoweredSaturation)}");
        foreach (string sector in scan.sectorNames) text.Append($",{sector.Length}:{sector}");

        text.Append($"|P,{GetKeyText(scan.playerMarkerSize)}");
        text.Append($"|D,{GetKeyText(scan.depthDimming)}");

        // Reading TMP settings can open its import window
        if (scan.labels.Count > 0)
        {
            text.Append($"|L,{GetKeyText(scan.labelSize)},{ColorUtility.ToHtmlStringRGBA(scan.labelColor)}");
            foreach (LabelPlacement label in scan.labels)
            {
                // Length prefix keeps labels containing | or , apart
                string font = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(GetLabelFont(label)));
                text.Append($"|{label.text.Length}:{label.text},{label.position:F3},{font},{(label.font != null ? "picked" : "default")},{label.active},{label.sector}");
            }
        }
        return Hash128.Compute(text.ToString()).ToString();
    }

    static void SaveMap(string mapPath, string levelName, ScanResult scan)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = new GameObject($"{levelName}_Map");
            SceneManager.MoveGameObjectToScene(root, scene);

            Transform rooms = AddGroup(MapLevel.RoomsGroup, root.transform);
            Transform icons = AddGroup(MapLevel.IconsGroup, root.transform);
            Transform terminals = AddGroup(MapLevel.TerminalsGroup, root.transform);
            Transform doors = AddGroup(MapLevel.DoorsGroup, root.transform);
            Transform labels = AddGroup(MapLevel.LabelsGroup, root.transform);
            labels.gameObject.AddComponent<MapFaceCamera>();

            var doorColors = doors.gameObject.AddComponent<MapDoors>();
            doorColors.working = GetDoorMaterials(MapDoorState.Working);
            doorColors.broken = GetDoorMaterials(MapDoorState.Broken);
            doorColors.locked = GetDoorMaterials(MapDoorState.Locked);
            doorColors.levelScene = levelName;
            doorColors.depthDimming = scan.depthDimming;

            var sectorParts = new Dictionary<string, List<Renderer>>();
            var sectorLabels = new Dictionary<string, List<TMP_Text>>();
            foreach (Placement placement in scan.placements)
            {
                Transform group = placement.kind == MapEntryKind.Terminal ? terminals
                    : placement.kind == MapEntryKind.Door ? doors
                    : placement.kind == MapEntryKind.Icon ? icons
                    : rooms;
                GameObject node = CreateNode(placement, group);
                if (placement.sector != "") GetList(sectorParts, placement.sector).AddRange(node.GetComponentsInChildren<Renderer>(true));
            }
            if (scan.labels.Count > 0)
            {
                AddLabels(scan, labels, sectorLabels);
            }
            AddPlayerMarker(root.transform, levelName, scan.playerMarkerSize);
            AddSectors(root, scan, sectorParts, sectorLabels);
            PrefabUtility.SaveAsPrefabAsset(root, mapPath);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    static void AddPlayerMarker(Transform root, string levelName, float size)
    {
        var marker = new GameObject(MapLevel.PlayerMarkerName, typeof(MeshFilter), typeof(MeshRenderer));
        marker.transform.SetParent(root, false);
        marker.transform.localScale = Vector3.one * size;
        marker.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");

        var ball = marker.GetComponent<MeshRenderer>();
        ball.sharedMaterial = GetPlayerMaterial();
        ball.enabled = false;
        marker.AddComponent<MapPlayerMarker>().levelScene = levelName;
    }

    static Material GetPlayerMaterial()
    {
        return KeepFloat(KeepQueue(GetMaterial(PlayerMaterialPath, FaceShader, ball =>
        {
            ball.SetColor("_Color", PlayerColor);
            ball.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Back);
        }), InFrontOfRoomsQueue), DepthDimmedProperty, 0f);
    }

    static void AddSectors(GameObject root, ScanResult scan, Dictionary<string, List<Renderer>> parts, Dictionary<string, List<TMP_Text>> labels)
    {
        if (scan.sectorNames.Count == 0) return;

        var script = root.AddComponent<MapSectors>();
        script.unpoweredBrightness = scan.unpoweredBrightness;
        script.unpoweredSaturation = scan.unpoweredSaturation;
        script.sectors = scan.sectorNames.Select(name => new MapSectors.Sector
        {
            name = name,
            parts = GetList(parts, name).ToArray(),
            labels = GetList(labels, name).ToArray(),
        }).ToArray();
    }

    static List<T> GetList<T>(Dictionary<string, List<T>> lists, string key)
    {
        if (!lists.TryGetValue(key, out List<T> list))
        {
            list = new List<T>();
            lists[key] = list;
        }
        return list;
    }

    static void AssignSectors(MapLevel level, ScanResult result)
    {
        result.unpoweredBrightness = level.unpoweredBrightness;
        result.unpoweredSaturation = level.unpoweredSaturation;
        foreach (MapLevel.SectorEntry sector in level.sectors)
        {
            string name = GetSectorName(sector);
            if (name != "" && !result.sectorNames.Contains(name)) result.sectorNames.Add(name);
        }
        if (result.sectorNames.Count == 0) return;

        Transform levelRoot = level.transform;
        var placements = new List<Placement>();
        foreach (Placement placement in result.placements)
        {
            if (placement.source == null)
            {
                placements.Add(placement);
                continue;
            }

            if (placement.kind == MapEntryKind.Room && placement.wires.Count > 0 && !placement.source.TryGetComponent<DoorScript>(out _))
            {
                var groups = new List<(string sector, List<WirePiece> wires)>();
                foreach (WirePiece wire in placement.wires)
                {
                    Vector3 world = placement.source.TransformPoint(wire.toRoom.MultiplyPoint3x4(wire.mesh.bounds.center));
                    string sector = FindSectorAt(level, levelRoot.InverseTransformPoint(world));
                    int group = groups.FindIndex(g => g.sector == sector);
                    if (group >= 0) groups[group].wires.Add(wire);
                    else groups.Add((sector, new List<WirePiece> { wire }));
                }

                if (groups.Count == 1)
                {
                    placement.sector = groups[0].sector;
                    placements.Add(placement);
                    continue;
                }
                foreach ((string sector, List<WirePiece> wires) in groups)
                {
                    Placement part = placement.CopyWith(wires);
                    part.sector = sector;
                    part.name = sector != "" ? $"{placement.name} ({sector})" : placement.name;
                    part.wireKey = GetWireKey(part, result);
                    placements.Add(part);
                }
                continue;
            }

            Vector3 middle = TryGetBounds(placement.source, levelRoot, out Bounds own) ? own.center : placement.position;
            placement.sector = FindSectorAt(level, middle);
            placements.Add(placement);
        }
        result.placements.Clear();
        result.placements.AddRange(placements);

        foreach (Placement placement in result.placements)
        {
            bool isRoom = placement.kind == MapEntryKind.Room && placement.source != null && !placement.source.TryGetComponent<DoorScript>(out _);
            if (placement.sector == "" && isRoom && !result.unsectored.Contains(placement.name)) result.unsectored.Add(placement.name);
        }

        foreach (LabelPlacement label in result.labels)
        {
            label.sector = FindSectorAt(level, label.position - Vector3.up * LabelGap);
            if (label.sector != "" || label.anchor == null) continue;

            Placement over = null;
            foreach (Placement placement in result.placements)
            {
                if (placement.source == null || !label.anchor.transform.IsChildOf(placement.source)) continue;
                if (over == null || placement.source.IsChildOf(over.source)) over = placement;
            }
            if (over != null) label.sector = over.sector;
        }

        foreach (string name in result.sectorNames)
        {
            if (!result.placements.Exists(placement => placement.sector == name)) result.emptySectors.Add(name);
        }
    }

    public static string GetSectorName(MapLevel.SectorEntry sector)
    {
        return sector.name != null ? sector.name.Trim() : "";
    }

    public static string FindSectorAt(MapLevel level, Vector3 point)
    {
        string best = "";
        float bestSize = float.MaxValue;
        foreach (MapLevel.SectorEntry sector in level.sectors)
        {
            string name = GetSectorName(sector);
            if (name == "") continue;
            foreach (Bounds box in sector.boxes)
            {
                if (!box.Contains(point)) continue;
                float size = box.size.x * box.size.y * box.size.z;
                if (size < bestSize)
                {
                    best = name;
                    bestSize = size;
                }
            }
        }
        return best;
    }

    static Transform AddGroup(string name, Transform parent)
    {
        var group = new GameObject(name).transform;
        group.SetParent(parent, false);
        return group;
    }

    static GameObject CreateNode(Placement placement, Transform parent)
    {
        GameObject node;
        if (placement.proxy != null)
        {
            node = (GameObject)PrefabUtility.InstantiatePrefab(placement.proxy, parent);
        }
        else
        {
            node = new GameObject();
            node.transform.SetParent(parent, false);
        }

        node.name = placement.name;
        node.transform.SetLocalPositionAndRotation(placement.position, placement.rotation);
        node.transform.localScale = placement.scale;

        if (placement.kind == MapEntryKind.Terminal)
        {
            node.AddComponent<MapTerminal>().index = placement.index;
        }

        Transform lines = null;
        if (placement.wires.Count > 0)
        {
            lines = AddLines("Wire", node.transform, GetWireMesh(placement), GetMaterials(placement));
        }
        else if (placement.proxy == null && HasShape(placement.kind))
        {
            lines = AddLines("Box", node.transform, GetBoxMesh(), GetMaterials(placement));
            lines.localPosition = placement.box.center;
            lines.localScale = placement.box.size;
        }

        if (placement.kind == MapEntryKind.Door)
        {
            var door = node.AddComponent<MapDoor>();
            door.index = placement.index;
            door.state = placement.doorState;
            door.panelPath = placement.panelPath;
            door.pieces = lines != null ? new[] { lines.GetComponent<MeshRenderer>() } : new MeshRenderer[0];
        }

        node.SetActive(placement.active);
        return node;
    }

    static Transform AddLines(string name, Transform parent, Mesh lines, Material[] materials)
    {
        var piece = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        piece.transform.SetParent(parent, false);
        piece.GetComponent<MeshFilter>().sharedMesh = lines;

        // The extra material redraws the faces: depth pass, then tint
        piece.GetComponent<MeshRenderer>().sharedMaterials = materials;
        return piece.transform;
    }

    static void AddLabels(ScanResult scan, Transform group, Dictionary<string, List<TMP_Text>> sectorLabels)
    {
        foreach (LabelPlacement label in scan.labels)
        {
            TMP_FontAsset font = GetLabelFont(label);
            if (font == null)
            {
                Debug.LogWarning("Map labels were left out because TextMesh Pro has no default font. Pick a Label Font in the Map Builder, or import TextMesh Pro's Essential Resources.");
                return;
            }
            bool picked = label.font != null;
            GameObject created = CreateLabel(label, group, font, GetLabelMaterial(font, picked), GetLabelMaskMaterial(font, picked), scan.labelSize, scan.labelColor);
            if (label.sector != "") GetList(sectorLabels, label.sector).Add(created.GetComponent<TMP_Text>());
        }
    }

    static TMP_FontAsset GetLabelFont(LabelPlacement label)
    {
        return label.font != null ? label.font : GetDefaultLabelFont();
    }

    static TMP_FontAsset GetDefaultLabelFont()
    {
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        return font != null ? font : TMP_Settings.defaultFontAsset;
    }

    static GameObject CreateLabel(LabelPlacement label, Transform group, TMP_FontAsset font, Material material, Material mask, float size, Color color)
    {
        var go = new GameObject(label.text, typeof(RectTransform));
        go.transform.SetParent(group, false);

        go.transform.SetLocalPositionAndRotation(label.position, Quaternion.Euler(90f, 0f, 0f));
        AddLabelText(go, label.text, font, material, size);

        go.GetComponent<TextMeshPro>().color = color;
        go.SetActive(label.active);

        var letters = new GameObject("Letter Mask", typeof(RectTransform));
        letters.transform.SetParent(go.transform, false);
        AddLabelText(letters, label.text, font, mask, size);
        return go;
    }

    static void AddLabelText(GameObject go, string value, TMP_FontAsset font, Material material, float size)
    {
        var text = go.AddComponent<TextMeshPro>();
        text.font = font;
        text.fontSharedMaterial = material;
        text.richText = false;
        text.text = value;
        text.fontSize = size * 10f; // font size 10 = 1 metre
        text.alignment = TextAlignmentOptions.Bottom;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.rectTransform.pivot = new Vector2(0.5f, 0f);
        text.rectTransform.sizeDelta = text.GetPreferredValues();
    }

    static Mesh GetWireMesh(Placement placement)
    {
        string path = $"{WireFolder}/{placement.wireKey}.asset";
        Mesh wire = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (wire != null) return wire;

        var faceVertices = new List<Vector3>();
        var faceTriangles = new List<int>();
        foreach (WirePiece piece in placement.wires)
        {
            int start = faceVertices.Count;
            foreach (Vector3 vertex in piece.mesh.vertices) faceVertices.Add(piece.toRoom.MultiplyPoint3x4(vertex));
            foreach (int index in piece.mesh.triangles) faceTriangles.Add(start + index);
        }

        EnsureFolder(WireFolder);
        wire = MapLines.ToMesh(GetWireLines(placement), faceVertices, faceTriangles);
        AssetDatabase.CreateAsset(wire, path);
        return wire;
    }

    static void DeleteUnused(string folder, string filter)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return;

        var used = new HashSet<string>();
        foreach (string guid in AssetDatabase.FindAssets("_Map t:Prefab"))
        {
            used.UnionWith(AssetDatabase.GetDependencies(AssetDatabase.GUIDToAssetPath(guid), false));
        }

        foreach (string guid in AssetDatabase.FindAssets(filter, new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!used.Contains(path)) AssetDatabase.DeleteAsset(path);
        }
    }

    static Mesh GetBoxMesh()
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(BoxMeshPath);
        if (mesh != null && mesh.subMeshCount == 2) return mesh;

        Mesh cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        Vector3[] vertices = cube.vertices;
        int[] triangles = cube.triangles;
        var faces = new List<int>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 normal = Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]], vertices[triangles[i + 2]] - vertices[triangles[i]]);
            if (normal.normalized.y > 0.5f) continue;
            faces.Add(triangles[i]);
            faces.Add(triangles[i + 1]);
            faces.Add(triangles[i + 2]);
        }
        Mesh box = MapLines.ToMesh(MapLines.GetHardEdges(cube), new List<Vector3>(vertices), faces);

        // Updated in place so older maps keep their reference
        if (mesh == null)
        {
            EnsureFolder(GeneratedFolder);
            AssetDatabase.CreateAsset(box, BoxMeshPath);
            return box;
        }
        EditorUtility.CopySerialized(box, mesh);
        Object.DestroyImmediate(box);
        AssetDatabase.SaveAssetIfDirty(mesh);
        return mesh;
    }

    static Material[] GetMaterials(Placement placement)
    {
        return placement.kind == MapEntryKind.Door ? GetDoorMaterials(placement.doorState) : GetMaterials(placement.kind);
    }

    static Material[] GetDoorMaterials(MapDoorState state)
    {
        Color color = DoorColors[(int)state];

        Material lines = GetMaterial($"{DoorFolder}/MapDoorLines {state}.mat", SeeThroughLineShader, created => created.SetColor("_Color", color));
        return new[]
        {
            KeepQueue(KeepShader(lines, SeeThroughLineShader), RoomLinesQueue),
            GetFaceDepthMaterial(),
            GetTintMaterial($"{DoorFolder}/MapDoorFill {state}.mat", new Color(color.r, color.g, color.b, DoorFillAlphas[(int)state])),
        };
    }

    public static Color GetDoorColor(MapDoorState state)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>($"{DoorFolder}/MapDoorLines {state}.mat");
        if (material != null && material.HasProperty("_Color")) return material.GetColor("_Color");
        return DoorColors[(int)state];
    }

    static Material[] GetMaterials(MapEntryKind kind)
    {
        if (kind == MapEntryKind.Terminal)
        {
            return new[]
            {
                KeepFloat(GetMaterial(TerminalLineMaterialPath, LineShader, lines =>
                {
                    lines.SetColor("_Color", TerminalColor);
                    lines.renderQueue = InFrontOfRoomsQueue;
                }), DepthDimmedProperty, 0f),
                GetFaceDepthMaterial(),
                GetTerminalFillMaterial(),
            };
        }

        return new[]
        {
            GetMaterial(LineMaterialPath, LineShader, null),
            GetFaceDepthMaterial(),
            GetMaterial(FaceTintMaterialPath, FaceShader, null),
        };
    }

    public static Color GetLineColor(MapEntryKind kind)
    {
        bool terminal = kind == MapEntryKind.Terminal;
        Material material = AssetDatabase.LoadAssetAtPath<Material>(terminal ? TerminalLineMaterialPath : LineMaterialPath);
        if (material != null && material.HasProperty("_Color")) return material.GetColor("_Color");
        return terminal ? TerminalColor : RoomColor;
    }

    static Material GetFaceDepthMaterial()
    {
        return GetDepthOnlyMaterial(FaceDepthMaterialPath, FaceDepthQueue);
    }

    static Material GetDepthOnlyMaterial(string path, int queue)
    {
        return KeepQueue(GetMaterial(path, FaceShader, depthOnly =>
        {
            depthOnly.SetFloat("_ColorMask", 0f);
            depthOnly.SetFloat("_ZWrite", 1f);
        }), queue);
    }

    static Material GetTintMaterial(string path, Color color)
    {
        Material tint = GetMaterial(path, FaceShader, created => created.SetColor("_Color", color));
        bool changed = false;
        void Set(string property, float value)
        {
            if (tint.GetFloat(property) == value) return;
            tint.SetFloat(property, value);
            changed = true;
        }
        Set("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        Set("_StencilComp", (float)UnityEngine.Rendering.CompareFunction.Always);
        Set("_StencilPass", (float)UnityEngine.Rendering.StencilOp.Keep);
        Set("_StencilRef", 0f);
        if (changed)
        {
            EditorUtility.SetDirty(tint);
            AssetDatabase.SaveAssetIfDirty(tint);
        }
        return KeepQueue(tint, RoomTintQueue);
    }

    static Material GetTerminalFillMaterial()
    {
        return KeepFloat(GetFillMaterial(TerminalFaceTintMaterialPath, new Color(TerminalColor.r, TerminalColor.g, TerminalColor.b, TerminalTintAlpha), TerminalFillQueue), DepthDimmedProperty, 0f);
    }

    static Material GetFillMaterial(string path, Color color, int queue)
    {
        Material fill = GetMaterial(path, FaceShader, created => created.SetColor("_Color", color));

        bool changed = false;
        void Set(string property, float value)
        {
            if (fill.GetFloat(property) == value) return;
            fill.SetFloat(property, value);
            changed = true;
        }
        Set("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        Set("_StencilRef", FillStencilBit);
        Set("_StencilReadMask", FillStencilBit);
        Set("_StencilWriteMask", FillStencilBit);
        Set("_StencilComp", (float)UnityEngine.Rendering.CompareFunction.NotEqual);
        Set("_StencilPass", (float)UnityEngine.Rendering.StencilOp.Replace);
        if (changed)
        {
            EditorUtility.SetDirty(fill);
            AssetDatabase.SaveAssetIfDirty(fill);
        }
        return KeepQueue(fill, queue);
    }

    const string DepthDimmedProperty = "_DepthDimmed";

    static Material KeepFloat(Material material, string property, float value)
    {
        if (!material.HasProperty(property) || material.GetFloat(property) == value) return material;
        material.SetFloat(property, value);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        return material;
    }

    static Material KeepShader(Material material, string shaderName)
    {
        Shader shader = Shader.Find(shaderName);
        if (shader == null || material.shader == shader) return material;
        material.shader = shader;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        return material;
    }

    static Material KeepQueue(Material material, int queue)
    {
        if (material.renderQueue != queue)
        {
            material.renderQueue = queue;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
        }
        return material;
    }

    // Only recopied when the font or the copy changed
    static readonly Dictionary<string, (int font, int copy)> labelMaterialSources = new Dictionary<string, (int font, int copy)>();

    static Material GetLabelMaterial(TMP_FontAsset font, bool picked)
    {
        string path = picked ? $"{LabelFolder}/MapLabel {GetFontGuid(font)}.mat" : LabelMaterialPath;
        return GetFontCopy(path, font, text =>
        {
            if (!picked) text.SetColor(ShaderUtilities.ID_FaceColor, LabelColor);

            text.DisableKeyword("UNITY_UI_ALPHACLIP");
            text.SetFloat(ShaderUtilities.ID_StencilComp, (float)UnityEngine.Rendering.CompareFunction.Always);
            text.SetFloat(ShaderUtilities.ID_StencilOp, (float)UnityEngine.Rendering.StencilOp.Keep);
            text.SetFloat(ShaderUtilities.ID_StencilWriteMask, 0f);
            text.renderQueue = LabelQueue;
        });
    }

    static Material GetLabelMaskMaterial(TMP_FontAsset font, bool picked)
    {
        string path = picked ? $"{LabelFolder}/MapLabel {GetFontGuid(font)} Mask.mat" : LabelMaskMaterialPath;
        return GetFontCopy(path, font, mask =>
        {
            // Always TMP's own shader, since some font shaders can't draw invisibly
            mask.shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            mask.SetTexture(ShaderUtilities.ID_MainTex, font.atlasTexture);
            mask.SetFloat(ShaderUtilities.ID_TextureWidth, font.atlasWidth);
            mask.SetFloat(ShaderUtilities.ID_TextureHeight, font.atlasHeight);
            mask.SetFloat(ShaderUtilities.ID_GradientScale, font.atlasPadding + 1);
            mask.SetFloat(ShaderUtilities.ID_WeightNormal, font.normalStyle);
            mask.SetFloat(ShaderUtilities.ID_WeightBold, font.boldStyle);

            mask.DisableKeyword("GLOW_ON");
            mask.DisableKeyword("UNDERLAY_ON");
            mask.DisableKeyword("UNDERLAY_INNER");
            mask.EnableKeyword("UNITY_UI_ALPHACLIP");
            mask.SetFloat("_ColorMask", 0f);
            mask.SetFloat(ShaderUtilities.ID_StencilID, LabelStencilBit);
            mask.SetFloat(ShaderUtilities.ID_StencilComp, (float)UnityEngine.Rendering.CompareFunction.Always);
            mask.SetFloat(ShaderUtilities.ID_StencilOp, (float)UnityEngine.Rendering.StencilOp.Replace);
            mask.SetFloat(ShaderUtilities.ID_StencilWriteMask, LabelStencilBit);
            mask.SetFloat(ShaderUtilities.ID_StencilReadMask, LabelStencilBit);
            mask.renderQueue = LabelMaskQueue;
        });
    }

    // Named by GUID so renaming the font is safe
    static string GetFontGuid(TMP_FontAsset font)
    {
        return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(font));
    }

    static Material GetFontCopy(string path, TMP_FontAsset font, System.Action<Material> setUp)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        int fontState = font.material.ComputeCRC();
        if (material != null && labelMaterialSources.TryGetValue(path, out var known)
            && known.font == fontState && known.copy == material.ComputeCRC())
        {
            return material;
        }

        bool isNew = material == null;
        if (isNew)
        {
            material = new Material(font.material);
        }
        else
        {
            material.shader = font.material.shader;
            material.CopyPropertiesFromMaterial(font.material);
            material.shaderKeywords = font.material.shaderKeywords;
        }
        setUp(material);

        if (isNew)
        {
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
        }
        labelMaterialSources[path] = (fontState, material.ComputeCRC());
        return material;
    }

    public static bool GetLabelPosition(MapLevel.LabelEntry label, Transform levelRoot, out Vector3 world)
    {
        Transform from = label.anchor != null ? label.anchor.transform : levelRoot;
        world = from.position + from.rotation * label.offset;
        return label.anchor != null || !label.hasAnchor;
    }

    public static void SetLabelPosition(MapLevel.LabelEntry label, Transform levelRoot, Vector3 world)
    {
        // A deleted anchor still holds a reference
        if (label.anchor == null) label.anchor = null;

        Transform from = label.anchor != null ? label.anchor.transform : levelRoot;
        label.hasAnchor = label.anchor != null;
        label.offset = Quaternion.Inverse(from.rotation) * (world - from.position);
    }

    public static Vector3 GetLabelSpot(Transform target, Transform levelRoot)
    {
        Vector3 top = TryGetBounds(target, levelRoot, out Bounds bounds)
            ? levelRoot.TransformPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z))
            : target.position;
        return top + levelRoot.up * LabelGap;
    }

    public static string GetLabelText(string objectName)
    {
        string text = Regex.Replace(objectName, @"\s*\(\d+\)$", "").Replace('_', ' ');
        return ObjectNames.NicifyVariableName(text);
    }

    static void CollectLabels(MapLevel level, MapProxyLibrary library, ScanResult result)
    {
        // Keeps a hidden terminal's label hidden
        var terminals = new List<Placement>();
        foreach (Placement placement in result.placements)
        {
            if (placement.kind == MapEntryKind.Terminal && placement.source != null) terminals.Add(placement);
        }

        result.labelSize = level.labelSize;
        result.labelColor = library.labelColor;
        result.playerMarkerSize = level.playerMarkerSize;
        result.depthDimming = level.depthDimming;
        foreach (MapLevel.LabelEntry label in level.labels)
        {
            if (label == null || string.IsNullOrWhiteSpace(label.text)) continue;

            string text = label.text.Trim();
            if (!GetLabelPosition(label, level.transform, out Vector3 world))
            {
                result.lostAnchors.Add(text);
                continue;
            }

            Placement over = label.anchor != null ? terminals.Find(terminal => label.anchor.transform.IsChildOf(terminal.source)) : null;
            result.labels.Add(new LabelPlacement
            {
                text = text,
                anchor = label.anchor,
                position = level.transform.InverseTransformPoint(world),
                font = library.labelFont,
                active = over == null || over.active,
            });
        }
    }

    static Material GetMaterial(string path, string shaderName, System.Action<Material> setUp)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        Shader shader = Shader.Find(shaderName);
        if (shader == null) throw new System.InvalidOperationException($"Shader \"{shaderName}\" is missing. It should be in Assets/Shaders/HoloMap.");
        material = new Material(shader);
        setUp?.Invoke(material);
        EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
