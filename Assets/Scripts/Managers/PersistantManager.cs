using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class PersistantManager : MonoBehaviour
{
    private ZeroGravity player;

    private ObjectiveUpdate objectiveUpdate;

    public void OnEnable()
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        //SceneManager.sceneUnloaded += OnSceneUnloaded;
    }
        
    private void OnSceneUnloaded(Scene scene)
    {
        player = null; // clears the destroyed reference
    }

    public static PersistantManager Instance { get; private set; }

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            // if persistent player already exists, destroy duplicate
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        //if the current scene is the main menu, destroy the persistent object
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            Destroy(gameObject);
            Debug.Log("Destroyed persistent object because the current scene is the main menu.");
        }
    }
}
