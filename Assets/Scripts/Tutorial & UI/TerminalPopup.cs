using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TerminalPopup : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text uploadText;
    [SerializeField] GameObject popupObject;
    [SerializeField] GameObject uploadCompleteText;
    [SerializeField] GameObject screenBlur;
    public float uploadDuration = 3f;
    [SerializeField] Slider progressFill;
    [SerializeField] GameObject terminalText;
    private float currentTime = 0f;
    private bool isUploading = false;
    private bool isUploaded = false;
    public bool IsUploaded => isUploaded;

    [Header("Audio")]
    [SerializeField] private TerminalAudioManager terminalAudio;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bootupSource;
    [SerializeField] private AudioSource completeSource;
    [SerializeField] private AudioSource loadingBarSource;

    public GameObject PopupObject { set { popupObject = value; } }
    public GameObject UploadCompleteText { set { uploadCompleteText = value; } }
    public GameObject ScreenBlur { set { screenBlur = value; } }
    public GameObject TerminalText { set { terminalText = value; } }
    public TMP_Text UploadText { set { uploadText = value; } }
    public Slider ProgressFill { set { progressFill = value; } }


    private void Start()
    {
        terminalAudio = FindFirstObjectByType<TerminalAudioManager>();
    }
    /// <summary>
    /// Start the upload sequence with progress bar and audio
    /// </summary>
    public void StartUpload()
    {
        screenBlur.SetActive(true);
        popupObject.SetActive(true);
        
        currentTime = 0f;
        isUploading = true;
        progressFill.value = 0f;
        uploadText.text = "UPLOADING... 0%";
        uploadCompleteText.SetActive(false);
        isUploaded = false;
        terminalAudio.PlayLoadingBarSound(loadingBarSource);

        StartCoroutine(UploadProgress());
    }

    private IEnumerator UploadProgress()
    {
        float currentTime = 0f;

        while (currentTime < uploadDuration)
        {
            currentTime += Time.deltaTime;
            float progress = Mathf.Clamp01(currentTime / uploadDuration);

            // Update progress bar
            progressFill.value = progress;
            uploadText.text = $"UPLOADING... {(int)(progress * 100)}%";

            

            yield return null;
        }

        // Ensure progress bar is full
        progressFill.value = 1f;
        uploadText.text = "UPLOADING... 100%";


        // Show upload complete UI
        uploadCompleteText.SetActive(true);

        // Play upload complete sound once
        terminalAudio?.PlayUploadCompleteSound(completeSource);

        yield return new WaitForSeconds(1f);
        isUploaded = true;

        terminalText.SetActive(false);
        popupObject.SetActive(false);
        
    }
}





