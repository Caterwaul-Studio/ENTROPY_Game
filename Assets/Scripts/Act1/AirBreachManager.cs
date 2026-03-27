using UnityEngine;
using System.Collections.Generic;

public class AirBreachManager : MonoBehaviour, ISaveable
{
    public List<AirBreachScript> breachList;


    void Start()
    {
        if (GlobalSaveManager.LoadFromSave) GlobalSaveManager.LoadSavable(this, false);
    }


    public class AirBreachData
    {
        public List<bool> breachPoweredStates;

        public AirBreachData(List<bool> poweredStates)
        {
            breachPoweredStates = poweredStates;
        }
    }
    /// <summary>
    /// Save and Load methods simply store whether or not the air breach is powered on.
    /// </summary>
    /// <param name="fileName"></param>
    public void LoadSaveFile(string fileName)
    {
        string path = Application.persistentDataPath;
        string loadedData = GlobalSaveManager.LoadTextFromFile(path, fileName);
        if (loadedData != null && loadedData != "")
        {
            AirBreachData _airBreachData = JsonUtility.FromJson<AirBreachData>(loadedData);
            for (int i = 0; i < _airBreachData.breachPoweredStates.Count; i++)
            {
                breachList[i].IsPoweredOn = _airBreachData.breachPoweredStates[i];
            }
        }
    }

    public void CreateSaveFile(string fileName)
    {
        AirBreachData _airBreachData = new AirBreachData(new List<bool>());
        foreach (AirBreachScript _breach in breachList)
        {
            _airBreachData.breachPoweredStates.Add(_breach.IsPoweredOn);
        }

        string json = JsonUtility.ToJson(_airBreachData);
        string path = Application.persistentDataPath;
        GlobalSaveManager.SaveTextToFile(path, fileName, json);
    }


}
