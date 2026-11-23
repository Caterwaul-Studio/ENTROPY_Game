using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MapProxyLibrary : ScriptableObject
{
    [Serializable]
    public class InsideEntry : MapShape
    {
        public GameObject child;
    }

    [Serializable]
    public class Entry : MapEntry
    {
        public GameObject levelPrefab;
        public bool placeNested;
        public bool lookInside;
        public List<InsideEntry> inside = new List<InsideEntry>();
    }

    public List<Entry> entries = new List<Entry>();
    public TMP_FontAsset labelFont;
    public Color labelColor = new Color(0f, 0.4148f, 0.5f, 0.4901f);

    public Entry Find(GameObject levelPrefab)
    {
        return entries.Find(entry => entry.levelPrefab == levelPrefab);
    }
}
