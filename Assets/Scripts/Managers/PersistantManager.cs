using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class PersistantManager : MonoBehaviour
{
    [SerializeField] private ZeroGravity player;
    public ZeroGravity Player => player;

    //private ObjectiveUpdate objectiveUpdate;
    private CheckpointManager checkpointManager;
    private MenuManager menuManager;
    public GameObject persistentObj;

    public static PersistantManager Instance { get; set; }

    public void OnEnable()
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void OnDisable()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneUnloaded(Scene scene)
    {
        //player = null; // clears the destroyed reference
        //Instance = null;
        //Destroy(gameObject);
        //^ this is unnecessary because the persistent object is not destroyed when the scene is unloaded
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        checkpointManager = FindFirstObjectByType<CheckpointManager>();
        menuManager = FindFirstObjectByType<MenuManager>();
        //if the player selects last checkpoint/ the GSM is called, destroy the persistent object
        if (GlobalSaveManager.lastCheckpointSelected == true)
        {
            Debug.Log("restarted from last checkpoint using the flag: " + GlobalSaveManager.lastCheckpointSelected);
            Instance = null;
            Destroy(gameObject);

            GlobalSaveManager.lastCheckpointSelected = false; // reset the flag
            Debug.Log("Last checkpoint selected, flag set to: " + GlobalSaveManager.lastCheckpointSelected);
        return;
        }
    }

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            // if persistent player already exists, destroy duplicate
            Destroy(gameObject);
            return;
        }
        
        Instance = this;

        if(player == null)
        {
            player = GetComponentInChildren<ZeroGravity>();
        }

        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        //if the current scene is the main menu, destroy the persistent object
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            Instance = null;
            Destroy(gameObject);
            //Debug.Log("Destroyed persistent object because the current scene is the main menu.");
            return;
        }
    }
}
