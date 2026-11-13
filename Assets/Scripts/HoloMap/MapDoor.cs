using UnityEngine;

// What a door shows on the map
public enum MapDoorState { Working = 0, Broken = 1, Locked = 2 }

/// <summary>
/// Marks a door's panel on the baked hologram map. Its frame is drawn with the rooms in the usual color.
/// MapDoors on the map's Doors group keeps the panel's color matching the real door.
/// </summary>
public class MapDoor : MonoBehaviour
{
    [Tooltip("This door's place in the DoorManager's list, the same numbering its save data uses. -1 if it isn't listed.")]
    public int index = -1;

    [Tooltip("What the door shows now. Baked from the door's state in the scene.")]
    public MapDoorState state;

    [Tooltip("The pieces that change color with the door's state.")]
    public MeshRenderer[] pieces = new MeshRenderer[0];

    public void Show(MapDoorState newState, Material[] materials)
    {
        state = newState;
        foreach (MeshRenderer piece in pieces)
        {
            if (piece != null) piece.sharedMaterials = materials;
        }
    }
}
