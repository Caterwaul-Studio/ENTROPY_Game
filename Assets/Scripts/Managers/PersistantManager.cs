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
}
