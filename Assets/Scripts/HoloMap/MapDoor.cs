using UnityEngine;

public enum MapDoorState { Working = 0, Broken = 1, Locked = 2 }

public class MapDoor : MonoBehaviour
{
    public int index = -1;
    public MapDoorState state;
    public MeshRenderer[] pieces = new MeshRenderer[0];
    public string panelPath = "";

    public void Show(MapDoorState newState, Material[] materials)
    {
        state = newState;
        foreach (MeshRenderer piece in pieces)
        {
            if (piece != null) piece.sharedMaterials = materials;
        }
    }
}
