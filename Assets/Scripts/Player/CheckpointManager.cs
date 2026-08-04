using UnityEngine;
using System.Collections.Generic;
using System;

public class CheckpointManager : MonoBehaviour, ISaveable
{
    [Tooltip("Ordered list of your checkpoints in scene")]
    [SerializeField]
    public List<Checkpoint> checkpoints;
    [SerializeField] private ZeroGravity playerZeroG;    // keep as inspector fallback only
    [SerializeField] private GameObject persistentPrefab;
    [SerializeField] private Camera playerCam;
    [SerializeField] private InventoryManager inventoryManager;
    private int _currentIndex = 0;

    public int CurrentIndex
    {
        get { return _currentIndex; }
    }

    void Start()
    {
        //prefer the persistant player over whatever was dragged in this scene
        if (PersistantManager.Instance != null && PersistantManager.Instance.Player != null)
        {
            playerZeroG = PersistantManager.Instance.Player;
            playerCam = PersistantManager.Instance.MainCamera;
        }
        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }
        // Wire up each checkpoint and only enable the first one
        for (int i = 0; i < checkpoints.Count; i++)
        {
            var cp = checkpoints[i];
            cp.OnReached += HandleCheckpointReached;
            cp.Initialize(playerZeroG, i == 0);
        }
        // continue from save
        if (GlobalSaveManager.LoadFromSave)
        {
            GlobalSaveManager.LoadSavable(this, false);
            HandleRestorePlayerFromSave();
        }   
    }

    void HandleCheckpointReached(Checkpoint reached)
    {
        // advance to next checkpoint if there is one
        if (_currentIndex + 1 < checkpoints.Count)
        {
            _currentIndex++;
            checkpoints[_currentIndex].Initialize(playerZeroG, true);
        }
        // store the Player's data to the save manager, passing in the position of this checkpoint
        playerZeroG.StorePlayerData(reached.respawnPoint.transform.position);
        // save the game at checkpoints
        GlobalSaveManager.SavedWithTerminal = false;
        GlobalSaveManager.SaveGame(false);
    }

    // these are for serialization and will be created during the save
    [Serializable]
    public class CheckPointData
    {
        [SerializeField]
        private List<bool> checkpointStates;
        public List<bool> CheckpointStates
        {
            get { return checkpointStates; }
        }
        public CheckPointData(List<bool> _checkpointStates)
        {
            checkpointStates = _checkpointStates;
        }
    }

    public void LoadSaveFile(string fileName)
    {
        // this will load data from the file to a variable we will use to change this objects data
        string path = Application.persistentDataPath;
        string loadedData = GlobalSaveManager.LoadTextFromFile(path, fileName);
        if (loadedData != null && loadedData != "")
        {
            CheckPointData _checkpointData = JsonUtility.FromJson<CheckPointData>(loadedData);
            for (int i = 0; i < _checkpointData.CheckpointStates.Count; i++)
            {
                checkpoints[i].Col.enabled = _checkpointData.CheckpointStates[i];
            }
        }
    }

    public void CreateSaveFile(string fileName)
    {
        // store a copy of the checkpoint data in the global save manager
        // GlobalSaveManager.Instance.Data.Checkpoints = new List<Checkpoint>(checkpoints);
        CheckPointData _checkpointData = new CheckPointData(new List<bool>());
        foreach (Checkpoint _checkpoint in checkpoints)
        {
            _checkpointData.CheckpointStates.Add(_checkpoint.Col.enabled);
        }
        // this will create a file backing up the data we give it
        string json = JsonUtility.ToJson(_checkpointData);
        string path = Application.persistentDataPath;
        GlobalSaveManager.SaveTextToFile(path, fileName, json);
    }

    public void HandleRestorePlayerFromSave()
    {

        //clean up lingering persistent instance before creating a new one
        //if (PersistantManager.Instance != null)
        //{
        //    Debug.Log("destroying lingering persistent instance before creating a new one");
        //    Destroy(PersistantManager.Instance.gameObject);
        //    PersistantManager.Instance = null;
        //}

        //GameObject newPersistentObj = Instantiate(persistentPrefab, Vector3.zero, Quaternion.identity);

        playerZeroG = PersistantManager.Instance.Player;
        playerCam = playerZeroG.GetComponentInChildren<Camera>();
        playerZeroG.PlayerCutSceneHandler(false);

        GlobalSaveManager.LoadSavable(playerZeroG, false);

        // restore inventory state (equipped item, held/acquired items) to match this checkpoint
        if (inventoryManager != null)
        {
            GlobalSaveManager.LoadSavable(inventoryManager, false);
        }
        else
        {
            Debug.LogWarning("CheckpointManager: inventoryManager reference missing, skipping inventory restore.");
        }

        //playerZeroG.transform.position = checkpoints[_currentIndex].respawnPoint.transform.position;
        //playerCam.transform.rotation = checkpoints[_currentIndex].respawnPoint.transform.rotation;

        //Debug.Log("Restored player from save at checkpoint " + _currentIndex);
    }
}
