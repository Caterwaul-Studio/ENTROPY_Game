using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pairs each level prefab with what stands in for it on the hologram map.
/// Only the Map Builder reads this, so it stays in an Editor folder and never ships with the game.
/// </summary>
public class MapProxyLibrary : ScriptableObject
{
    /// <summary>
    /// A child of a level prefab that gets its own piece on the map.
    /// </summary>
    [Serializable]
    public class InsideEntry : MapShape
    {
        public GameObject child;
    }

    [Serializable]
    public class Entry : MapEntry
    {
        public GameObject levelPrefab;

        // Set on prefabs added by hand, so they show even when they sit inside another room
        public bool placeNested;

        public bool lookInside;
        public List<InsideEntry> inside = new List<InsideEntry>();
    }

    public List<Entry> entries = new List<Entry>();

    public Entry Find(GameObject levelPrefab)
    {
        return entries.Find(entry => entry.levelPrefab == levelPrefab);
    }
}
