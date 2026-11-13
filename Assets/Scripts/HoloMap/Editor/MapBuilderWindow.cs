using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The Map Builder window: scan a level, pair its prefabs with proxies, and bake the map.
/// Rows read and write the data directly, because this redraws on every event and
/// SerializedProperty is far too slow for lists this long. Labels are in MapBuilderWindow.Labels.cs.
/// </summary>
public partial class MapBuilderWindow : EditorWindow
{
    const float ListIndent = 18f;
    const float CheckboxWidth = 88f;
    const float MaxMergeDistance = 0.5f;
    const string AddHelp = "From the Project window: covers every copy of that prefab.\n"
        + "From the Hierarchy: covers just that object, prefab or not, and overrides the scan.";

    const float OverlayAlpha = 0.9f;
    const float HiddenOverlayAlpha = 0.3f;
    const string TerminalsHelp = "Everything with the game's Terminal script goes on the map in orange, drawn from its own meshes. "
        + "The number is its place in the TerminalManager's list, which the map can use to show each terminal's state.";
    const string DoorsHelp = "Everything with the game's DoorScript goes on the map, drawn from its main parts and colored by its state: "
        + "blue working, yellow broken, red locked. While the game runs the map follows each door's state. "
        + "The number is its place in the DoorManager's list.";
    static readonly GUIContent AddScanLabel = new GUIContent("Add Object to Scan", "An object beside the level root whose children belong on the map, such as rooms kept outside it. Terminals don't need this: they're found anywhere in the scene.");
    static readonly GUIContent LookInsideLabel = new GUIContent("Look Inside", "List this prefab's children so you can put them on the map one by one.");
    static readonly GUIContent UseMeshesLabel = new GUIContent("Use Meshes", "Pick which meshes draw this, instead of a box. Terminals start with all their meshes ticked.");
    static readonly GUIContent MergeLabel = new GUIContent("Merge (m)", "Lines closer together than this merge into one. Raise it to clear up dense areas, lower it if curves get lumpy. 0 is off.");

    /// <summary>
    /// A mesh in a pick list, with its label worked out once instead of on every redraw.
    /// </summary>
    private class MeshRow
    {
        public Mesh mesh;
        public string label;
    }

    private MapProxyLibrary library;
    private MapLevel level;
    private bool levelIsStale = true;
    private List<(Terminal terminal, int index)> foundTerminals = new List<(Terminal terminal, int index)>();
    private List<(DoorScript door, int index)> foundDoors = new List<(DoorScript door, int index)>();
    private Vector2 scroll;
    private readonly List<string> report = new List<string>();
    private readonly HashSet<object> openMeshLists = new HashSet<object>();
    private readonly HashSet<object> openInsideLists = new HashSet<object>();
    private readonly Dictionary<GameObject, List<MeshRow>> meshLists = new Dictionary<GameObject, List<MeshRow>>();

    private bool showOverlay;
    private bool overlayIsStale = true;
    private MapLevel overlayLevel;
    private List<MapBuilder.Placement> overlayPlacements = new List<MapBuilder.Placement>();
    private readonly HashSet<Transform> pieces = new HashSet<Transform>();
    private readonly HashSet<Transform> markerParts = new HashSet<Transform>();
    private Color roomOverlayColor, terminalOverlayColor;
    private readonly Color[] doorOverlayColors = new Color[3];
    private Material overlayMaterial;
    private readonly Dictionary<string, Mesh> overlayLines = new Dictionary<string, Mesh>();

    [MenuItem("Tools/Hologram Map Builder")]
    public static void ShowWindow()
    {
        GetWindow<MapBuilderWindow>("Map Builder");
    }

    private void OnEnable()
    {
        overlayIsStale = true;
        levelIsStale = true;
        SceneView.duringSceneGui += DrawOverlay;
        SceneView.duringSceneGui += OnLabelsSceneGUI;
        EditorApplication.hierarchyChanged += OnLevelChanged;
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable()
    {
        SetPlacing(false);
        SceneView.duringSceneGui -= DrawOverlay;
        SceneView.duringSceneGui -= OnLabelsSceneGUI;
        EditorApplication.hierarchyChanged -= OnLevelChanged;
        Undo.undoRedoPerformed -= OnUndoRedo;
        SaveLibrary();

        if (overlayMaterial != null) DestroyImmediate(overlayMaterial);
        foreach (Mesh lines in overlayLines.Values) DestroyImmediate(lines);
        overlayLines.Clear();
    }

    private void OnLostFocus()
    {
        SaveLibrary();
    }

    // Edits made elsewhere, like reordering the TerminalManager's list, show when coming back to the window
    private void OnFocus()
    {
        levelIsStale = true;
    }

    private void SaveLibrary()
    {
        if (library != null) AssetDatabase.SaveAssetIfDirty(library);
    }

    private void OnLevelChanged()
    {
        levelIsStale = true;
        overlayIsStale = true;
        meshLists.Clear();
        Repaint();
    }

    // The map and its labels show in the Scene view while either of these is on
    private bool DrawsInScene => showOverlay || placingLabels;

    private void OnUndoRedo()
    {
        // Undo can remove the selected label
        selectedLabel = -1;
        OnLevelChanged();
        Repaint();
        if (DrawsInScene) SceneView.RepaintAll();
    }

    private void MarkChanged(Object owner)
    {
        EditorUtility.SetDirty(owner);
        overlayIsStale = true;
        if (DrawsInScene) SceneView.RepaintAll();
    }

    private void OnGUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SetPlacing(false);
            EditorGUILayout.HelpBox("The Map Builder is off in Play mode, so a bake can't capture a level mid-game.", MessageType.Info);
            return;
        }

        // Right after pressing Place Labels the window still has focus, so Esc has to work here too
        if (placingLabels && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            SetPlacing(false);
            Event.current.Use();
        }

        if (library == null)
        {
            library = MapBuilder.GetOrCreateLibrary();
        }

        // Layout only, because the rows have to stay the same between Layout and Repaint
        if (Event.current.type == EventType.Layout && (levelIsStale || level == null))
        {
            level = MapBuilder.FindLevel(EditorSceneManager.GetActiveScene());
            foundTerminals = level != null ? MapBuilder.FindTerminals(level) : new List<(Terminal terminal, int index)>();
            foundDoors = level != null ? MapBuilder.FindDoors(level) : new List<(DoorScript door, int index)>();
            levelIsStale = false;
        }

        DrawLevelSection();
        EditorGUILayout.Space();
        DrawEntriesSection();
        DrawAddSection();
        DrawReport();
    }

    private void DrawLevelSection()
    {
        GUILayout.Label("Level", EditorStyles.boldLabel);

        if (level == null)
        {
            EditorGUILayout.HelpBox("This scene has no MapLevel. Select the level's root object and add one.", MessageType.Info);

            GameObject selected = Selection.activeGameObject;
            using (new EditorGUI.DisabledScope(selected == null || !selected.scene.IsValid()))
            {
                if (GUILayout.Button("Add MapLevel to Selected Object"))
                {
                    Undo.AddComponent<MapLevel>(selected);
                }
            }
            return;
        }

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Level Root", level, typeof(MapLevel), true);
            EditorGUILayout.ObjectField("Map Prefab", level.mapPrefab, typeof(GameObject), false);
        }
        DrawAlsoScan();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Scan Level"))
            {
                ScanLevel();
            }
            if (GUILayout.Button("Build Map"))
            {
                BuildMap();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            bool overlay = GUILayout.Toggle(showOverlay, "Show Map Overlay", "Button");
            if (overlay != showOverlay)
            {
                showOverlay = overlay;
                overlayIsStale = true;
                SceneView.RepaintAll();
            }

            bool placing = GUILayout.Toggle(placingLabels, "Place Labels", "Button");
            if (placing != placingLabels)
            {
                SetPlacing(placing);
            }
        }

        if (placingLabels)
        {
            EditorGUILayout.HelpBox("Click a room in the Scene view to label it, or anything else to label that spot. Terminals don't get labels. Shift+click: label the exact spot. Esc: done, then click a label to move it with its arrows.", MessageType.Info);
        }
    }

    /// <summary>
    /// Objects beside the level root whose children are scanned too.
    /// </summary>
    private void DrawAlsoScan()
    {
        for (int i = 0; i < level.alsoScan.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("Also Scan", level.alsoScan[i], typeof(GameObject), true);
                }
                if (GUILayout.Button("X", GUILayout.Width(22)))
                {
                    Undo.RecordObject(level, "Remove Object to Scan");
                    level.alsoScan.RemoveAt(i);
                    MarkChanged(level);
                    break;
                }
            }
        }

        var added = (GameObject)EditorGUILayout.ObjectField(AddScanLabel, null, typeof(GameObject), true);
        if (added != null)
        {
            AddAlsoScan(added);
        }
    }

    private void AddAlsoScan(GameObject go)
    {
        report.Clear();
        GameObject holder = level.alsoScan.Find(other => other != null && other != go && go.transform.IsChildOf(other.transform));
        if (level.alsoScan.Contains(go))
        {
            report.Add($"{go.name} is already scanned.");
        }
        else if (holder != null)
        {
            report.Add($"{go.name} is already scanned as part of {holder.name}.");
        }
        else if (!MapBuilder.CanAlsoScan(go, level))
        {
            report.Add($"{go.name} can't be scanned. Pick an object in this level's scene that sits beside {level.name}, not inside it or around it.");
        }
        else
        {
            Undo.RecordObject(level, "Add Object to Scan");
            level.alsoScan.Add(go);

            // Anything already listed inside it is covered now
            int covered = level.alsoScan.RemoveAll(other => other != null && other != go && other.transform.IsChildOf(go.transform));
            if (covered > 0) report.Add($"Removed {covered} object(s) now scanned as part of {go.name}.");
            MarkChanged(level);
        }
    }

    private void ScanLevel()
    {
        int added = MapBuilder.Scan(level, library);
        overlayIsStale = true;

        report.Clear();
        report.Add($"Scan finished: {added} new prefab(s) added to the library.");
        AddScanWarnings(MapBuilder.Collect(level, library));
    }

    private void BuildMap()
    {
        SaveLibrary();
        MapBuilder.ScanResult scan = MapBuilder.Collect(level, library);
        bool changed = MapBuilder.Build(level, library, scan);

        int rooms = 0, proxies = 0, meshes = 0, icons = 0, terminals = 0, doors = 0;
        foreach (MapBuilder.Placement placement in scan.placements)
        {
            if (placement.kind == MapEntryKind.Icon)
            {
                icons++;
                continue;
            }
            if (placement.kind == MapEntryKind.Terminal)
            {
                terminals++;
                continue;
            }
            if (placement.kind == MapEntryKind.Door)
            {
                doors++;
                continue;
            }
            rooms++;
            if (placement.proxy != null) proxies++;
            else if (placement.wires.Count > 0) meshes++;
        }

        report.Clear();
        report.Add(changed ? "Map built." : "Nothing changed, so the map was left as it is.");
        report.Add($"{rooms} room(s): {proxies} with proxies, {meshes} with meshes, {rooms - proxies - meshes} as boxes.");
        report.Add($"{terminals} terminal(s), {doors} door(s), {icons} icon(s), {scan.labels.Count} label(s).");
        AddScanWarnings(scan);
    }

    private void AddScanWarnings(MapBuilder.ScanResult scan)
    {
        foreach (GameObject prefab in scan.unlisted)
        {
            report.Add($"Not in the library, left off the map: {prefab.name}");
        }
        foreach (string name in scan.notPrefabs)
        {
            report.Add($"Not a prefab, left off the map: {name}");
        }
        foreach (string name in scan.noGeometry)
        {
            report.Add($"No visible geometry for a box: {name}");
        }
        foreach (string name in scan.disabled)
        {
            report.Add($"Disabled, left off the map: {name}");
        }
        foreach (string name in scan.missingMeshes)
        {
            report.Add($"None of the meshes picked for {name} are in it, so it shows as a box. If it's a prefab variant, add that prefab from the Project window to give it its own row and meshes.");
        }
        foreach (string name in scan.hidden)
        {
            report.Add($"{name} is disabled in the scene, so its map piece starts hidden.");
        }
        foreach (string name in scan.duplicateTerminals)
        {
            report.Add($"Two or more terminals are named {name}. Rename them so the map can tell them apart.");
        }
        foreach (string text in scan.lostAnchors)
        {
            report.Add($"Label \"{text}\" lost the object it sat over, so it's left off the map. Give it a new Anchor, or remove it.");
        }
    }

    private void DrawEntriesSection()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        GUILayout.Label($"Map Proxy Library ({library.entries.Count} prefabs)", EditorStyles.boldLabel);
        for (int i = 0; i < library.entries.Count; i++)
        {
            if (DrawLibraryRow(library.entries[i]))
            {
                Undo.RecordObject(library, "Remove Map Entry");
                library.entries.RemoveAt(i);
                MarkChanged(library);
                break;
            }
        }

        if (level != null)
        {
            DrawTerminalsSection();
            DrawDoorsSection();

            EditorGUILayout.Space();
            GUILayout.Label($"Objects in This Level ({level.sceneEntries.Count})", EditorStyles.boldLabel);
            for (int i = 0; i < level.sceneEntries.Count; i++)
            {
                if (DrawSceneRow(level.sceneEntries[i]))
                {
                    Undo.RecordObject(level, "Remove Map Entry");
                    level.sceneEntries.RemoveAt(i);
                    MarkChanged(level);
                    break;
                }
            }

            DrawLabelsSection();
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// The terminals found in the level. Nothing to set up: they go on the map on their own.
    /// </summary>
    private void DrawTerminalsSection()
    {
        EditorGUILayout.Space();
        GUILayout.Label(new GUIContent($"Terminals ({foundTerminals.Count}), found automatically", TerminalsHelp), EditorStyles.boldLabel);
        if (foundTerminals.Count == 0)
        {
            EditorGUILayout.HelpBox("No terminals in this level. Anything with the game's Terminal script shows up here.", MessageType.None);
            return;
        }

        foreach ((Terminal terminal, int index) in foundTerminals)
        {
            if (terminal == null) continue;
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawObjectName(terminal.gameObject);
                MapLevel.SceneEntry row = level.sceneEntries.Find(item => item.target == terminal.gameObject);
                string number = index >= 0 ? $"#{index}" : "not in TerminalManager";
                string hidden = terminal.gameObject.activeInHierarchy ? "" : ", starts hidden";
                string status = row != null && row.kind != MapEntryKind.Terminal ? $"added by hand as {row.kind}" : number + hidden;
                GUILayout.Label(status, GUILayout.Width(170));
            }
        }
    }

    /// <summary>
    /// The doors found in the level, with the state each shows. Nothing to set up.
    /// </summary>
    private void DrawDoorsSection()
    {
        EditorGUILayout.Space();
        GUILayout.Label(new GUIContent($"Doors ({foundDoors.Count}), found automatically", DoorsHelp), EditorStyles.boldLabel);
        if (foundDoors.Count == 0)
        {
            EditorGUILayout.HelpBox("No doors in this level. Anything with the game's DoorScript shows up here.", MessageType.None);
            return;
        }

        foreach ((DoorScript door, int index) in foundDoors)
        {
            if (door == null) continue;
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawObjectName(door.gameObject);
                MapLevel.SceneEntry row = level.sceneEntries.Find(item => item.target == door.gameObject);
                string number = index >= 0 ? $"#{index}" : "not in DoorManager";
                string hidden = door.gameObject.activeInHierarchy ? "" : ", starts hidden";
                string status = row != null && row.kind != MapEntryKind.Door
                    ? $"added by hand as {row.kind}"
                    : $"{number}, {MapDoors.GetState(door.DoorState).ToString().ToLower()}{hidden}";
                GUILayout.Label(status, GUILayout.Width(170));
            }
        }
    }

    /// <summary>
    /// Draws one library row and any lists open under it. Returns true if its remove button was pressed.
    /// </summary>
    private bool DrawLibraryRow(MapProxyLibrary.Entry entry)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawObjectName(entry.levelPrefab);
            DrawKind(library, entry);
            DrawShapeControls(library, entry, entry.levelPrefab);

            using (new EditorGUI.DisabledScope(MapBuilder.IsMarker(entry.kind)))
            {
                bool lookInside = EditorGUILayout.ToggleLeft(LookInsideLabel, entry.lookInside, GUILayout.Width(CheckboxWidth));
                if (lookInside != entry.lookInside)
                {
                    Undo.RecordObject(library, "Change Look Inside");
                    entry.lookInside = lookInside;
                    SetOpen(openInsideLists, entry, lookInside);
                    MarkChanged(library);
                }
            }

            if (GUILayout.Button("X", GUILayout.Width(22))) return true;
        }

        if (MapBuilder.HasShape(entry.kind))
        {
            DrawMeshSection(library, entry, entry.levelPrefab, 0f);
        }
        if (entry.lookInside && !MapBuilder.IsMarker(entry.kind))
        {
            DrawInsideSection(entry);
        }
        return false;
    }

    /// <summary>
    /// Draws one "Objects in This Level" row. Returns true if its remove button was pressed.
    /// </summary>
    private bool DrawSceneRow(MapLevel.SceneEntry entry)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawObjectName(entry.target);
            DrawKind(level, entry);
            DrawShapeControls(level, entry, entry.target);
            if (GUILayout.Button("X", GUILayout.Width(22))) return true;
        }

        if (MapBuilder.HasShape(entry.kind))
        {
            DrawMeshSection(level, entry, entry.target, 0f);
        }
        return false;
    }

    private void DrawObjectName(GameObject go)
    {
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField(go, typeof(GameObject), true);
        }
    }

    private void DrawKind(Object owner, MapEntry entry)
    {
        var kind = (MapEntryKind)EditorGUILayout.EnumPopup(entry.kind, GUILayout.Width(80));
        if (kind != entry.kind)
        {
            Undo.RecordObject(owner, "Change Map Entry Type");
            entry.kind = kind;

            // Doors draw their own lines in their state's color, so they don't take a proxy
            if (kind == MapEntryKind.Door) entry.proxy = null;
            MarkChanged(owner);
        }
    }

    /// <summary>
    /// Draws the proxy slot and the Use Meshes checkbox. A proxy wins over picked meshes,
    /// so the checkbox greys out while one is set.
    /// </summary>
    private void DrawShapeControls(Object owner, MapShape shape, GameObject source)
    {
        MapEntryKind kind = shape is MapEntry entry ? entry.kind : MapEntryKind.Room;
        using (new EditorGUI.DisabledScope(kind == MapEntryKind.Ignore || kind == MapEntryKind.Door))
        {
            var proxy = (GameObject)EditorGUILayout.ObjectField(shape.proxy, typeof(GameObject), false);
            if (proxy != shape.proxy)
            {
                Undo.RecordObject(owner, "Change Proxy");
                shape.proxy = proxy;
                MarkChanged(owner);
            }
        }

        using (new EditorGUI.DisabledScope(!MapBuilder.HasShape(kind) || shape.proxy != null))
        {
            bool useMeshes = EditorGUILayout.ToggleLeft(UseMeshesLabel, shape.useMeshes, GUILayout.Width(CheckboxWidth));
            if (useMeshes != shape.useMeshes)
            {
                Undo.RecordObject(owner, "Change Use Meshes");
                shape.useMeshes = useMeshes;
                if (useMeshes) TickAllForTerminal(kind, shape, source);
                SetOpen(openMeshLists, shape, useMeshes);
                MarkChanged(owner);
            }
        }
    }

    /// <summary>
    /// A terminal is small and every part of it matters, so it starts with all its meshes ticked.
    /// Rooms start with none, since they have far more.
    /// </summary>
    private void TickAllForTerminal(MapEntryKind kind, MapShape shape, GameObject source)
    {
        if (kind != MapEntryKind.Terminal || shape.meshes.Count > 0 || source == null) return;

        foreach (MeshRow row in GetMeshes(source))
        {
            shape.meshes.Add(row.mesh);
        }
    }

    /// <summary>
    /// A collapsible list of every mesh under the source object, biggest first and all off until ticked.
    /// </summary>
    private void DrawMeshSection(Object owner, MapShape shape, GameObject source, float indent)
    {
        if (!shape.useMeshes || shape.proxy != null || source == null) return;

        List<MeshRow> rows = GetMeshes(source);
        if (!DrawFoldout(openMeshLists, shape, $"Meshes ({shape.meshes.Count} of {rows.Count} on)", indent)) return;

        DrawMergeSlider(owner, shape, indent + ListIndent);
        foreach (MeshRow row in rows)
        {
            bool wasOn = shape.meshes.Contains(row.mesh);
            bool isOn;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(indent + ListIndent);
                isOn = EditorGUILayout.ToggleLeft(row.label, wasOn);
            }

            if (isOn != wasOn)
            {
                Undo.RecordObject(owner, "Change Picked Meshes");
                if (isOn) shape.meshes.Add(row.mesh);
                else shape.meshes.Remove(row.mesh);
                MarkChanged(owner);
            }
        }
    }

    private void DrawMergeSlider(Object owner, MapShape shape, float indent)
    {
        float merge;
        EditorGUI.BeginChangeCheck();
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(indent);
            merge = EditorGUILayout.Slider(MergeLabel, shape.mergeDistance, 0f, MaxMergeDistance);
        }

        // Only when it's moved, so drawing never rewrites a value typed elsewhere
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(owner, "Change Merge Distance");
            shape.mergeDistance = Mathf.Round(merge * 100f) / 100f;
            MarkChanged(owner);
        }
    }

    /// <summary>
    /// A collapsible list of the prefab's direct children, all off until ticked.
    /// A ticked child gets its own piece on the map.
    /// </summary>
    private void DrawInsideSection(MapProxyLibrary.Entry entry)
    {
        if (entry.levelPrefab == null) return;

        Transform prefab = entry.levelPrefab.transform;
        if (!DrawFoldout(openInsideLists, entry, $"Look Inside ({entry.inside.Count} of {prefab.childCount} on)", 0f)) return;

        foreach (Transform child in prefab)
        {
            DrawInsideRow(entry, child.gameObject);
        }
    }

    private void DrawInsideRow(MapProxyLibrary.Entry entry, GameObject child)
    {
        MapProxyLibrary.InsideEntry inside = entry.inside.Find(item => item.child == child);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(ListIndent);
            bool ticked = EditorGUILayout.ToggleLeft(child.name, inside != null);

            if (ticked != (inside != null))
            {
                Undo.RecordObject(library, "Change Look Inside");
                if (ticked) entry.inside.Add(new MapProxyLibrary.InsideEntry { child = child });
                else entry.inside.Remove(inside);
                MarkChanged(library);
                return;
            }

            if (inside != null)
            {
                DrawShapeControls(library, inside, child);
            }
        }

        if (inside != null)
        {
            DrawMeshSection(library, inside, child, ListIndent);
        }
    }

    private bool DrawFoldout(HashSet<object> openLists, object key, string label, float indent)
    {
        bool wasOpen = openLists.Contains(key);
        bool isOpen;
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(indent + ListIndent);
            isOpen = EditorGUILayout.Foldout(wasOpen, label, true);
        }

        if (isOpen != wasOpen)
        {
            SetOpen(openLists, key, isOpen);
        }
        return isOpen;
    }

    private static void SetOpen(HashSet<object> openLists, object key, bool open)
    {
        if (open) openLists.Add(key);
        else openLists.Remove(key);
    }

    private List<MeshRow> GetMeshes(GameObject source)
    {
        if (meshLists.TryGetValue(source, out List<MeshRow> rows)) return rows;

        var counts = new Dictionary<Mesh, int>();
        foreach (MeshFilter filter in source.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh != null && !counts.ContainsKey(mesh)) counts[mesh] = CountTriangles(mesh);
        }

        var meshes = new List<Mesh>(counts.Keys);
        meshes.Sort((a, b) => counts[b].CompareTo(counts[a]));

        rows = new List<MeshRow>();
        foreach (Mesh mesh in meshes)
        {
            rows.Add(new MeshRow { mesh = mesh, label = $"{mesh.name}  ({counts[mesh]:N0} tris)" });
        }

        meshLists[source] = rows;
        return rows;
    }

    private static int CountTriangles(Mesh mesh)
    {
        int triangles = 0;
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            triangles += (int)mesh.GetIndexCount(i) / 3;
        }
        return triangles;
    }

    private void DrawAddSection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(AddHelp, MessageType.None);

        var added = (GameObject)EditorGUILayout.ObjectField("Add Prefab or Object", null, typeof(GameObject), true);
        if (added != null)
        {
            AddToMap(added);
        }

        using (new EditorGUI.DisabledScope(Selection.count == 0))
        {
            if (GUILayout.Button("Add Selected"))
            {
                foreach (GameObject selected in Selection.gameObjects)
                {
                    AddToMap(selected);
                }
            }
        }
    }

    /// <summary>
    /// A prefab from the Project window goes in the library and covers every copy of it.
    /// An object from the Hierarchy is saved on the level and covers only that object.
    /// </summary>
    private void AddToMap(GameObject go)
    {
        if (EditorUtility.IsPersistent(go))
        {
            GameObject prefab = go.transform.root.gameObject;
            MapProxyLibrary.Entry entry = library.Find(prefab);

            Undo.RecordObject(library, "Add Prefab");
            if (entry == null)
            {
                var added = new MapProxyLibrary.Entry { levelPrefab = prefab, placeNested = true };
                StartLike(added, prefab);
                library.entries.Add(added);
            }
            else
            {
                entry.placeNested = true;
            }
            MarkChanged(library);
            SaveLibrary();
        }
        else if (level == null || go == level.gameObject || go.scene != level.gameObject.scene)
        {
            report.Clear();
            report.Add($"{go.name} can't be added: it has to be an object in the level's scene, other than the MapLevel object.");
        }
        else if (!level.sceneEntries.Exists(item => item.target == go))
        {
            Undo.RecordObject(level, "Add Object to Map");
            var added = new MapLevel.SceneEntry { target = go };
            StartLike(added, go);
            level.sceneEntries.Add(added);
            MarkChanged(level);
        }
    }

    /// <summary>
    /// A new row starts like the row it overrides, so adding a terminal or its variant keeps it a terminal.
    /// Picked meshes aren't copied, since a new row is usually added for different ones.
    /// </summary>
    private void StartLike(MapEntry added, GameObject go)
    {
        MapProxyLibrary.Entry current = MapBuilder.FindEntry(go, library);
        if (current == null || current.kind == MapEntryKind.Ignore) return;

        added.kind = current.kind;
        added.useMeshes = current.useMeshes;
        added.mergeDistance = current.mergeDistance;
        if (added.useMeshes) SetOpen(openMeshLists, added, true);
    }

    private void DrawReport()
    {
        if (report.Count > 0)
        {
            EditorGUILayout.HelpBox(string.Join("\n", report), MessageType.None);
        }
    }

    /// <summary>
    /// Works out the map again after something changed. Shared by the overlay and the label tools.
    /// </summary>
    private void RefreshOverlay()
    {
        if (!overlayIsStale) return;

        // A selected label or hovered piece belongs to the level it was picked in
        MapLevel found = MapBuilder.FindLevel(EditorSceneManager.GetActiveScene());
        if (found != overlayLevel)
        {
            selectedLabel = -1;
            hoverSource = null;
        }
        overlayLevel = found;
        overlayPlacements = overlayLevel != null
            ? MapBuilder.Collect(overlayLevel, library).placements
            : new List<MapBuilder.Placement>();

        // Rooms can be clicked to label them. Terminals can't, since they stand out by their fill instead
        pieces.Clear();
        markerParts.Clear();
        foreach (MapBuilder.Placement placement in overlayPlacements)
        {
            if (placement.source == null) continue;
            // A door's frame is drawn as a room, but it's still part of a door
            bool doorFrame = placement.source.TryGetComponent<DoorScript>(out _);
            if (placement.kind == MapEntryKind.Room && !doorFrame) pieces.Add(placement.source);
            else if (placement.kind != MapEntryKind.Room || doorFrame) markerParts.Add(placement.source);
        }

        roomOverlayColor = MapBuilder.GetLineColor(MapEntryKind.Room);
        terminalOverlayColor = MapBuilder.GetLineColor(MapEntryKind.Terminal);
        roomOverlayColor.a = terminalOverlayColor.a = OverlayAlpha;
        for (int i = 0; i < doorOverlayColors.Length; i++)
        {
            doorOverlayColors[i] = MapBuilder.GetDoorColor((MapDoorState)i);
            doorOverlayColors[i].a = OverlayAlpha;
        }
        overlayIsStale = false;
    }

    /// <summary>
    /// Draws the proxies, meshes and boxes over the real level. Nothing here is saved into the scene.
    /// </summary>
    private void DrawOverlay(SceneView view)
    {
        if (!DrawsInScene || library == null || Event.current.type != EventType.Repaint) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        RefreshOverlay();

        if (overlayMaterial == null)
        {
            overlayMaterial = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
            overlayMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
            overlayMaterial.SetInt("_ZWrite", 0);
        }

        // Handles settings are shared with every other tool drawing in the Scene view, so put them back
        CompareFunction previousZTest = Handles.zTest;
        Handles.zTest = CompareFunction.Always;
        try
        {
            using (new Handles.DrawingScope(Handles.color))
            {
                foreach (MapBuilder.Placement placement in overlayPlacements)
                {
                    if (placement.source == null) continue;

                    // The piece a click would label, or the selected label's piece, shows in white
                    Color color = IsHighlighted(placement.source) ? new Color(1f, 1f, 1f, OverlayAlpha)
                        : placement.kind == MapEntryKind.Terminal ? terminalOverlayColor
                        : placement.kind == MapEntryKind.Door ? doorOverlayColors[(int)placement.doorState]
                        : roomOverlayColor;

                    // A terminal that starts hidden is drawn faint
                    if (!placement.active) color.a = HiddenOverlayAlpha;
                    Handles.color = color;
                    overlayMaterial.SetColor("_Color", color);

                    Matrix4x4 world = placement.source.localToWorldMatrix;
                    if (placement.proxy != null)
                    {
                        DrawProxy(placement.proxy, world);
                    }
                    else if (placement.wires.Count > 0)
                    {
                        DrawWires(placement, world);
                    }
                    else if (MapBuilder.HasShape(placement.kind))
                    {
                        Handles.matrix = world;
                        Handles.DrawWireCube(placement.box.center, placement.box.size);
                    }
                }
            }
        }
        finally
        {
            Handles.zTest = previousZTest;
        }
    }

    private void DrawWires(MapBuilder.Placement placement, Matrix4x4 world)
    {
        if (!overlayLines.TryGetValue(placement.wireKey, out Mesh lines))
        {
            lines = MapLines.ToMesh(MapBuilder.GetWireLines(placement));
            lines.hideFlags = HideFlags.HideAndDontSave;
            overlayLines[placement.wireKey] = lines;
        }

        overlayMaterial.SetPass(0);
        Graphics.DrawMeshNow(lines, world);
    }

    private void DrawProxy(GameObject proxy, Matrix4x4 world)
    {
        overlayMaterial.SetPass(0);
        GL.wireframe = true;
        try
        {
            foreach (MeshFilter filter in proxy.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;

                Matrix4x4 local = proxy.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                Graphics.DrawMeshNow(filter.sharedMesh, world * local);
            }
        }
        finally
        {
            GL.wireframe = false;
        }
    }
}
