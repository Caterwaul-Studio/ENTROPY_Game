using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// The Map Builder's labels: the Labels list in the window, and placing, picking and dragging them in the Scene view.
/// </summary>
public partial class MapBuilderWindow
{
    const float MinLabelPixels = 8f;
    const float MaxLabelPixels = 64f;
    const float MinLabelSize = 0.25f;
    const float MaxLabelSize = 5f;

    static readonly int PlaceHint = "MapBuilderPlaceLabel".GetHashCode();
    static readonly int LabelTextHint = "MapBuilderLabelText".GetHashCode();

    // Just under the 5 pixels Unity gives empty-space clicks, so label text beats box selection,
    // while any real handle near the mouse, like a selected object's move arrows, still wins
    const float LabelTextPickDistance = 4.9f;
    static readonly Color LabelShadowColor = new Color(0f, 0f, 0f, 0.8f);
    static readonly Color SceneLabelColor = new Color(0.85f, 0.95f, 1f, 1f);
    static readonly GUIContent LabelSizeLabel = new GUIContent("Label Size (m)", "Label text size in metres at full map size, about the height of one line.");
    static readonly GUIContent LabelFontLabel = new GUIContent("Label Font", "A TextMesh Pro font asset from the game, like Oxanium-Regular SDF. Its glow and colors are used as they are. Shared by every level. Empty: TextMesh Pro's default font.");
    static readonly GUIContent FindButton = new GUIContent("Find", "Show this label in the Scene view and select it. Drag its arrows to move it.");

    private bool placingLabels;
    private bool toolsWereHidden;
    private int selectedLabel = -1;
    private Transform hoverSource;
    private GUIStyle sceneLabelStyle;
    private readonly GUIContent sceneLabelContent = new GUIContent();

    private void SetPlacing(bool placing)
    {
        if (placing == placingLabels) return;

        // Hiding the move tool stops it covering the rooms being clicked
        placingLabels = placing;
        if (placing)
        {
            toolsWereHidden = Tools.hidden;
            Tools.hidden = true;
            overlayIsStale = true;
        }
        else
        {
            Tools.hidden = toolsWereHidden;
            hoverSource = null;
        }
        Repaint();
        SceneView.RepaintAll();
    }

    /// <summary>
    /// Selects a label for its move arrows. Selected objects are cleared, or their own move tool shows as well.
    /// </summary>
    private void SelectLabel(int index)
    {
        selectedLabel = index;
        Selection.activeObject = null;
        Repaint();
    }

    private void MarkLabelsChanged(MapLevel owner)
    {
        EditorUtility.SetDirty(owner);
        Repaint();
        SceneView.RepaintAll();
    }

    private bool IsHighlighted(Transform source)
    {
        if (source == hoverSource) return true;
        if (overlayLevel == null || selectedLabel < 0 || selectedLabel >= overlayLevel.labels.Count) return false;

        GameObject anchor = overlayLevel.labels[selectedLabel].anchor;
        return anchor != null && anchor.transform == source;
    }

    // ---- The Labels list in the window

    private void DrawLabelsSection()
    {
        EditorGUILayout.Space();
        GUILayout.Label($"Labels ({level.labels.Count})", EditorStyles.boldLabel);

        // Only when it's moved, so drawing never rewrites a value typed in the Inspector
        EditorGUI.BeginChangeCheck();
        float size = EditorGUILayout.Slider(LabelSizeLabel, level.labelSize, MinLabelSize, MaxLabelSize);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(level, "Change Label Size");
            level.labelSize = Mathf.Round(size * 10f) / 10f;
            MarkLabelsChanged(level);
        }

        // The fonts are shared by every level, so they live in the library
        var font = (TMP_FontAsset)EditorGUILayout.ObjectField(LabelFontLabel, library.labelFont, typeof(TMP_FontAsset), false);
        if (font != library.labelFont)
        {
            Undo.RecordObject(library, "Change Label Font");
            library.labelFont = font;
            MarkChanged(library);
        }

        if (level.labels.Count == 0)
        {
            EditorGUILayout.HelpBox("No labels yet. Press Place Labels, then click rooms in the Scene view.", MessageType.None);
            return;
        }

        for (int i = 0; i < level.labels.Count; i++)
        {
            if (DrawLabelRow(i))
            {
                RemoveLabelLater(level, i);
                break;
            }
        }
    }

    /// <summary>
    /// Removing a row moves the rows below it up, so a half-typed name would land on the next label.
    /// The name field lets go first, and the row goes once that has finished.
    /// </summary>
    private void RemoveLabelLater(MapLevel owner, int index)
    {
        GUI.FocusControl(null);
        EditorApplication.delayCall += () =>
        {
            if (owner == null || index >= owner.labels.Count) return;

            Undo.RecordObject(owner, "Remove Map Label");
            owner.labels.RemoveAt(index);
            if (selectedLabel == index) selectedLabel = -1;
            else if (selectedLabel > index) selectedLabel--;
            MarkLabelsChanged(owner);
        };
    }

    /// <summary>
    /// Draws one label row. Returns true if its remove button was pressed.
    /// </summary>
    private bool DrawLabelRow(int index)
    {
        MapLevel.LabelEntry label = level.labels[index];
        bool lost = !MapBuilder.GetLabelPosition(label, level.transform, out Vector3 world);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(lost))
            {
                if (GUILayout.Button(FindButton, GUILayout.Width(40)))
                {
                    FindLabel(index, world);
                }
            }

            if (lost)
            {
                var warning = new GUIContent(EditorGUIUtility.IconContent("console.warnicon.sml").image,
                    "The object this label sat over was deleted. Give it a new Anchor, or remove it.");
                GUILayout.Label(warning, GUILayout.Width(20));
            }

            // Delayed, so a word typed is one undo step instead of one per letter
            string text = EditorGUILayout.DelayedTextField(label.text);
            if (text != label.text)
            {
                Undo.RecordObject(level, "Rename Map Label");
                label.text = text;
                MarkLabelsChanged(level);
            }

            var anchor = (GameObject)EditorGUILayout.ObjectField(label.anchor, typeof(GameObject), true, GUILayout.Width(140));
            if (anchor != label.anchor)
            {
                SetAnchor(label, anchor, lost, world);
            }

            if (GUILayout.Button("X", GUILayout.Width(22))) return true;
        }
        return false;
    }

    /// <summary>
    /// A label keeps its spot when it gets a new anchor. One that had lost its anchor moves over the new one.
    /// </summary>
    private void SetAnchor(MapLevel.LabelEntry label, GameObject anchor, bool lost, Vector3 world)
    {
        if (anchor != null && anchor.scene != level.gameObject.scene)
        {
            report.Clear();
            report.Add($"{anchor.name} can't be a label's anchor: it has to be an object in the level's scene.");
            return;
        }

        Undo.RecordObject(level, "Change Label Anchor");
        label.anchor = anchor;
        if (lost && anchor != null) world = MapBuilder.GetLabelSpot(anchor.transform, level.transform);
        MapBuilder.SetLabelPosition(label, level.transform, world);
        MarkLabelsChanged(level);
    }

    private void FindLabel(int index, Vector3 world)
    {
        if (!showOverlay)
        {
            showOverlay = true;
            overlayIsStale = true;
        }

        // Before selecting, so a refresh that finds a new level can't clear the selection afterwards
        RefreshOverlay();
        SelectLabel(index);

        SceneView view = SceneView.lastActiveSceneView;
        if (view != null) view.Frame(new Bounds(world, Vector3.one * level.labelSize * 4f), false);
        SceneView.RepaintAll();
    }

    // ---- Placing a label

    /// <summary>
    /// Adds a label over a room or terminal, named after it, or at an exact spot. Returns the new label's index.
    /// </summary>
    private int AddLabel(MapLevel owner, Transform piece, Vector3? spot)
    {
        var label = new MapLevel.LabelEntry
        {
            text = piece != null ? MapBuilder.GetLabelText(piece.name) : "Label",
            anchor = piece != null ? piece.gameObject : null,
        };

        Undo.RecordObject(owner, "Add Map Label");
        MapBuilder.SetLabelPosition(label, owner.transform, spot ?? MapBuilder.GetLabelSpot(piece, owner.transform));
        owner.labels.Add(label);
        SelectLabel(owner.labels.Count - 1);
        MarkLabelsChanged(owner);
        return selectedLabel;
    }

    /// <summary>
    /// The room under the mouse: the nearest room holding whatever Unity picks there.
    /// Null over a terminal or door, since they don't get labels.
    /// </summary>
    private Transform PickPiece(Vector2 mouse)
    {
        GameObject picked = HandleUtility.PickGameObject(mouse, false);
        return IsOnTerminal(picked) ? null : FindHolder(picked, pieces);
    }

    private bool IsOnTerminal(GameObject picked)
    {
        return FindHolder(picked, markerParts) != null;
    }

    private static Transform FindHolder(GameObject picked, HashSet<Transform> holders)
    {
        for (Transform t = picked != null ? picked.transform : null; t != null; t = t.parent)
        {
            if (holders.Contains(t)) return t;
        }
        return null;
    }

    private void PlaceLabel(Vector2 mouse, bool exactSpot)
    {
        // Terminals and doors stand out by their fill, so clicking one adds no label
        if (IsOnTerminal(HandleUtility.PickGameObject(mouse, false))) return;

        Transform piece = PickPiece(mouse);
        Vector3? spot = null;
        if (exactSpot || piece == null)
        {
            // Clicking something else labels that spot, and clicking empty space does nothing
            if (!HandleUtility.PlaceObject(mouse, out Vector3 hit, out _)) return;
            spot = hit + overlayLevel.transform.up * MapBuilder.LabelGap;
        }
        AddLabel(overlayLevel, piece, spot);
    }

    // ---- Scene view

    private void OnLabelsSceneGUI(SceneView view)
    {
        if (!DrawsInScene || library == null) return;

        // Labels belong to the open level, not a prefab being edited or a game being played
        if (EditorApplication.isPlayingOrWillChangePlaymode || StageUtility.GetCurrentStageHandle() != StageUtility.GetMainStageHandle())
        {
            SetPlacing(false);
            return;
        }

        RefreshOverlay();
        if (overlayLevel == null)
        {
            SetPlacing(false);
            return;
        }

        DrawSceneLabels(view);
        if (placingLabels) HandlePlacing(view);
    }

    private void DrawSceneLabels(SceneView view)
    {
        Event current = Event.current;
        Camera camera = view.camera;
        int clicked = -1;
        for (int i = 0; i < overlayLevel.labels.Count; i++)
        {
            MapLevel.LabelEntry label = overlayLevel.labels[i];
            if (string.IsNullOrWhiteSpace(label.text) || !MapBuilder.GetLabelPosition(label, overlayLevel.transform, out Vector3 world)) continue;
            if (camera.WorldToViewportPoint(world).z <= 0f) continue;

            Rect rect = GetSceneLabelRect(label.text.Trim(), world, camera, i == selectedLabel);
            int textId = GUIUtility.GetControlID(LabelTextHint, FocusType.Passive);
            switch (current.GetTypeForControl(textId))
            {
                case EventType.Repaint:
                    DrawSceneLabel(rect);
                    break;

                case EventType.Layout:
                    // Clicking a label's text selects it, except while placing, which keeps clicks for the rooms.
                    // Later labels win ties, so the one drawn on top gets the click
                    if (!placingLabels && rect.Contains(current.mousePosition)) HandleUtility.AddControl(textId, LabelTextPickDistance);
                    break;

                case EventType.MouseDown:
                    if (current.button == 0 && !current.alt && HandleUtility.nearestControl == textId) clicked = i;
                    break;
            }

            // A dot to select the label by
            float dot = HandleUtility.GetHandleSize(world) * 0.06f;
            using (new Handles.DrawingScope(i == selectedLabel ? Color.white : SceneLabelColor))
            {
                if (Handles.Button(world, Quaternion.identity, dot, dot * 1.5f, Handles.DotHandleCap))
                {
                    SelectLabel(i);
                }
            }

            // Placing keeps clicks free for the next room, so the arrows only show when it's done
            if (i == selectedLabel && !placingLabels)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.PositionHandle(world, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(overlayLevel, "Move Map Label");
                    MapBuilder.SetLabelPosition(label, overlayLevel.transform, moved);
                    MarkLabelsChanged(overlayLevel);
                }
            }
        }

        // Checked after the arrows, so grabbing one of this label's arrows still wins
        if (clicked >= 0 && current.type == EventType.MouseDown)
        {
            SelectLabel(clicked);
            current.Use();
        }

        if (!placingLabels && selectedLabel >= 0 && current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
        {
            selectedLabel = -1;
            current.Use();
            Repaint();
        }
    }

    /// <summary>
    /// Where a label's text goes on screen, at the size it will be on the map, so it shows how labels fit together.
    /// Also sets up the style and text that DrawSceneLabel draws.
    /// </summary>
    private Rect GetSceneLabelRect(string text, Vector3 world, Camera camera, bool selected)
    {
        Vector2 bottom = HandleUtility.WorldToGUIPoint(world);
        Vector2 top = HandleUtility.WorldToGUIPoint(world + camera.transform.up * overlayLevel.labelSize);
        float pixels = Mathf.Clamp(Vector2.Distance(bottom, top), MinLabelPixels, MaxLabelPixels);

        if (sceneLabelStyle == null)
        {
            sceneLabelStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.LowerCenter };
        }
        sceneLabelStyle.fontSize = Mathf.RoundToInt(pixels);
        sceneLabelStyle.normal.textColor = selected ? Color.white : SceneLabelColor;

        sceneLabelContent.text = text;
        Vector2 size = sceneLabelStyle.CalcSize(sceneLabelContent);
        return new Rect(bottom.x - size.x * 0.5f, bottom.y - size.y, size.x, size.y);
    }

    /// <summary>
    /// Draws the text set up by GetSceneLabelRect. A slight shadow keeps it readable over bright walls, with no box behind it.
    /// </summary>
    private void DrawSceneLabel(Rect rect)
    {
        Color color = sceneLabelStyle.normal.textColor;
        Handles.BeginGUI();
        sceneLabelStyle.normal.textColor = LabelShadowColor;
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), sceneLabelContent, sceneLabelStyle);
        sceneLabelStyle.normal.textColor = color;
        GUI.Label(rect, sceneLabelContent, sceneLabelStyle);
        Handles.EndGUI();
    }

    private void HandlePlacing(SceneView view)
    {
        Event current = Event.current;
        int id = GUIUtility.GetControlID(PlaceHint, FocusType.Passive);
        EditorGUIUtility.AddCursorRect(new Rect(0f, 0f, view.position.width, view.position.height), MouseCursor.ArrowPlus);

        switch (current.GetTypeForControl(id))
        {
            case EventType.Layout:
                // Clicks that hit nothing else come here instead of selecting objects
                HandleUtility.AddDefaultControl(id);
                break;

            case EventType.MouseMove:
                // The piece under the mouse shows white, so it's the one a click will label.
                // Over a label's dot the click selects that label instead, so nothing shows white
                Transform hovered = HandleUtility.nearestControl == id ? PickPiece(current.mousePosition) : null;
                if (hovered != hoverSource)
                {
                    hoverSource = hovered;
                    view.Repaint();
                }
                break;

            case EventType.MouseLeaveWindow:
                if (hoverSource != null)
                {
                    hoverSource = null;
                    view.Repaint();
                }
                break;

            case EventType.MouseDown:
                // Alt and the other buttons still move the camera
                if (current.button == 0 && !current.alt && HandleUtility.nearestControl == id)
                {
                    GUIUtility.hotControl = id;
                    PlaceLabel(current.mousePosition, current.shift);
                    current.Use();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == id)
                {
                    GUIUtility.hotControl = 0;
                    current.Use();
                }
                break;

            case EventType.KeyDown:
                if (current.keyCode == KeyCode.Escape)
                {
                    SetPlacing(false);
                    current.Use();
                }
                break;
        }
    }
}
