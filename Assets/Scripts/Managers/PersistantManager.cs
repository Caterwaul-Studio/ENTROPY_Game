using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class PersistantManager : MonoBehaviour
{
    [SerializeField] private GameObject playerObject;
    public GameObject PlayerObject => playerObject;

    [SerializeField] private ZeroGravity player;
    public ZeroGravity Player => player;

    [SerializeField] public InventoryManager InventoryManager;

    [SerializeField] public PlayerUIManager PlayerUIManager;

    [SerializeField] public WristMonitor WristMonitor;

    [SerializeField] public TutorialCanvases TutorialCanvases;

    [SerializeField] private Camera mainCamera;
    public Camera MainCamera => mainCamera;

    //[SerializeField] private Transform holdPos;
    //public Transform HoldPos => holdPos;

    //private ObjectiveUpdate objectiveUpdate;
    //private CheckpointManager checkpointManager;
    //private MenuManager menuManager;
    public GameObject persistentObj;

    //temporary bool to allow the Geist to spawn on a timer if the geist spawn trigger was used
    //---------------------------------------------------------- remove this once better spawn/ persistent logic 
    // exists for the Geist
    public bool GeistAlrSpawned = false;

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

    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        
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

        //ensure the player is able to use the wrist monitor in any scene except for Level1New, where the player does not have a wrist monitor yet.
        if (SceneManager.GetActiveScene().name != "Level1New")
        {
            WristMonitor.HasWristMonitor = true;
            return;
        }
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
