using System;
using System.Collections.Generic;
using TMPro;
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

    [Tooltip("The TextMesh Pro font asset map labels use, glow and colors included, like Oxanium-Regular SDF. Empty: TextMesh Pro's default font.")]
    public TMP_FontAsset labelFont;

    [Tooltip("The color of the label text, set the way the game's own texts get theirs. Default: a deep translucent cyan, #006A80 at 49%, so labels sit back from the map's lines.")]
    public Color labelColor = new Color(0f, 0.4148f, 0.5f, 0.4901f);

    public Entry Find(GameObject levelPrefab)
    {
        return entries.Find(entry => entry.levelPrefab == levelPrefab);
    }
}
