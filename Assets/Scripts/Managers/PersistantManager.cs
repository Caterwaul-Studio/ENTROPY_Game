using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class PersistantManager : MonoBehaviour
{
    [SerializeField] private GameObject playerObject;
    public GameObject PlayerObject => playerObject;

    [SerializeField] private ZeroGravity player;
    public ZeroGravity Player => player;

    [SerializeField] private WristMonitor wristMonitor;
    public WristMonitor WristMonitor => wristMonitor;

    [SerializeField] private Camera mainCamera;
    public Camera MainCamera => mainCamera;

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
