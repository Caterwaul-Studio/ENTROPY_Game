using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;

public partial class MapBuilderWindow
{
    static readonly GUIContent SectorColorLabel = new GUIContent("");
    static readonly GUIContent SectorNameLabel = new GUIContent("", "Must match the power zone name, like A2");
    static readonly GUIContent AddBoxLabel = new GUIContent("Add Box");
    static readonly GUIContent FindBoxLabel = new GUIContent("Find");
    static readonly GUIContent UnpoweredBrightnessLabel = new GUIContent("Unpowered Brightness");
    static readonly GUIContent UnpoweredSaturationLabel = new GUIContent("Unpowered Color");
    static readonly GUIContent ShowSectorsLabel = new GUIContent("Show Sectors");

    static readonly Vector3 NewBoxSize = new Vector3(10f, 4f, 10f);
    static readonly Color NoSectorColor = new Color(0.55f, 0.55f, 0.55f, OverlayAlpha);

    private readonly HashSet<object> openSectors = new HashSet<object>();
    private bool showSectors;

    // Indexes survive undo, -1 is none
    private int selectedSector = -1;
    private int selectedBox = -1;

    // Collect is slow on big levels, so refresh after the drag
    private bool sectorsNeedRefresh;
    private readonly BoxBoundsHandle boxHandle = new BoxBoundsHandle();
    private readonly Dictionary<string, Color> sectorColors = new Dictionary<string, Color>();
    private GUIStyle sectorNameStyle;

    private void DrawSectorsSection()
    {
        EditorGUILayout.Space();
        GUILayout.Label($"Sectors ({level.sectors.Count})", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        float brightness = EditorGUILayout.Slider(UnpoweredBrightnessLabel, level.unpoweredBrightness, 0f, 1f);
        float saturation = EditorGUILayout.Slider(UnpoweredSaturationLabel, level.unpoweredSaturation, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(level, "Change Unpowered Look");
            level.unpoweredBrightness = Mathf.Round(brightness * 20f) / 20f;
            level.unpoweredSaturation = Mathf.Round(saturation * 20f) / 20f;
            EditorUtility.SetDirty(level);
        }

        bool show = GUILayout.Toggle(showSectors, ShowSectorsLabel, "Button");
        if (show != showSectors) SetShowSectors(show);

        for (int i = 0; i < level.sectors.Count; i++)
        {
            if (DrawSectorRow(level.sectors[i], i))
            {
                SelectBox(-1, -1);
                Undo.RecordObject(level, "Remove Sector");
                level.sectors.RemoveAt(i);
                MarkChanged(level);
                break;
            }
        }

        if (GUILayout.Button("Add Sector"))
        {
            Undo.RecordObject(level, "Add Sector");
            var sector = new MapLevel.SectorEntry { name = GetFreeSectorName(), color = GetAutoColor(level.sectors.Count) };
            level.sectors.Add(sector);
            SetOpen(openSectors, sector, true);
            MarkChanged(level);
        }
    }

    private bool DrawSectorRow(MapLevel.SectorEntry sector, int index)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            Color color = GetSectorColor(level, index);
            color.a = 1f;
            Color picked = EditorGUILayout.ColorField(SectorColorLabel, color, false, false, false, GUILayout.Width(36));
            if (picked != color)
            {
                Undo.RecordObject(level, "Change Sector Color");
                picked.a = 1f;
                sector.color = picked;
                EditorUtility.SetDirty(level);
                RefreshSectorOverlay();
                SceneView.RepaintAll();
            }

            string name = EditorGUILayout.DelayedTextField(SectorNameLabel, sector.name, GUILayout.Width(90));
            if (name != sector.name)
            {
                Undo.RecordObject(level, "Rename Sector");
                sector.name = name.Trim();
                MarkChanged(level);
            }

            if (GUILayout.Button(AddBoxLabel)) AddBox(index);
            if (GUILayout.Button("X", GUILayout.Width(22))) return true;
        }

        if (!DrawFoldout(openSectors, sector, $"{sector.boxes.Count} box(es)", 0f)) return false;

        for (int i = 0; i < sector.boxes.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(ListIndent * 2f);
                bool selected = index == selectedSector && i == selectedBox;
                GUILayout.Label($"Box {i + 1}", selected ? EditorStyles.boldLabel : EditorStyles.label);
                if (GUILayout.Button(FindBoxLabel, GUILayout.Width(44)))
                {
                    FindBox(index, i);
                }
                if (GUILayout.Button("X", GUILayout.Width(22)))
                {
                    RemoveBox(index, i);
                    break;
                }
            }
        }
        return false;
    }

    private void SetShowSectors(bool show)
    {
        showSectors = show;
        if (!show) SelectBox(-1, -1);
        overlayIsStale = true;
        Repaint();
        SceneView.RepaintAll();
    }

    private void SelectBox(int sector, int box)
    {
        if (sector == selectedSector && box == selectedBox) return;

        selectedSector = sector;
        selectedBox = sector >= 0 ? box : -1;
        if (sector >= 0)
        {
            selectedLabel = -1;
            Selection.activeObject = null;
        }
        Repaint();
        SceneView.RepaintAll();
    }

    private void AddBox(int index)
    {
        MapLevel.SectorEntry sector = level.sectors[index];
        Undo.RecordObject(level, "Add Sector Box");
        sector.boxes.Add(GetNewBox(level.transform));
        SetOpen(openSectors, sector, true);
        MarkChanged(level);
        if (!showSectors) SetShowSectors(true);
        SelectBox(index, sector.boxes.Count - 1);
    }

    private static Bounds GetNewBox(Transform root)
    {
        bool found = false;
        var box = new Bounds();
        foreach (GameObject go in Selection.gameObjects)
        {
            if (go.scene != root.gameObject.scene) continue;
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
            {
                Bounds world = renderer.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = world.center + Vector3.Scale(world.extents, new Vector3(
                        (corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
                    point = root.InverseTransformPoint(point);
                    if (!found) box = new Bounds(point, Vector3.zero);
                    else box.Encapsulate(point);
                    found = true;
                }
            }
        }
        if (found) return box;

        SceneView view = SceneView.lastActiveSceneView;
        Vector3 middle = view != null ? root.InverseTransformPoint(view.pivot) : Vector3.zero;
        Vector3 scale = root.lossyScale;
        return new Bounds(middle, new Vector3(NewBoxSize.x / scale.x, NewBoxSize.y / scale.y, NewBoxSize.z / scale.z));
    }

    private void RemoveBox(int sector, int box)
    {
        SelectBox(-1, -1);
        Undo.RecordObject(level, "Remove Sector Box");
        level.sectors[sector].boxes.RemoveAt(box);
        MarkChanged(level);
    }

    private void FindBox(int sector, int box)
    {
        if (!showSectors) SetShowSectors(true);
        SelectBox(sector, box);

        SceneView view = SceneView.lastActiveSceneView;
        if (view == null) return;
        Bounds local = level.sectors[sector].boxes[box];
        Matrix4x4 toWorld = level.transform.localToWorldMatrix;
        var world = new Bounds(toWorld.MultiplyPoint3x4(local.min), Vector3.zero);
        world.Encapsulate(toWorld.MultiplyPoint3x4(local.max));
        view.Frame(world, false);
    }

    private string GetFreeSectorName()
    {
        for (int n = level.sectors.Count + 1; ; n++)
        {
            string name = $"Sector {n}";
            if (!level.sectors.Exists(sector => MapBuilder.GetSectorName(sector) == name)) return name;
        }
    }

    private static Color GetSectorColor(MapLevel owner, int index)
    {
        Color color = owner.sectors[index].color.a > 0f ? owner.sectors[index].color : GetAutoColor(index);
        color.a = OverlayAlpha;
        return color;
    }

    private static Color GetAutoColor(int index)
    {
        return Color.HSVToRGB(Mathf.Repeat(index * 0.618034f + 0.05f, 1f), 0.8f, 1f);
    }

    private void RefreshSectorOverlay()
    {
        sectorColors.Clear();
        if (overlayLevel == null) return;

        for (int i = 0; i < overlayLevel.sectors.Count; i++)
        {
            string name = MapBuilder.GetSectorName(overlayLevel.sectors[i]);
            if (name != "" && !sectorColors.ContainsKey(name)) sectorColors[name] = GetSectorColor(overlayLevel, i);
        }
    }

    private Color GetSectorOverlayColor(MapBuilder.Placement placement)
    {
        return placement.sector != "" && sectorColors.TryGetValue(placement.sector, out Color color) ? color : NoSectorColor;
    }

    private bool ShowsSectors => showSectors && sectorColors.Count > 0;

    private void OnSectorsSceneGUI(SceneView view)
    {
        if (!showSectors || library == null) return;

        if (EditorApplication.isPlayingOrWillChangePlaymode || StageUtility.GetCurrentStageHandle() != StageUtility.GetMainStageHandle()) return;

        RefreshOverlay();
        if (overlayLevel == null) return;

        if (sectorsNeedRefresh && GUIUtility.hotControl == 0)
        {
            sectorsNeedRefresh = false;
            MarkChanged(overlayLevel);
        }

        List<MapLevel.SectorEntry> sectors = overlayLevel.sectors;
        if (selectedSector >= sectors.Count || (selectedSector >= 0 && selectedBox >= sectors[selectedSector].boxes.Count))
        {
            SelectBox(-1, -1);
        }

        if (sectorNameStyle == null)
        {
            sectorNameStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
        }

        using (new Handles.DrawingScope(overlayLevel.transform.localToWorldMatrix))
        {
            for (int s = 0; s < sectors.Count; s++)
            {
                string name = MapBuilder.GetSectorName(sectors[s]);
                Color color = GetSectorColor(overlayLevel, s);
                color.a = 1f;
                for (int b = 0; b < sectors[s].boxes.Count; b++)
                {
                    Bounds box = sectors[s].boxes[b];
                    Handles.color = color;
                    if (s == selectedSector && b == selectedBox)
                    {
                        DrawBoxHandles(sectors[s], b, color);
                    }
                    else
                    {
                        Handles.DrawWireCube(box.center, box.size);

                        float dot = HandleUtility.GetHandleSize(box.center) * 0.08f;
                        if (Handles.Button(box.center, Quaternion.identity, dot, dot * 1.5f, Handles.DotHandleCap))
                        {
                            SelectBox(s, b);
                        }
                    }

                    sectorNameStyle.normal.textColor = color;
                    Handles.Label(box.center + Vector3.up * box.extents.y, name == "" ? "(no name)" : name, sectorNameStyle);
                }
            }
        }

        HandleSelectedBoxKeys();
    }

    private void DrawBoxHandles(MapLevel.SectorEntry sector, int index, Color color)
    {
        Bounds box = sector.boxes[index];
        boxHandle.center = box.center;
        boxHandle.size = box.size;
        boxHandle.SetColor(color);

        EditorGUI.BeginChangeCheck();
        boxHandle.DrawHandle();
        Vector3 center = Handles.PositionHandle(boxHandle.center, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(overlayLevel, "Change Sector Box");
            Vector3 size = boxHandle.size;
            sector.boxes[index] = new Bounds(center, new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z)));
            EditorUtility.SetDirty(overlayLevel);
            sectorsNeedRefresh = true;
            Repaint();
        }
    }

    private void HandleSelectedBoxKeys()
    {
        if (selectedSector < 0) return;

        UnityEngine.Event current = UnityEngine.Event.current;
        bool delete = current.commandName == "SoftDelete" || current.commandName == "Delete";
        if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
        {
            SelectBox(-1, -1);
            current.Use();
        }
        else if (current.type == EventType.ValidateCommand && delete)
        {
            current.Use();
        }
        else if (current.type == EventType.ExecuteCommand && delete)
        {
            Undo.RecordObject(overlayLevel, "Remove Sector Box");
            overlayLevel.sectors[selectedSector].boxes.RemoveAt(selectedBox);
            SelectBox(-1, -1);
            MarkChanged(overlayLevel);
            current.Use();
        }
    }
}
