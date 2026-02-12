using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FPSDisplay : MonoBehaviour
{
    public Text fpsText;  // Reference to a Text UI element to display FPS
    public float updateInterval = 0.5f;  // How often the FPS is updated (in seconds)

    private float timeLeft;
    private int frameCount;
    private float fps;

    //public getter create for FPS so we can reference it in the dev tools. therefore remove the p key stuff here. need to calculate here tho as the Universal Dev tools run as a GUI and therefore can't make these calcs
    public float FPS {  get { return fps; } }

    void Start()
    {
        if (fpsText == null)
        {
            Debug.LogError("FPSDisplay: No Text UI element assigned!");
        }
        else
        {
            timeLeft = updateInterval;
            fpsText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (Keyboard.current.pKey.wasPressedThisFrame)  // Press 'P' to toggle FPS display
        {
            fpsText.gameObject.SetActive(!fpsText.gameObject.activeSelf);
        }

        frameCount++;
        timeLeft -= Time.deltaTime;

        if (timeLeft <= 0.0)
        {
            // Update FPS
            fps = frameCount / updateInterval;

            // Display FPS in UI Text
            fpsText.text = "FPS: " + Mathf.Round(fps).ToString();

            // Reset
            timeLeft = updateInterval;
            frameCount = 0;
        }
    }
}
