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

/// <summary>
/// Finds the prefabs in a level and bakes the hologram map prefab from the Map Proxy Library.
/// The level's own prefabs are only read, never changed.
/// </summary>
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
    const string LabelFolder = GeneratedFolder + "/Labels";
    const string LineShader = "Entropy/HoloMap Lines";
    const string FaceShader = "Entropy/HoloMap Faces";

    // Raise MapVersion when the baked layout changes, so existing maps rebuild.
    // Raise WireVersion only when the saved line meshes change, since it renames every one of them
    const int MapVersion = 14;

    // Doors show their state: blue working, yellow broken, red locked
    static readonly Color[] DoorColors =
    {
        new Color(0.35f, 0.85f, 1f, 1f),
        new Color(1f, 0.82f, 0.15f, 1f),
        new Color(1f, 0.22f, 0.18f, 1f),
    };
    const float DoorFillAlpha = 0.4f;

    // Doors are drawn from their main parts. Anything smaller than this share of the door,
    // like grab bars and screens, is left out so it doesn't clutter the map
    const float DoorPartShare = 0.25f;
    const float DoorMergeDistance = 0.15f;

    // The grab bars on the game's doors are objects with this name in every door prefab
    const string DoorBarsName = "DoorBars";

    // Terminals are small, so their lines merge only when very close
    const float TerminalMergeDistance = 0.02f;
    const int WireVersion = 3;

    public static readonly Color RoomColor = new Color(0.35f, 0.85f, 1f, 1f);
    public static readonly Color TerminalColor = new Color(1f, 0.55f, 0.15f, 1f);
    static readonly Color LabelColor = new Color(0.85f, 0.95f, 1f, 1f);
    // Terminals are filled in strongly, so they stand out as objectives
    const float TerminalTintAlpha = 0.6f;

    // The map draws in this order, after ordinary see-through things like smoke so its invisible faces can't
    // cut holes in them. Terminal fill and label letter marks first, then label text over the fill, then
    // terminal lines, all before the faces record what's in front, so rooms never hide labels or terminals.
    // The room tint and room lines come after, from their shaders' queues (Transparent+100 and +101).
    // Lines skip the pixels label letters marked, so no line draws over text.
    // Terminal fill before door fill, so where they overlap the terminal keeps its color
    const int TerminalFillQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 95;
    const int DoorFillQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 96;
    const int LabelMaskQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 96;
    const int LabelQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 97;
    const int InFrontOfRoomsQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 98;
    const int FaceDepthQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 99;

    // Stencil values. URP leaves 1 to 8 for projects, but its stencil LOD cross-fade uses 4 and 8.
    // Label letters mark 1, which HoloMapLines.shader checks. Terminal and door fills mark 2 as they draw, so each pixel fills once
    const int LabelStencilBit = 1;
    const int FillStencilBit = 2;

    // How far above the thing it names a label sits, in metres
    public const float LabelGap = 0.5f;

    /// <summary>
    /// One copy of a picked mesh inside a room, stored relative to that room.
    /// </summary>
    public class WirePiece
    {
        public Mesh mesh;
        public Matrix4x4 toRoom;
    }

    /// <summary>
    /// One thing to place on the map, stored relative to the level root.
    /// </summary>
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

        // For terminals and doors: their number in their manager's list, and whether they're showing in the scene
        public int index = -1;
        public bool active = true;

        // For doors: the state they show
        public MapDoorState doorState;
    }

    /// <summary>
    /// One label to bake, stored relative to the level root.
    /// </summary>
    public class LabelPlacement
    {
        public string text;
        public Vector3 position;

        // A font asset picked in the library. Empty: TextMesh Pro's default font
        public TMP_FontAsset font;

        // A label over a terminal that's disabled in the scene starts hidden with it
        public bool active = true;
    }

    /// <summary>
    /// Everything found in one pass over the level.
    /// </summary>
    public class ScanResult
    {
        public List<Placement> placements = new List<Placement>();
        public List<LabelPlacement> labels = new List<LabelPlacement>();
        public float labelSize;
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

    /// <summary>
    /// Walks the level and works out what belongs on the map, without changing anything.
    /// </summary>
    public static ScanResult Collect(MapLevel level, MapProxyLibrary library)
    {
        var result = new ScanResult();

        // Objects added by hand go first, so they override whatever the walk would find
        foreach (MapLevel.SceneEntry entry in level.sceneEntries)
        {
            if (entry.target == null) continue;

            result.handAdded[entry.target] = entry.kind;
            if (HasShape(entry.kind))
            {
                // A door always draws its own lines, so it can show its state
                GameObject proxy = entry.kind == MapEntryKind.Door ? null : entry.proxy;
                AddShape(entry.target.transform, level.transform, entry.kind, proxy, entry.useMeshes ? entry : null, result);
            }
            else if (entry.kind == MapEntryKind.Icon)
            {
                result.placements.Add(CreatePlacement(entry.target.transform, level.transform, entry.kind, entry.proxy));
            }
        }

        // Terminals are built in: every terminal in the level goes on the map, with no setup.
        // A hand-added row for one still wins, and the walk leaves them alone like any marker
        foreach ((Terminal terminal, int index) in FindTerminals(level))
        {
            GameObject go = terminal.gameObject;
            if (result.handAdded.TryGetValue(go, out MapEntryKind addedKind))
            {
                // Added by hand as a terminal, such as to give it a proxy: it still keeps its number and hidden state
                Placement own = addedKind == MapEntryKind.Terminal ? result.placements.Find(p => p.source == terminal.transform) : null;
                if (own != null) SetTerminalState(own, terminal, index, result);
                continue;
            }

            result.handAdded[go] = MapEntryKind.Terminal;
            AddTerminal(terminal.transform, level.transform, index, result);
        }

        // Doors are built in the same way, colored by their state
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

        // Things kept outside the level root, like the terminals, are walked the same way
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

        // Objects added by hand outside the level still get what's inside them, as they would inside it
        foreach (MapLevel.SceneEntry entry in level.sceneEntries)
        {
            if (entry.target == null || !entry.target.activeInHierarchy) continue;

            Transform t = entry.target.transform;
            if (t.IsChildOf(level.transform) || level.transform.IsChildOf(t)) continue;
            if (walked.Any(root => t.IsChildOf(root.transform))) continue;
            Walk(t, t, level.transform, false, library, result);
        }

        CollectLabels(level, library, result);

        // The map display will match terminals to the real ones by name, so names have to be unique
        var terminalNames = new HashSet<string>();
        foreach (Placement placement in result.placements)
        {
            if (placement.kind != MapEntryKind.Terminal || terminalNames.Add(placement.name)) continue;
            if (!result.duplicateTerminals.Contains(placement.name)) result.duplicateTerminals.Add(placement.name);
        }
        return result;
    }

    /// <summary>
    /// Every terminal in the level's scene, found by the game's Terminal script. They're numbered by the
    /// TerminalManager's list, the same numbering the terminals' save data uses. Any not in that list come last, as -1.
    /// </summary>
    public static List<(Terminal terminal, int index)> FindTerminals(MapLevel level)
    {
        var found = new List<(Terminal terminal, int index)>();
        GameObject[] roots = level.gameObject.scene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            foreach (TerminalManager manager in root.GetComponentsInChildren<TerminalManager>(true))
            {
                // The list is private to the game's code, so it's read the way the Inspector reads it
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

    /// <summary>
    /// A terminal is drawn from all of its own meshes, so every kind of terminal looks like itself.
    /// </summary>
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

    /// <summary>
    /// Every door in the level's scene, found by the game's DoorScript. They're numbered the way the
    /// DoorManager numbers them, which its save data uses too. Any it doesn't count come last, as -1.
    /// </summary>
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

    /// <summary>
    /// A door's frame is drawn like the rest of the map, and only its moving panel (the DoorScript's doorPart)
    /// is colored by the door's state. A door with no panel set is colored as a whole.
    /// </summary>
    static void AddDoor(DoorScript door, Transform levelRoot, int index, ScanResult result)
    {
        Transform t = door.transform;
        Transform panel = new SerializedObject(door).FindProperty("doorPart")?.objectReferenceValue as Transform;
        if (panel != null && !panel.IsChildOf(t)) panel = null;

        var frameShape = new MapShape { useMeshes = true, mergeDistance = DoorMergeDistance };
        var panelShape = new MapShape { useMeshes = true, mergeDistance = DoorMergeDistance };
        foreach (MeshFilter filter in GetDoorParts(door))
        {
            MapShape shape = panel != null && filter.transform.IsChildOf(panel) ? panelShape : frameShape;
            if (!shape.meshes.Contains(filter.sharedMesh)) shape.meshes.Add(filter.sharedMesh);
        }

        if (panel == null || panelShape.meshes.Count == 0)
        {
            Placement whole = AddShape(t, levelRoot, MapEntryKind.Door, null, frameShape, result);
            if (whole != null) SetDoorState(whole, door, index, result);
            return;
        }

        if (frameShape.meshes.Count > 0)
        {
            Placement frame = AddShape(t, levelRoot, MapEntryKind.Room, null, frameShape, result, panel);
            if (frame != null) frame.active = t.gameObject.activeInHierarchy;
        }
        Placement panelPiece = AddShape(panel, levelRoot, MapEntryKind.Door, null, panelShape, result);
        if (panelPiece == null) return;
        panelPiece.name = t.name;
        SetDoorState(panelPiece, door, index, result);
    }

    /// <summary>
    /// A door's main parts: its frame and panel. The holograms (the DoorScript's hologramGroup) and the
    /// grab bars are left out, along with anything much smaller than the door.
    /// </summary>
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

    /// <summary>
    /// An Also Scan object has to sit beside the level root, not inside it, around it, or inside another
    /// Also Scan object, so nothing is walked twice.
    /// </summary>
    public static bool CanAlsoScan(GameObject go, MapLevel level)
    {
        if (go == null || go.scene != level.gameObject.scene) return false;

        Transform t = go.transform;
        if (t.IsChildOf(level.transform) || level.transform.IsChildOf(t)) return false;
        return !level.alsoScan.Exists(other => other != null && other != go && t.IsChildOf(other.transform));
    }

    /// <summary>
    /// Shapes are drawn with lines and faces. Markers stand for one spot: they show even inside
    /// a room, and nothing under them is looked at.
    /// </summary>
    public static bool HasShape(MapEntryKind kind) => kind == MapEntryKind.Room || kind == MapEntryKind.Terminal || kind == MapEntryKind.Door;
    public static bool IsMarker(MapEntryKind kind) => kind == MapEntryKind.Icon || kind == MapEntryKind.Terminal || kind == MapEntryKind.Door;

    /// <summary>
    /// Adds every prefab the level uses to the library. Returns how many were new.
    /// </summary>
    public static int Scan(MapLevel level, MapProxyLibrary library)
    {
        List<GameObject> unlisted = Collect(level, library).unlisted;

        Undo.RecordObject(library, "Scan Level");
        int added = 0;
        foreach (GameObject prefab in unlisted)
        {
            // A variant shares its base prefab's entry, so only the base needs listing
            if (HasBaseIn(prefab, unlisted)) continue;

            library.entries.Add(new MapProxyLibrary.Entry { levelPrefab = prefab, kind = GuessKind(prefab) });
            added++;
        }

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssetIfDirty(library);
        return added;
    }

    /// <summary>
    /// Bakes the level's map prefab. Skips the save when nothing has changed, so saving a scene
    /// doesn't rewrite the prefab and fill up everyone's git changes. Returns true if it saved.
    /// </summary>
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

            // A save rebuilds with the window closed, so lost labels are mentioned here too
            foreach (string label in scan.lostAnchors)
            {
                Debug.LogWarning($"Map label \"{label}\" lost the object it sat over, so it was left off the map. Fix it in the Map Builder's Labels list.");
            }
        }
        else
        {
            // A font's glow or colors can change with nothing else changing. The labels' materials are
            // separate files from the map, so they're brought up to date without rebuilding it
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

    /// <param name="top">The walked root's child this branch started from, named in reports</param>
    static void Walk(Transform t, Transform top, Transform levelRoot, bool inRoom, MapProxyLibrary library, ScanResult result)
    {
        GameObject go = t.gameObject;
        if (result.handAdded.TryGetValue(go, out MapEntryKind handAddedKind))
        {
            // Already handled by Collect. Markers and disabled objects have nothing more to find underneath
            if (IsMarker(handAddedKind) || !go.activeSelf) return;

            // Its own row replaces the prefab's, but the children ticked on the prefab still count
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
                // Prefabs nested inside a room are part of that room, so only standalone ones get listed
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
                // A ticked Look Inside child may already be on the map as a room
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

                // Ignore only turns off this prefab's own piece, so the walk carries on inside it
                inRoom = true;
            }
        }
        else if (!inRoom && go.TryGetComponent<Renderer>(out _))
        {
            // Report the top-level object once instead of every piece under it
            if (!result.notPrefabs.Contains(top.name)) result.notPrefabs.Add(top.name);
        }

        foreach (Transform child in t)
        {
            Walk(child, top, levelRoot, inRoom, library, result);
        }
    }

    /// <summary>
    /// Places the children ticked under Look Inside. A child with no shape of its own
    /// uses its prefab's proxy or meshes if that prefab is in the library.
    /// </summary>
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

                // Placed first here, so a terminal would otherwise end up as a room
                if (listed.kind == MapEntryKind.Terminal || listed.kind == MapEntryKind.Door) kind = listed.kind;
                if (kind == MapEntryKind.Door) proxy = null;
            }
            AddShape(child, levelRoot, kind, proxy, meshSource, result);
        }
    }

    /// <summary>
    /// Places a room or terminal drawn by its proxy, its picked meshes, or a box around it.
    /// </summary>
    /// <param name="skip">Something inside that's placed on its own, like a door's panel, so its meshes are left out here</param>
    static Placement AddShape(Transform room, Transform levelRoot, MapEntryKind kind, GameObject proxy, MapShape meshSource, ScanResult result, Transform skip = null)
    {
        // The same object can be reached more than once, such as a hand-added prefab ticked under Look Inside
        if (!result.placed.Add(room)) return null;

        Placement placement = CreatePlacement(room, levelRoot, kind, proxy);
        if (proxy == null && meshSource != null)
        {
            placement.wires = FindWires(room, meshSource.meshes, skip);
            placement.worldScale = room.lossyScale;
            placement.mergeDistance = meshSource.mergeDistance;
            placement.wireKey = GetWireKey(placement, result);

            // Such as a prefab variant that swapped out its base's meshes
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

    /// <summary>
    /// Finds every copy of the picked meshes inside a room, so they land where they really are.
    /// </summary>
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

    /// <summary>
    /// Identifies a room's lines by everything that goes into them, so identical rooms
    /// share one saved line mesh and a change to any input makes a new one.
    /// </summary>
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

    /// <summary>
    /// Hashes the geometry itself, so the same model gives the same key on everyone's machine,
    /// and meshes that aren't assets, like ProBuilder's, still get told apart.
    /// </summary>
    static string GetMeshHash(Mesh mesh, ScanResult result)
    {
        if (!result.meshHashes.TryGetValue(mesh, out string hash))
        {
            hash = $"{Hash128.Compute(mesh.vertices)},{Hash128.Compute(mesh.triangles)}";
            result.meshHashes[mesh] = hash;
        }
        return hash;
    }

    // Rounded to whole steps, so float noise in a rotated copy of a room doesn't make it look different
    static int GetKeyText(float value)
    {
        return Mathf.RoundToInt(value * 1000f);
    }

    /// <summary>
    /// Builds a room's lines from all its picked meshes together, so lines from different
    /// meshes merge too. Merging happens at real size so the distance is in metres.
    /// </summary>
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

    /// <summary>
    /// Follows an object back through the prefabs it came from, nearest first. Going through the whole
    /// chain is what lets nested prefabs and variants match, including a variant using its base's entry.
    /// </summary>
    static IEnumerable<GameObject> GetSources(GameObject go)
    {
        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(go);
        while (source != null)
        {
            yield return source;
            source = PrefabUtility.GetCorrespondingObjectFromSource(source);
        }
    }

    /// <summary>
    /// The library row an object or prefab falls under now, through the prefabs it came from. Null if none.
    /// </summary>
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
        // Rooms are never rigged, so a skinned mesh means a character
        return prefab.GetComponentInChildren<SkinnedMeshRenderer>(true) != null
            ? MapEntryKind.Ignore
            : MapEntryKind.Room;
    }

    /// <summary>
    /// Measures a room in its own space, so a rotated room still gets a tight box.
    /// </summary>
    static Bounds GetLocalBounds(Transform room)
    {
        TryGetBounds(room, room, out Bounds bounds);
        return bounds;
    }

    /// <summary>
    /// Measures everything drawn under the target, in the given object's space.
    /// </summary>
    static bool TryGetBounds(Transform target, Transform space, out Bounds bounds)
    {
        bounds = new Bounds();
        bool hasBounds = false;

        // A disabled object was added by hand, so it still needs measuring
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
            text.Append($",{placement.wireKey},{placement.index},{placement.active},{placement.doorState}");
        }

        // TextMesh Pro is only touched when there are labels, since reading its settings can open its import window
        if (scan.labels.Count > 0)
        {
            text.Append($"|L,{GetKeyText(scan.labelSize)}");
            foreach (LabelPlacement label in scan.labels)
            {
                // The length in front stops a label containing '|' or ',' from looking like another one
                string font = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(GetLabelFont(label)));
                text.Append($"|{label.text.Length}:{label.text},{label.position:F3},{font},{(label.font != null ? "picked" : "default")},{label.active}");
            }
        }
        return Hash128.Compute(text.ToString()).ToString();
    }

    /// <summary>
    /// Assembles the map in a preview scene, so baking never touches or dirties the open level.
    /// </summary>
    static void SaveMap(string mapPath, string levelName, ScanResult scan)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = new GameObject($"{levelName}_Map");
            SceneManager.MoveGameObjectToScene(root, scene);

            // Every group is always there, so the map display can rely on finding them
            Transform rooms = AddGroup(MapLevel.RoomsGroup, root.transform);
            Transform icons = AddGroup(MapLevel.IconsGroup, root.transform);
            Transform terminals = AddGroup(MapLevel.TerminalsGroup, root.transform);
            Transform doors = AddGroup(MapLevel.DoorsGroup, root.transform);
            Transform labels = AddGroup(MapLevel.LabelsGroup, root.transform);
            labels.gameObject.AddComponent<MapFaceCamera>();

            // Keeps doors colored by their real state while the game runs
            var doorColors = doors.gameObject.AddComponent<MapDoors>();
            doorColors.working = GetDoorMaterials(MapDoorState.Working);
            doorColors.broken = GetDoorMaterials(MapDoorState.Broken);
            doorColors.locked = GetDoorMaterials(MapDoorState.Locked);

            foreach (Placement placement in scan.placements)
            {
                Transform group = placement.kind == MapEntryKind.Terminal ? terminals
                    : placement.kind == MapEntryKind.Door ? doors
                    : placement.kind == MapEntryKind.Icon ? icons
                    : rooms;
                CreateNode(placement, group);
            }
            if (scan.labels.Count > 0)
            {
                AddLabels(scan, labels);
            }
            PrefabUtility.SaveAsPrefabAsset(root, mapPath);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    static Transform AddGroup(string name, Transform parent)
    {
        var group = new GameObject(name).transform;
        group.SetParent(parent, false);
        return group;
    }

    static void CreateNode(Placement placement, Transform parent)
    {
        GameObject node;
        if (placement.proxy != null)
        {
            // Kept as a linked prefab, so editing a proxy updates every map that uses it
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
            // Links the piece to its terminal for showing its state later. A terminal that's
            // disabled in the scene starts hidden, ready to be shown when the real one is
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
            // The door's pieces change color with its state. One that's disabled in the scene starts hidden
            var door = node.AddComponent<MapDoor>();
            door.index = placement.index;
            door.state = placement.doorState;
            door.pieces = lines != null ? new[] { lines.GetComponent<MeshRenderer>() } : new MeshRenderer[0];
        }

        // A terminal or door that's disabled in the scene starts hidden, ready to be shown when the real one is
        node.SetActive(placement.active);
    }

    static Transform AddLines(string name, Transform parent, Mesh lines, Material[] materials)
    {
        var piece = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        piece.transform.SetParent(parent, false);
        piece.GetComponent<MeshFilter>().sharedMesh = lines;

        // Three materials on a two-part mesh: Unity draws the last part (the faces) once per extra
        // material, so the faces get both their depth-only pass and their tint
        piece.GetComponent<MeshRenderer>().sharedMaterials = materials;
        return piece.transform;
    }

    static void AddLabels(ScanResult scan, Transform group)
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
            CreateLabel(label, group, font, GetLabelMaterial(font, picked), GetLabelMaskMaterial(font, picked), scan.labelSize);
        }
    }

    static TMP_FontAsset GetLabelFont(LabelPlacement label)
    {
        return label.font != null ? label.font : GetDefaultLabelFont();
    }

    /// <summary>
    /// TextMesh Pro's own default font. It's looked up by name, because a project can have more than one
    /// TMP Settings file (render pipeline samples bring their own), and the one TextMesh Pro picks may
    /// give a font whose shader can't hide the letter copy or keep lines off the text.
    /// </summary>
    static TMP_FontAsset GetDefaultLabelFont()
    {
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        return font != null ? font : TMP_Settings.defaultFontAsset;
    }

    static void CreateLabel(LabelPlacement label, Transform group, TMP_FontAsset font, Material material, Material mask, float size)
    {
        var go = new GameObject(label.text, typeof(RectTransform));
        go.transform.SetParent(group, false);

        // Lying flat so it reads from above until MapFaceCamera turns it toward a camera
        go.transform.SetLocalPositionAndRotation(label.position, Quaternion.Euler(90f, 0f, 0f));
        AddLabelText(go, label.text, font, material, size);
        go.SetActive(label.active);

        // The invisible copy that keeps map lines off the letters, moving with the label
        var letters = new GameObject("Letter Mask", typeof(RectTransform));
        letters.transform.SetParent(go.transform, false);
        AddLabelText(letters, label.text, font, mask, size);
    }

    static void AddLabelText(GameObject go, string value, TMP_FontAsset font, Material material, float size)
    {
        var text = go.AddComponent<TextMeshPro>();
        text.font = font;
        text.fontSharedMaterial = material;
        text.richText = false;
        text.text = value;
        text.fontSize = size * 10f; // TextMesh Pro's 3D text draws font size 10 as one metre
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

    /// <summary>
    /// Clears out generated files no map uses any more, such as line meshes left by trying other merge
    /// distances, or label materials for a font no longer picked.
    /// </summary>
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

    /// <summary>
    /// A wireframe cube shared by every room that has no proxy yet. Its faces leave out the top,
    /// so looking down into a box isn't blocked by a ceiling.
    /// </summary>
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

        // Older maps saved a lines-only box. Updating it in place keeps their reference to it
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

    /// <summary>
    /// Lines, then faces drawn depth-only, then faces tinted. The draw order comes from each material's queue.
    /// The materials are assets, so their colors and settings can be changed in the Inspector.
    /// </summary>
    static Material[] GetMaterials(Placement placement)
    {
        return placement.kind == MapEntryKind.Door ? GetDoorMaterials(placement.doorState) : GetMaterials(placement.kind);
    }

    /// <summary>
    /// A door's lines and fill in its state's color. Like terminals, doors draw in front of rooms,
    /// so a locked or broken one shows down a hall.
    /// </summary>
    static Material[] GetDoorMaterials(MapDoorState state)
    {
        Color color = DoorColors[(int)state];
        return new[]
        {
            KeepQueue(GetMaterial($"{DoorFolder}/MapDoorLines {state}.mat", LineShader, lines => lines.SetColor("_Color", color)), InFrontOfRoomsQueue),
            GetFaceDepthMaterial(),
            GetFillMaterial($"{DoorFolder}/MapDoorFill {state}.mat", new Color(color.r, color.g, color.b, DoorFillAlpha), DoorFillQueue),
        };
    }

    /// <summary>
    /// The line color for a door's state, read from its material when it exists, so the overlay matches the map.
    /// </summary>
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
                GetMaterial(TerminalLineMaterialPath, LineShader, lines =>
                {
                    lines.SetColor("_Color", TerminalColor);
                    lines.renderQueue = InFrontOfRoomsQueue;
                }),
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

    /// <summary>
    /// The line color for a type, read from its material when it exists, so the overlay matches the map.
    /// </summary>
    public static Color GetLineColor(MapEntryKind kind)
    {
        bool terminal = kind == MapEntryKind.Terminal;
        Material material = AssetDatabase.LoadAssetAtPath<Material>(terminal ? TerminalLineMaterialPath : LineMaterialPath);
        if (material != null && material.HasProperty("_Color")) return material.GetColor("_Color");
        return terminal ? TerminalColor : RoomColor;
    }

    // Shared by every color, since it draws nothing you can see
    static Material GetFaceDepthMaterial()
    {
        return GetDepthOnlyMaterial(FaceDepthMaterialPath, FaceDepthQueue);
    }

    /// <summary>
    /// An invisible material that only records what's in front. Its draw order is part of how the map works,
    /// so it's set every time, which also updates materials made by older versions.
    /// </summary>
    static Material GetDepthOnlyMaterial(string path, int queue)
    {
        return KeepQueue(GetMaterial(path, FaceShader, depthOnly =>
        {
            depthOnly.SetFloat("_ColorMask", 0f);
            depthOnly.SetFloat("_ZWrite", 1f);
        }), queue);
    }

    static Material GetTerminalFillMaterial()
    {
        return GetFillMaterial(TerminalFaceTintMaterialPath, new Color(TerminalColor.r, TerminalColor.g, TerminalColor.b, TerminalTintAlpha), TerminalFillQueue);
    }

    /// <summary>
    /// The solid fill that makes terminals and doors stand out. It draws before the rooms record what's in front,
    /// so it shows through walls, and marks each pixel it fills, so overlapping parts and both sides of a
    /// piece fill it once, evenly. How it draws is set every time, which also updates older materials.
    /// </summary>
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

    // For materials whose draw order is part of how the map works. Set every time, which also updates older materials
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

    // Each label material is a copy of its font's material. Remembered here is what each was copied from and
    // what it looked like after, so it's copied again only when the font or the copy has changed since
    static readonly Dictionary<string, (int font, int copy)> labelMaterialSources = new Dictionary<string, (int font, int copy)>();

    /// <summary>
    /// The material a label's text draws with. A font picked in the library keeps its own look, glow included,
    /// so the map matches the rest of the game. TextMesh Pro's default font gets the map's own color.
    /// </summary>
    static Material GetLabelMaterial(TMP_FontAsset font, bool picked)
    {
        string path = picked ? $"{LabelFolder}/MapLabel {GetFontGuid(font)}.mat" : LabelMaterialPath;
        return GetFontCopy(path, font, text =>
        {
            if (!picked) text.SetColor(ShaderUtilities.ID_FaceColor, LabelColor);

            // The text itself marks nothing, so its glow isn't clipped off
            text.DisableKeyword("UNITY_UI_ALPHACLIP");
            text.SetFloat(ShaderUtilities.ID_StencilComp, (float)UnityEngine.Rendering.CompareFunction.Always);
            text.SetFloat(ShaderUtilities.ID_StencilOp, (float)UnityEngine.Rendering.StencilOp.Keep);
            text.SetFloat(ShaderUtilities.ID_StencilWriteMask, 0f);
            text.renderQueue = LabelQueue;
        });
    }

    /// <summary>
    /// An invisible copy of a label's letters, drawn just before the text. It marks the pixels the letters cover,
    /// so map lines skip them and the text stays readable with the map still showing around it.
    /// </summary>
    static Material GetLabelMaskMaterial(TMP_FontAsset font, bool picked)
    {
        string path = picked ? $"{LabelFolder}/MapLabel {GetFontGuid(font)} Mask.mat" : LabelMaskMaterialPath;
        return GetFontCopy(path, font, mask =>
        {
            // Always TextMesh Pro's own text shader, whatever the font's material uses, since a font
            // can come with a shader that can't draw invisibly or mark the stencil, and would show as
            // a second copy of the text. Its atlas values are set the way TextMesh Pro sets them for a font
            mask.shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            mask.SetTexture(ShaderUtilities.ID_MainTex, font.atlasTexture);
            mask.SetFloat(ShaderUtilities.ID_TextureWidth, font.atlasWidth);
            mask.SetFloat(ShaderUtilities.ID_TextureHeight, font.atlasHeight);
            mask.SetFloat(ShaderUtilities.ID_GradientScale, font.atlasPadding + 1);
            mask.SetFloat(ShaderUtilities.ID_WeightNormal, font.normalStyle);
            mask.SetFloat(ShaderUtilities.ID_WeightBold, font.boldStyle);

            // Just the letters and their outline, without glow or shadow, and only where they really are
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

    // Named by the font's GUID, so renaming the font doesn't leave the map pointing at an old copy
    static string GetFontGuid(TMP_FontAsset font)
    {
        return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(font));
    }

    /// <summary>
    /// Copies a font's material and sets it up for the map. The font's own material is left alone,
    /// since other text in the game uses it.
    /// </summary>
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

    /// <summary>
    /// Where a label is now. False when the object it was placed over has been deleted.
    /// </summary>
    public static bool GetLabelPosition(MapLevel.LabelEntry label, Transform levelRoot, out Vector3 world)
    {
        Transform from = label.anchor != null ? label.anchor.transform : levelRoot;
        world = from.position + from.rotation * label.offset;
        return label.anchor != null || !label.hasAnchor;
    }

    /// <summary>
    /// Puts a label at a spot, kept relative to its anchor so it follows the anchor around.
    /// </summary>
    public static void SetLabelPosition(MapLevel.LabelEntry label, Transform levelRoot, Vector3 world)
    {
        // A deleted anchor still holds a reference, so clear it properly
        if (label.anchor == null) label.anchor = null;

        Transform from = label.anchor != null ? label.anchor.transform : levelRoot;
        label.hasAnchor = label.anchor != null;
        label.offset = Quaternion.Inverse(from.rotation) * (world - from.position);
    }

    /// <summary>
    /// Just above the middle of the top of everything drawn under the target.
    /// </summary>
    public static Vector3 GetLabelSpot(Transform target, Transform levelRoot)
    {
        Vector3 top = TryGetBounds(target, levelRoot, out Bounds bounds)
            ? levelRoot.TransformPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z))
            : target.position;
        return top + levelRoot.up * LabelGap;
    }

    /// <summary>
    /// A readable default label for an object, like "Terminal Medbay" for "TerminalMedbay (1)".
    /// </summary>
    public static string GetLabelText(string objectName)
    {
        string text = Regex.Replace(objectName, @"\s*\(\d+\)$", "").Replace('_', ' ');
        return ObjectNames.NicifyVariableName(text);
    }

    static void CollectLabels(MapLevel level, MapProxyLibrary library, ScanResult result)
    {
        // Terminals don't get labels from the Place Labels tool, but one set as a label's anchor by hand
        // starts hidden with its terminal, so it doesn't give away where a hidden terminal is
        var terminals = new List<Placement>();
        foreach (Placement placement in result.placements)
        {
            if (placement.kind == MapEntryKind.Terminal && placement.source != null) terminals.Add(placement);
        }

        result.labelSize = level.labelSize;
        foreach (MapLevel.LabelEntry label in level.labels)
        {
            if (label == null || string.IsNullOrWhiteSpace(label.text)) continue;

            string text = label.text.Trim();
            if (!GetLabelPosition(label, level.transform, out Vector3 world))
            {
                result.lostAnchors.Add(text);
                continue;
            }

            // Anchored to the terminal or any part of it
            Placement over = label.anchor != null ? terminals.Find(terminal => label.anchor.transform.IsChildOf(terminal.source)) : null;
            result.labels.Add(new LabelPlacement
            {
                text = text,
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
