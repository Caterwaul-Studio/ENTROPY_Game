using System;
using System.Collections.Generic;
using UnityEngine;

public enum MapEntryKind { Room, Icon, Ignore }

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
    /// <summary>
    /// One object in this scene, added by hand. These live here because the project-wide
    /// library can't reference scene objects.
    /// </summary>
    [Serializable]
    public class SceneEntry : MapEntry
    {
        public GameObject target;
    }

    [Tooltip("The baked map for this level. Filled in by the Map Builder.")]
    public GameObject mapPrefab;

    [Tooltip("Objects added by hand in the Map Builder. These override anything the scan finds.")]
    public List<SceneEntry> sceneEntries = new List<SceneEntry>();
}
