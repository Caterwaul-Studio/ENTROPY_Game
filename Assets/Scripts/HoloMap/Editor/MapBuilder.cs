using System.Collections.Generic;
using System.IO;
using System.Text;
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
    const string BoxMaterialPath = GeneratedFolder + "/MapBoxFallback.mat";

    // Raise this when the baked layout changes, so existing maps rebuild
    const int BakeVersion = 2;

    static readonly Color BoxColor = new Color(0.35f, 0.85f, 1f);

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
    }

    /// <summary>
    /// Everything found in one pass over the level.
    /// </summary>
    public class ScanResult
    {
        public List<Placement> placements = new List<Placement>();
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
            if (entry.kind == MapEntryKind.Room)
            {
                AddRoom(entry.target.transform, level.transform, entry.proxy, entry.useMeshes ? entry : null, result);
            }
            else if (entry.kind == MapEntryKind.Icon)
            {
                result.placements.Add(CreatePlacement(entry.target.transform, level.transform, entry.kind, entry.proxy));
            }
        }

        foreach (Transform child in level.transform)
        {
            Walk(child, level.transform, false, library, result);
        }
        return result;
    }

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
        string signature = GetSignature(scan.placements);

        var importer = AssetImporter.GetAtPath(mapPath);
        bool changed = importer == null || importer.userData != signature;
        if (changed)
        {
            SaveMap(mapPath, levelScene.name, scan.placements);
            importer = AssetImporter.GetAtPath(mapPath);
            importer.userData = signature;
            importer.SaveAndReimport();
            DeleteUnusedWires();
            Debug.Log($"Hologram map built: {mapPath}");
        }

        GameObject map = AssetDatabase.LoadAssetAtPath<GameObject>(mapPath);
        if (level.mapPrefab != map)
        {
            level.mapPrefab = map;
            EditorUtility.SetDirty(level);
        }
        return changed;
    }

    static void Walk(Transform t, Transform levelRoot, bool inRoom, MapProxyLibrary library, ScanResult result)
    {
        GameObject go = t.gameObject;
        if (result.handAdded.TryGetValue(go, out MapEntryKind handAddedKind))
        {
            // Already handled by Collect. Icons and disabled objects have nothing more to find underneath
            if (handAddedKind == MapEntryKind.Icon || !go.activeSelf) return;

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
                    AddRoom(t, levelRoot, entry.proxy, entry.useMeshes ? entry : null, result);
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
            Transform top = t;
            while (top.parent != levelRoot) top = top.parent;
            if (!result.notPrefabs.Contains(top.name)) result.notPrefabs.Add(top.name);
        }

        foreach (Transform child in t)
        {
            Walk(child, levelRoot, inRoom, library, result);
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

            MapProxyLibrary.Entry listed = FindEntry(go, library);
            if (listed != null)
            {
                if (proxy == null) proxy = listed.proxy;
                if (meshSource == null && listed.useMeshes) meshSource = listed;
            }
            AddRoom(child, levelRoot, proxy, meshSource, result);
        }
    }

    static void AddRoom(Transform room, Transform levelRoot, GameObject proxy, MapShape meshSource, ScanResult result)
    {
        // The same object can be reached more than once, such as a hand-added prefab ticked under Look Inside
        if (!result.placed.Add(room)) return;

        Placement placement = CreatePlacement(room, levelRoot, MapEntryKind.Room, proxy);
        if (proxy == null && meshSource != null)
        {
            placement.wires = FindWires(room, meshSource.meshes);
            placement.worldScale = room.lossyScale;
            placement.mergeDistance = meshSource.mergeDistance;
            placement.wireKey = GetWireKey(placement, result);
        }
        if (proxy == null && placement.wires.Count == 0)
        {
            placement.box = GetLocalBounds(room);
            if (placement.box.size == Vector3.zero)
            {
                result.noGeometry.Add(room.name);
                return;
            }
        }
        result.placements.Add(placement);
    }

    /// <summary>
    /// Finds every copy of the picked meshes inside a room, so they land where they really are.
    /// </summary>
    static List<WirePiece> FindWires(Transform room, List<Mesh> meshes)
    {
        var wires = new List<WirePiece>();
        foreach (MeshFilter filter in room.GetComponentsInChildren<MeshFilter>(!room.gameObject.activeInHierarchy))
        {
            if (filter.sharedMesh == null || !meshes.Contains(filter.sharedMesh)) continue;

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
        var text = new StringBuilder($"{BakeVersion},{GetKeyText(scale.x)},{GetKeyText(scale.y)},{GetKeyText(scale.z)},{GetKeyText(placement.mergeDistance)}");
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

    static MapProxyLibrary.Entry FindEntry(GameObject go, MapProxyLibrary library)
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
        var bounds = new Bounds();
        bool hasBounds = false;

        // A disabled room was added by hand, so it still needs measuring
        bool includeInactive = !room.gameObject.activeInHierarchy;
        foreach (Renderer renderer in room.GetComponentsInChildren<Renderer>(includeInactive))
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;

            Matrix4x4 toRoom = room.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
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
        return bounds;
    }

    static string GetSignature(List<Placement> placements)
    {
        var text = new StringBuilder(BakeVersion.ToString());
        foreach (Placement placement in placements)
        {
            string proxyPath = placement.proxy != null ? AssetDatabase.GetAssetPath(placement.proxy) : "";
            text.Append($"|{placement.name},{placement.kind},{AssetDatabase.AssetPathToGUID(proxyPath)}");
            text.Append($",{placement.position:F3},{placement.rotation:F4},{placement.scale:F3}");
            text.Append($",{placement.box.center:F3},{placement.box.size:F3}");
            text.Append($",{placement.wireKey}");
        }
        return Hash128.Compute(text.ToString()).ToString();
    }

    /// <summary>
    /// Assembles the map in a preview scene, so baking never touches or dirties the open level.
    /// </summary>
    static void SaveMap(string mapPath, string levelName, List<Placement> placements)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = new GameObject($"{levelName}_Map");
            SceneManager.MoveGameObjectToScene(root, scene);

            foreach (Placement placement in placements)
            {
                CreateNode(placement, root.transform);
            }
            PrefabUtility.SaveAsPrefabAsset(root, mapPath);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
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

        if (placement.wires.Count > 0)
        {
            AddLines("Wire", node.transform, GetWireMesh(placement));
        }
        else if (placement.proxy == null && placement.kind == MapEntryKind.Room)
        {
            Transform box = AddLines("Box", node.transform, GetBoxMesh());
            box.localPosition = placement.box.center;
            box.localScale = placement.box.size;
        }
    }

    static Transform AddLines(string name, Transform parent, Mesh lines)
    {
        var piece = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        piece.transform.SetParent(parent, false);
        piece.GetComponent<MeshFilter>().sharedMesh = lines;
        piece.GetComponent<MeshRenderer>().sharedMaterial = GetBoxMaterial();
        return piece.transform;
    }

    static Mesh GetWireMesh(Placement placement)
    {
        string path = $"{WireFolder}/{placement.wireKey}.asset";
        Mesh wire = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (wire != null) return wire;

        EnsureFolder(WireFolder);
        wire = MapLines.ToMesh(GetWireLines(placement));
        AssetDatabase.CreateAsset(wire, path);
        return wire;
    }

    /// <summary>
    /// Clears out line meshes no map uses any more, such as the ones left by trying other merge distances.
    /// </summary>
    static void DeleteUnusedWires()
    {
        if (!AssetDatabase.IsValidFolder(WireFolder)) return;

        var used = new HashSet<string>();
        foreach (string guid in AssetDatabase.FindAssets("_Map t:Prefab"))
        {
            used.UnionWith(AssetDatabase.GetDependencies(AssetDatabase.GUIDToAssetPath(guid), false));
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { WireFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!used.Contains(path)) AssetDatabase.DeleteAsset(path);
        }
    }

    /// <summary>
    /// A wireframe cube shared by every room that has no proxy yet.
    /// </summary>
    static Mesh GetBoxMesh()
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(BoxMeshPath);
        if (mesh != null) return mesh;

        mesh = MapLines.ToMesh(MapLines.GetHardEdges(Resources.GetBuiltinResource<Mesh>("Cube.fbx")));
        EnsureFolder(GeneratedFolder);
        AssetDatabase.CreateAsset(mesh, BoxMeshPath);
        return mesh;
    }

    static Material GetBoxMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(BoxMaterialPath);
        if (material != null) return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        material = new Material(shader) { color = BoxColor };
        EnsureFolder(GeneratedFolder);
        AssetDatabase.CreateAsset(material, BoxMaterialPath);
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
