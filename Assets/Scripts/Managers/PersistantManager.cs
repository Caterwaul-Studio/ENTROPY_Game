using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class PersistantManager : MonoBehaviour
{
    [SerializeField] private ZeroGravity player;
    public ZeroGravity Player => player;

    //private ObjectiveUpdate objectiveUpdate;
    private CheckpointManager checkpointManager;
    public GameObject persistentObj;

    public static PersistantManager Instance { get; private set; }

    public void OnEnable()
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
        
    private void OnSceneUnloaded(Scene scene)
    {
        //player = null; // clears the destroyed reference
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        checkpointManager = FindFirstObjectByType<CheckpointManager>();

        //if the player selects last checkpoint/ the GSM is called, destroy the persistent object
        if (GlobalSaveManager.LoadFromSave && checkpointManager != null)
        {
            //GlobalSaveManager.LoadFromSave = false;
            //GlobalSaveManager.LoadSavable(checkpointManager, true);
            Debug.Log("restarted from last checkpoint");

            Instance = null;
            Destroy(gameObject);
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
            Destroy(gameObject);
            Debug.Log("Destroyed persistent object because the current scene is the main menu.");
            return;
        }
    }
}
