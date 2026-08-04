using UnityEngine;

[System.Serializable]
public class InventoryData
{
    public int currentIndex;
    public string[] itemData;

    public InventoryData() { } // required by JsonUtility.FromJson

    public InventoryData(int _currentIndex, string[] _itemData)
    {
        currentIndex = _currentIndex;
        itemData = _itemData;
    }
}

