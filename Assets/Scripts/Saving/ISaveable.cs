using UnityEngine;

public interface ISaveable
{
    void LoadSaveFile(string fileName);
    void CreateSaveFile(string fileName);
}
