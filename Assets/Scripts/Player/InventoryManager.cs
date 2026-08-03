using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] public IInventoryItem[] slots = new IInventoryItem[4];
    private int currentIndex = 0;

    [SerializeField] private int heldLayer = 8; // Layer for held objects
    [SerializeField] private int floatingObjLayer = 9; // Default layer for floating objects
    [SerializeField] private int iInteractableLayer = 10; // Layer for interactable objects

    [SerializeField] public Flashlight flashlight;
    [SerializeField] public FireExtinguisher fireExtinguisher;
    [SerializeField] public HeldFloatingObject heldFloatingObject;
    [SerializeField] public PickupScript pickupScript;

    //Cache the original emission colors of materials to restore them when unequipping
    private Dictionary<Material, Color> _originalEmissionColors = new Dictionary<Material, Color>();

    public int HeldLayer => heldLayer;
    public int FloatingObjLayer => floatingObjLayer;
    public int IInteractableLayer => iInteractableLayer;

    public void RegisterSlot(int index, IInventoryItem item)
    {
        if (item == null)
        {
            Debug.LogWarning($"InventoryManager: tried to register null item at slot {index}");
            return;
        }

        slots[index] = item;
    }

    public void RequestActivate(int index)
    {
        if (index < 0 || index >= slots.Length || slots[index] == null)
        {
            Debug.LogWarning($"InventoryManager: no item registered at slot {index}");
            return;
        }

        if (index == currentIndex) return;

        if(currentIndex != -1 && slots[currentIndex] != null)
        {
            slots[currentIndex].Unequip();
        }
        currentIndex = index;
        slots[currentIndex].Equip();
    }

    public void DeactivateCurrent()
    {
        if (currentIndex != -1 && slots[currentIndex] != null)
        {
            slots[currentIndex].Unequip();
        }
        currentIndex = -1;
    }

    public void SetChildrenToHoldLayer(GameObject parent)
    {
        parent.gameObject.layer = heldLayer; // Set the parent object to the held layer

        foreach (Transform child in parent.transform)
        {
            //this guard prevents any billboard objects childed from being reassigned
            if (child.gameObject.GetComponent<Canvas>() != null)
            {
                continue;
            }

            child.gameObject.layer = heldLayer;
            SetChildrenToHoldLayer(child.gameObject);

            MeshRenderer mr = child.gameObject.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                foreach (Material mat in mr.materials) // instances per-renderer material to avoid changing the original material
                {
                    if (!_originalEmissionColors.ContainsKey(mat))
                    {
                        _originalEmissionColors[mat] = mat.GetColor("_EmissionColor");
                    }
                    mat.DisableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.black); // set emission color to black to disable emission)
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack; // ensure that the emission is treated as black for GI
                }
            }
        }
    }

    public void SetChildrenToDefaultLayer(GameObject parent, int layer)
    {
        parent.gameObject.layer = layer; // Set the parent object to the held layer

        foreach (Transform child in parent.transform)
        {
            //this guard prevents any billboard objects childed from being reassigned
            if (child.gameObject.GetComponent<Canvas>() != null)
            {
                continue;
            }

            child.gameObject.layer = layer;
            SetChildrenToDefaultLayer(child.gameObject, layer);

            MeshRenderer mr = child.gameObject.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                foreach (Material mat in mr.materials)
                {
                    mat.EnableKeyword("_EMISSION");
                    //restore original emission color if it was cached
                    if (_originalEmissionColors.TryGetValue(mat, out Color originalColor))
                    {
                        mat.SetColor("_EmissionColor", originalColor);
                    }

                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; // reset GI flags to default
                }
            }
        }
    }
}
