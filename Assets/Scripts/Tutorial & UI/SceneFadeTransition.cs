using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles fading to black when transitioning between scenes
/// </summary>
public class SceneFadeTransition : MonoBehaviour
{
    public static SceneFadeTransition Instance { get; private set; }

    [SerializeField] private Canvas fadeCanvas;
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeOutDuration = 1f;  // Fade to black duration
    [SerializeField] private float fadeInDuration = 1f;   // Fade from black duration

    private void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Ensure the fade image starts transparent
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    /// <summary>
    /// Loads a scene with fade transition
    /// </summary>
    public void LoadSceneWithFade(string sceneName)
    {
        StartCoroutine(FadeAndLoadScene(sceneName));
    }

    private IEnumerator FadeAndLoadScene(string sceneName)
    {
        // Fade to black (using fadeOutDuration)
        yield return StartCoroutine(Fade(1f, fadeOutDuration));

        // Start loading the scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // Wait until the scene is fully loaded
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Small delay to ensure everything is initialized
        yield return new WaitForSeconds(0.1f);

        // Fade from black (using fadeInDuration)
        yield return StartCoroutine(Fade(0f, fadeInDuration));
    }

    /// <summary>
    /// Fades the screen to/from black
    /// </summary>
    /// <param name="targetAlpha">Target alpha value (0 = transparent, 1 = black)</param>
    /// <param name="duration">How long the fade takes</param>
    private IEnumerator Fade(float targetAlpha, float duration)
    {
        if (fadeImage == null) yield break;

        float startAlpha = fadeImage.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);

            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;

            yield return null;
        }

        // Ensure we end at exact target alpha
        Color finalColor = fadeImage.color;
        finalColor.a = targetAlpha;
        fadeImage.color = finalColor;
    }
}