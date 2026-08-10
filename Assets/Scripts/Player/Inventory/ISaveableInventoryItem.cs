using UnityEngine;

public interface ISaveableInventoryItem
{
    string GetSaveData();
    void LoadSaveData(string json);
    void ClearRuntimeState();
}
