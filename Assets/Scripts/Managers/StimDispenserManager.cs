using UnityEngine;
using System.Collections.Generic;

public class StimDispenserManager : MonoBehaviour, ISaveable
{
    public StimDispenser currentStimDispenser;

    public List<StimDispenser> dispensers;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (GlobalSaveManager.LoadFromSave) GlobalSaveManager.LoadSavable(this, false);
    }

    public class StimDispenserData
    {
        public List<bool> dispenserUsableStates;

        public StimDispenserData(List<bool> usableStates)
        {
            dispenserUsableStates = usableStates;
        }
    }

    public void LoadSaveFile(string fileName)
    {
        string path = Application.persistentDataPath;
        string loadedData = GlobalSaveManager.LoadTextFromFile(path, fileName);
        if (loadedData != null && loadedData != "")
        {
            StimDispenserData _stimDispenserData = JsonUtility.FromJson<StimDispenserData>(loadedData);
            for (int i = 0; i < _stimDispenserData.dispenserUsableStates.Count; i++)
            {
                dispensers[i].ToggleUsability(_stimDispenserData.dispenserUsableStates[i]); 
            }
        }
    }

    public void CreateSaveFile(string fileName)
    {
        StimDispenserData _stimDispenserData = new StimDispenserData(new List<bool>());
        foreach (StimDispenser _dispenser in dispensers)
        {
            _stimDispenserData.dispenserUsableStates.Add(_dispenser.IsUsable);
        }

        string json = JsonUtility.ToJson(_stimDispenserData);
        string path = Application.persistentDataPath;
        GlobalSaveManager.SaveTextToFile(path, fileName, json);
    }


}
