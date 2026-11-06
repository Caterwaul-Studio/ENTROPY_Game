using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The Map Builder window: scan a level, pair its prefabs with proxies, and bake the map.
/// Rows read and write the data directly, because this redraws on every event and
/// SerializedProperty is far too slow for lists this long.
/// </summary>
public class MapBuilderWindow : EditorWindow
{
    const float ListIndent = 18f;
    const float CheckboxWidth = 88f;
    const float MaxMergeDistance = 0.5f;
    const string AddHelp = "From the Project window: covers every copy of that prefab.\n"
        + "From the Hierarchy: covers just that object, prefab or not, and overrides the scan.";

    static readonly Color OverlayColor = new Color(0.35f, 0.85f, 1f, 0.9f);
    static readonly GUIContent LookInsideLabel = new GUIContent("Look Inside", "List this prefab's children so you can put them on the map one by one.");
    static readonly GUIContent UseMeshesLabel = new GUIContent("Use Meshes", "Pick which meshes draw this room, instead of a box.");
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
    private Vector2 scroll;
    private readonly List<string> report = new List<string>();
    private readonly HashSet<object> openMeshLists = new HashSet<object>();
    private readonly HashSet<object> openInsideLists = new HashSet<object>();
    private readonly Dictionary<GameObject, List<MeshRow>> meshLists = new Dictionary<GameObject, List<MeshRow>>();

    private bool showOverlay;
    private bool overlayIsStale = true;
    private List<MapBuilder.Placement> overlayPlacements = new List<MapBuilder.Placement>();
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
        EditorApplication.hierarchyChanged += OnLevelChanged;
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DrawOverlay;
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

    private void SaveLibrary()
    {
        if (library != null) AssetDatabase.SaveAssetIfDirty(library);
    }

    private void OnLevelChanged()
    {
        levelIsStale = true;
        overlayIsStale = true;
        meshLists.Clear();
    }

    private void OnUndoRedo()
    {
        OnLevelChanged();
        Repaint();
        if (showOverlay) SceneView.RepaintAll();
    }

    private void MarkChanged(Object owner)
    {
        EditorUtility.SetDirty(owner);
        overlayIsStale = true;
        if (showOverlay) SceneView.RepaintAll();
    }

    private void OnGUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorGUILayout.HelpBox("The Map Builder is off in Play mode, so a bake can't capture a level mid-game.", MessageType.Info);
            return;
        }

        if (library == null)
        {
            library = MapBuilder.GetOrCreateLibrary();
        }

        // Layout only, because the rows have to stay the same between Layout and Repaint
        if (Event.current.type == EventType.Layout && (levelIsStale || level == null))
        {
            level = MapBuilder.FindLevel(EditorSceneManager.GetActiveScene());
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

        bool overlay = GUILayout.Toggle(showOverlay, "Show Map Overlay", "Button");
        if (overlay != showOverlay)
        {
            showOverlay = overlay;
            overlayIsStale = true;
            SceneView.RepaintAll();
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

        int rooms = 0, proxies = 0, meshes = 0, icons = 0;
        foreach (MapBuilder.Placement placement in scan.placements)
        {
            if (placement.kind == MapEntryKind.Icon)
            {
                icons++;
                continue;
            }
            rooms++;
            if (placement.proxy != null) proxies++;
            else if (placement.wires.Count > 0) meshes++;
        }

        report.Clear();
        report.Add(changed ? "Map built." : "Nothing changed, so the map was left as it is.");
        report.Add($"{rooms} room(s): {proxies} with proxies, {meshes} with meshes, {rooms - proxies - meshes} as boxes. {icons} icon(s).");
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
        }

        EditorGUILayout.EndScrollView();
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
            DrawShapeControls(library, entry);

            using (new EditorGUI.DisabledScope(entry.kind == MapEntryKind.Icon))
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

        if (entry.kind == MapEntryKind.Room)
        {
            DrawMeshSection(library, entry, entry.levelPrefab, 0f);
        }
        if (entry.lookInside && entry.kind != MapEntryKind.Icon)
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
            DrawShapeControls(level, entry);
            if (GUILayout.Button("X", GUILayout.Width(22))) return true;
        }

        if (entry.kind == MapEntryKind.Room)
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
        var kind = (MapEntryKind)EditorGUILayout.EnumPopup(entry.kind, GUILayout.Width(70));
        if (kind != entry.kind)
        {
            Undo.RecordObject(owner, "Change Map Entry Type");
            entry.kind = kind;
            MarkChanged(owner);
        }
    }

    /// <summary>
    /// Draws the proxy slot and the Use Meshes checkbox. A proxy wins over picked meshes,
    /// so the checkbox greys out while one is set.
    /// </summary>
    private void DrawShapeControls(Object owner, MapShape shape)
    {
        MapEntryKind kind = shape is MapEntry entry ? entry.kind : MapEntryKind.Room;
        using (new EditorGUI.DisabledScope(kind == MapEntryKind.Ignore))
        {
            var proxy = (GameObject)EditorGUILayout.ObjectField(shape.proxy, typeof(GameObject), false);
            if (proxy != shape.proxy)
            {
                Undo.RecordObject(owner, "Change Proxy");
                shape.proxy = proxy;
                MarkChanged(owner);
            }
        }

        using (new EditorGUI.DisabledScope(kind != MapEntryKind.Room || shape.proxy != null))
        {
            bool useMeshes = EditorGUILayout.ToggleLeft(UseMeshesLabel, shape.useMeshes, GUILayout.Width(CheckboxWidth));
            if (useMeshes != shape.useMeshes)
            {
                Undo.RecordObject(owner, "Change Use Meshes");
                shape.useMeshes = useMeshes;
                SetOpen(openMeshLists, shape, useMeshes);
                MarkChanged(owner);
            }
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
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(indent);
            merge = EditorGUILayout.Slider(MergeLabel, shape.mergeDistance, 0f, MaxMergeDistance);
        }

        merge = Mathf.Round(merge * 100f) / 100f;
        if (merge != shape.mergeDistance)
        {
            Undo.RecordObject(owner, "Change Merge Distance");
            shape.mergeDistance = merge;
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
                DrawShapeControls(library, inside);
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
            if (entry == null) library.entries.Add(new MapProxyLibrary.Entry { levelPrefab = prefab, placeNested = true });
            else entry.placeNested = true;
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
            level.sceneEntries.Add(new MapLevel.SceneEntry { target = go });
            MarkChanged(level);
        }
    }

    private void DrawReport()
    {
        if (report.Count > 0)
        {
            EditorGUILayout.HelpBox(string.Join("\n", report), MessageType.None);
        }
    }

    /// <summary>
    /// Draws the proxies, meshes and boxes over the real level. Nothing here is saved into the scene.
    /// </summary>
    private void DrawOverlay(SceneView view)
    {
        if (!showOverlay || library == null || Event.current.type != EventType.Repaint) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        if (overlayIsStale)
        {
            MapLevel current = MapBuilder.FindLevel(EditorSceneManager.GetActiveScene());
            overlayPlacements = current != null
                ? MapBuilder.Collect(current, library).placements
                : new List<MapBuilder.Placement>();
            overlayIsStale = false;
        }

        if (overlayMaterial == null)
        {
            overlayMaterial = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
            overlayMaterial.SetColor("_Color", OverlayColor);
            overlayMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
            overlayMaterial.SetInt("_ZWrite", 0);
        }

        // Handles settings are shared with every other tool drawing in the Scene view, so put them back
        CompareFunction previousZTest = Handles.zTest;
        Handles.zTest = CompareFunction.Always;
        try
        {
            using (new Handles.DrawingScope(OverlayColor))
            {
                foreach (MapBuilder.Placement placement in overlayPlacements)
                {
                    if (placement.source == null) continue;

                    Matrix4x4 world = placement.source.localToWorldMatrix;
                    if (placement.proxy != null)
                    {
                        DrawProxy(placement.proxy, world);
                    }
                    else if (placement.wires.Count > 0)
                    {
                        DrawWires(placement, world);
                    }
                    else if (placement.kind == MapEntryKind.Room)
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
