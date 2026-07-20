using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// This script is used to mark the TutorialManager as persistent across scene reloads in level 1. 
/// </summary>

public class DialogueTutorialPersistence : MonoBehaviour
{
    public static DialogueTutorialPersistence Instance;

    [Tooltip("Scene names this object is allowed to persist through. Loading any scene NOT in this list destroys this object.")]
    public string[] level1SceneNames;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Duplicate came in with a reloaded/re-entered scene - the original
            // persistent instance already holds the live tutorial/dialogue state.
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Update()
    {
        //if the current scene is the main menu, destroy the persistent object
        if (SceneManager.GetActiveScene().name != "Level1New")
        {
            Instance = null;
            Destroy(gameObject);
            //Debug.Log("Destroyed persistent object because the current scene is the main menu.");
            return;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (level1SceneNames == null || level1SceneNames.Length == 0) return;

        bool stillInLevel1 = System.Array.IndexOf(level1SceneNames, scene.name) >= 0;
        if (!stillInLevel1)
        {
            UnityEngine.Debug.Log($"Left Level 1 (loaded '{scene.name}'). Destroying persistent Dialogue&Tutorial.");
            Destroy(gameObject);
        }
    }
}
