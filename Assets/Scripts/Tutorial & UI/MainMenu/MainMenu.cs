using System.Collections;
using System.Collections.Generic;
using System.Xml.XPath;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private SettingsMenu settingsMenu;
    [SerializeField] private GameObject confirmationPopUp;
    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private Volume volume;
    [SerializeField] private UIAudioManager uiAudio;
    [SerializeField] private MainMenuMusicController musicController;
    [SerializeField] private Image fadeImage;
    private bool continueButtonEnabled = false;

    private void Start()
    {
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        // Ensure cursor is visible and locked
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        if (volume.profile.TryGet<LensDistortion>(out LensDistortion distortion))
        {
            distortion.active = true;
        }

        // Get Menu dependencies stored in DontDestroyOnLoad
        SceneFadeTransition transitionScript = FindFirstObjectByType<SceneFadeTransition>();
        if (transitionScript != null) fadeImage = transitionScript.GetFadeImage();


        // Set time scale to normal
        Time.timeScale = 1f;
        //if(//GlobalSaveManager.Instance.Data.PlayerData. == 0)
        //{

        //}
        // show continue button
        if (GlobalSaveManager.SaveDataExists())
        {
            continueButtonEnabled = true;
        }
    }

    /// <summary>
    /// Loads a new game
    /// </summary>
    //public void StartGame()
    //{
    //    Cursor.visible = false;
    //    Cursor.lockState = CursorLockMode.Locked;

    //    uiAudio?.PlaySelectSound();
    //    GlobalSaveManager.LoadFromSave = false;
    //    GlobalSaveManager.DeleteTempFiles();
    //    musicController.FadeOut();
    //    // Use fade transition instead of direct scene load
    //    if (SceneFadeTransition.Instance != null)
    //    {
    //        SceneFadeTransition.Instance.LoadSceneWithFade("Level1New");
    //    }
    //    else
    //    {
    //        SceneManager.LoadScene("Level1New");
    //    }
    //}

    /// <summary>
    /// Loads a new game once music has faded out
    /// </summary>
    public void StartGame()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        uiAudio?.PlaySelectSound();
        GlobalSaveManager.LoadFromSave = false;
        GlobalSaveManager.DeleteTempFiles();

        LoadWithMusicFade("Level1New");
    }
    /// <summary>
    /// Loads saved instance of game
    /// </summary>
    //public void LoadGame()
    //{
    //    if (continueButtonEnabled)
    //    {
    //        Cursor.visible = false;
    //        Cursor.lockState = CursorLockMode.Locked;

    //        uiAudio?.PlaySelectSound();
    //        GlobalSaveManager.LoadFromSave = true;
    //        GlobalSaveManager.DeleteTempFiles();
    //        GlobalSaveManager.OverwriteTempFiles();
    //        musicController.FadeOut();
    //        // Use fade transition instead of direct scene load
    //        if (SceneFadeTransition.Instance != null)
    //        {
    //            SceneFadeTransition.Instance.LoadSceneWithFade("Level1New");
    //        }
    //        else
    //        {
    //            SceneManager.LoadScene("Level1New");
    //        }
    //    }
    //    else
    //    {
    //        uiAudio?.PlayBackSound();
    //    }
    //}


    ///<summary>
    ///Loads saved instance of game once music has faded out
    ///</summary>
    public void LoadGame()
    {
        if (continueButtonEnabled)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            uiAudio?.PlaySelectSound();
            GlobalSaveManager.LoadFromSave = true;
            GlobalSaveManager.DeleteTempFiles();
            GlobalSaveManager.OverwriteTempFiles();

            LoadWithMusicFade("Level1New");
        }
        else
        {
            uiAudio?.PlayBackSound();
        }
    }
    ///</summary>
    /// <summary>
    /// Opens the options menu and turns off lens distortion
    /// </summary>
    public void Options()
    {
        uiAudio?.PlaySelectSound();
        optionsMenu.SetActive(true);
        this.gameObject.SetActive(false);
        settingsMenu.SetUp();
        if (volume.profile.TryGet<LensDistortion>(out LensDistortion distortion))
        {
            distortion.active = false;
        }
    }

    /// <summary>
    /// Prompts user to verify they want to leave options when something is changed
    /// </summary>
    public void CloseOptions()
    {
        uiAudio?.PlayBackSound();
        
        if (settingsMenu.isChanged)
        {
            settingsMenu.OpenPopUp(confirmationPopUp);
        }
        else
        {
            ExitOptions();
        }
    }

    /// <summary>
    /// Closes options menu
    /// </summary>
    /// 
    public void ExitOptions()
    {
        uiAudio?.PlayBackSound();
        if (volume.profile.TryGet<LensDistortion>(out LensDistortion distortion))
        {
            distortion.active = true;
        }
        optionsMenu.SetActive(false);
        this.gameObject.SetActive(true);
    }

    /// <summary>
    /// Fades out music over time then loads the specified scene
    /// </summary>
    public void LoadWithMusicFade(string sceneName)
    {
        StartCoroutine(FadeThenLoad(sceneName));
    }

    private IEnumerator FadeThenLoad(string sceneName)
    {
        musicController.FadeOut();
        float elapsed = 0f;
        float duration = (float)Looper.mediumFadeDuration;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;
            yield return null;
        }

        if (SceneFadeTransition.Instance != null)
            SceneFadeTransition.Instance.LoadSceneWithFade(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Quits to desktop or stops playing if in editor
    /// </summary>
    /// 

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
