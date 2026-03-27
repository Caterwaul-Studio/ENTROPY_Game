using System.Collections.Generic;
using UnityEngine;

public class TriggerManager : MonoBehaviour, ISaveable
{
    public List<GlobalTrigger> TriggerList;

    void Start()
    {
        if (GlobalSaveManager.LoadFromSave) GlobalSaveManager.LoadSavable(this, false);
    }

    public class TriggerData
    {
        public List<bool> triggerStates;

        public TriggerData(List<bool> states)
        {
            triggerStates = states;
        }
    }

    public void LoadSaveFile(string fileName)
    {
        string path = Application.persistentDataPath;
        string loadedData = GlobalSaveManager.LoadTextFromFile(path, fileName);
        if (loadedData != null && loadedData != "")
        {
            TriggerData _triggerData = JsonUtility.FromJson<TriggerData>(loadedData);
            for (int i = 0; i < _triggerData.triggerStates.Count; i++)
            {
                TriggerList[i].isTriggered = _triggerData.triggerStates[i];

                // If the trigger is triggered and should recall on scene load, execute the behavior
                if (TriggerList[i].isTriggered)
                {
                    if (TriggerList[i].recallUponSceneLoad)
                    {
                        TriggerList[i].RecallTriggeredBehavior();
                    }
                    TriggerList[i].Deactivate();


                }
            }
        }
    }

    public void CreateSaveFile(string fileName)
    {
        TriggerData _triggerData = new TriggerData(new List<bool>());
        foreach (GlobalTrigger _trigger in TriggerList)
        {
            _triggerData.triggerStates.Add(_trigger.isTriggered);
        }

        string json = JsonUtility.ToJson(_triggerData);
        string path = Application.persistentDataPath;
        GlobalSaveManager.SaveTextToFile(path, fileName, json);
    }

}
