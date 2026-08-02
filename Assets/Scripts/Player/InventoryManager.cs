using UnityEngine;
using System.Collections;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] public IInventoryItem[] slots = new IInventoryItem[4];
    private int currentIndex = 0;

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
}
