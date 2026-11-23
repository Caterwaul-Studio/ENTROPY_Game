using System;
using System.Collections.Generic;
using UnityEngine;

// Saved as numbers, add new types at the end
public enum MapEntryKind { Room = 0, Icon = 1, Ignore = 2, Terminal = 3, Door = 4 }

[Serializable]
public class MapShape
{
    public GameObject proxy;
    public bool useMeshes;
    public List<Mesh> meshes = new List<Mesh>();
    public float mergeDistance = 0.2f;
}

[Serializable]
public class MapEntry : MapShape
{
    public MapEntryKind kind = MapEntryKind.Room;
}

public class MapLevel : MonoBehaviour
{
    public const string RoomsGroup = "Rooms";
    public const string IconsGroup = "Icons";
    public const string TerminalsGroup = "Terminals";
    public const string DoorsGroup = "Doors";
    public const string LabelsGroup = "Labels";
    public const string PlayerMarkerName = "Player";

    [Serializable]
    public class SceneEntry : MapEntry
    {
        public GameObject target;
    }

    [Serializable]
    public class LabelEntry
    {
        public string text = "Label";
        public GameObject anchor;
        public Vector3 offset;
        [HideInInspector] public bool hasAnchor;
    }

    [Serializable]
    public class SectorEntry
    {
        public string name = "";

        // In the level root's space
        public List<Bounds> boxes = new List<Bounds>();

        // Editor overlay only. Clear means automatic
        public Color color = Color.clear;
    }

    public GameObject mapPrefab;
    public List<GameObject> alsoScan = new List<GameObject>();
    public List<SceneEntry> sceneEntries = new List<SceneEntry>();
    [HideInInspector] public List<LabelEntry> labels = new List<LabelEntry>();
    public float labelSize = 1f;
    public float playerMarkerSize = 1f;
    [Range(0f, 1f)] public float depthDimming = 0.9f;
    [HideInInspector] public List<SectorEntry> sectors = new List<SectorEntry>();
    [Range(0f, 1f)] public float unpoweredBrightness = 0.25f;
    [Range(0f, 1f)] public float unpoweredSaturation = 0.2f;
}
