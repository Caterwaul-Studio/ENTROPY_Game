using System;
using System.Collections.Generic;
using UnityEngine;

// Saved as numbers, so new types go at the end
public enum MapEntryKind { Room = 0, Icon = 1, Ignore = 2, Terminal = 3, Door = 4 }

/// <summary>
/// What draws something on the map: a proxy, picked meshes, or a box if neither is set.
/// </summary>
[Serializable]
public class MapShape
{
    public GameObject proxy;
    public bool useMeshes;
    public List<Mesh> meshes = new List<Mesh>();

    [Tooltip("Lines closer together than this, in metres, merge into one. 0 turns merging off.")]
    public float mergeDistance = 0.2f;
}

[Serializable]
public class MapEntry : MapShape
{
    public MapEntryKind kind = MapEntryKind.Room;
}

/// <summary>
/// Marks a level's root object for the hologram map. The Map Builder scans everything under this object.
/// </summary>
public class MapLevel : MonoBehaviour
{
    // The groups inside a baked map, one per type, so the map display can find what it needs
    public const string RoomsGroup = "Rooms";
    public const string IconsGroup = "Icons";
    public const string TerminalsGroup = "Terminals";
    public const string DoorsGroup = "Doors";
    public const string LabelsGroup = "Labels";

    /// <summary>
    /// One object in this scene, added by hand. These live here because the project-wide
    /// library can't reference scene objects.
    /// </summary>
    [Serializable]
    public class SceneEntry : MapEntry
    {
        public GameObject target;
    }

    /// <summary>
    /// Text shown over part of the map, placed from the Map Builder.
    /// </summary>
    [Serializable]
    public class LabelEntry
    {
        public string text = "Label";

        // The label follows this object when it moves. Empty: it stays where it was put
        public GameObject anchor;

        // Where the label sits from its anchor, or from the level root when it has none. In metres, turning with the anchor
        public Vector3 offset;

        // Remembers that an anchor was set, so a deleted one is reported instead of the label jumping to the level root
        [HideInInspector] public bool hasAnchor;
    }

    [Tooltip("The baked map for this level. Filled in by the Map Builder.")]
    public GameObject mapPrefab;

    [Tooltip("Other objects in this scene whose children belong on the map, such as rooms kept outside this object. Scanned the same way as this object. Terminals don't need this: they're found anywhere in the scene.")]
    public List<GameObject> alsoScan = new List<GameObject>();

    [Tooltip("Objects added by hand in the Map Builder. These override anything the scan finds.")]
    public List<SceneEntry> sceneEntries = new List<SceneEntry>();

    // Edited only in the Map Builder's Labels list, which keeps each label's anchor and position in step
    [HideInInspector] public List<LabelEntry> labels = new List<LabelEntry>();

    [Tooltip("Label text size in metres at full map size, about the height of one line.")]
    public float labelSize = 1f;
}
