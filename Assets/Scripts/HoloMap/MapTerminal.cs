using UnityEngine;

/// <summary>
/// Marks a terminal on the baked hologram map. The Map Builder adds it, so the map can later show
/// each terminal's state, like which ones are active.
/// </summary>
public class MapTerminal : MonoBehaviour
{
    [Tooltip("This terminal's place in the level's TerminalManager list, the same numbering its save data uses. -1 if it isn't listed.")]
    public int index = -1;
}
