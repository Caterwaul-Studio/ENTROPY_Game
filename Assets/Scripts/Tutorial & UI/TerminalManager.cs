using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TerminalManager : MonoBehaviour, ISaveable
{

    public Terminal currentTerminal;
    
    
    public Terminal CurrentTerminal
    {
        get { return currentTerminal; }
        set { currentTerminal = value; }
    }
    [SerializeField]
    private List<Terminal> terminals;

    void Start()
    {
        // continue from save
        if (GlobalSaveManager.LoadFromSave) GlobalSaveManager.LoadSavable(this, false);
    }
    public void OnInteract(InputAction.CallbackContext context)
    {
        
        if (currentTerminal != null)
        {
            if (context.performed && currentTerminal.isLookedAt && !currentTerminal.isActivated)
            {
                currentTerminal.Activation();
                // store a copy of the Player's data, passing in the position of this checkpoint
                currentTerminal.PlayerScript.StorePlayerData(currentTerminal.TargetTransform.transform.position);
                // save the game at checkpoints
                //GlobalSaveManager.Instance.Data.SavedWithTerminal = true;
                //GlobalSaveManager.Instance.CreateTempSaveFile();
                //GlobalSaveManager.Instance.CreatePersistantSaveFile();
                GlobalSaveManager.SavedWithTerminal = true;
                GlobalSaveManager.SaveGame(true);
            }
        }
    }
    // these are for serialization and will be created during the save
    [Serializable]
    public class TerminalData
    {
        [SerializeField]
        private List<bool> terminalStates;
        public List<bool> TerminalStates
        {
            get { return terminalStates; }
        }
        [SerializeField]
        private int latestTerminalIndex;
        public int LatestTerminalIndex
        {
            get { return latestTerminalIndex; }
            set {  latestTerminalIndex = value; }
        }
        public TerminalData(List<bool> _terminalStates, int _latestTerminalIndex)
        {
            terminalStates = _terminalStates;
            latestTerminalIndex = _latestTerminalIndex;
        }
    }
    public void LoadSaveFile(string fileName)
    {
        // this will load data from the file to a variable we will use to change this objects data
        string path = Application.persistentDataPath;
        string loadedData = GlobalSaveManager.LoadTextFromFile(path, fileName);
        if (loadedData != null && loadedData != "")
        {
            TerminalData _terminalData = JsonUtility.FromJson<TerminalData>(loadedData);
            // activates all of the terminals, only the current terminal will play its cutscene
            for (int i = 0; i < _terminalData.TerminalStates.Count; i++)
            {
                if (_terminalData.TerminalStates[i])
                {
                    if (i == _terminalData.LatestTerminalIndex)
                    {
                        terminals[i].MediumActivation();
                        //Debug.Log("Loaded with medium activation");
                    }
                    else
                    {
                        terminals[i].SoftActivation();
                        //Debug.Log("Loaded with soft activation");
                    }
                }
            }
        }
    }

    public void CreateSaveFile(string fileName)
    {
        // store a copy of the terminal data in the global save manager
        TerminalData _terminalData = new TerminalData(new List<bool>(), -1);
        for (int i = 0; i < terminals.Count; i++)
        {
            _terminalData.TerminalStates.Add(terminals[i].isActivated);
            // track the index of the current terminal for save loading
            if (terminals[i] == CurrentTerminal)
            {
                _terminalData.LatestTerminalIndex = i;
            }
        }
        // this will create a file backing up the data we give it
        string json = JsonUtility.ToJson(_terminalData);
        string path = Application.persistentDataPath;
        GlobalSaveManager.SaveTextToFile(path, fileName, json);
    }

    //What you can do is move the onInteract into this script so that you don't have to give the player input an OnInteract for every single terminal in the game.
    //Then have a reference for currentTerminal in here and update what the currently viewed terminal is by passing a reference of the terminal you're looking at to this script in the UIManager.
    //OnInteract should activate the terminal sequence of the currentTerminal (and it should only do this once, terminals need to be marked as used and you should be checking to see if the terminal is used before displaying the UI)
    //Things like sound effects will be called from the terminal script so that it can easily be localized to that terminal's audio source.
    //if you want the interaction to be a hold like on stim dispenser you can look at the stim dispenser script to see how I did it, possibly use the same UI element as I did for the progress bar.

}
