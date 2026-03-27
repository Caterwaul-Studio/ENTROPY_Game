using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ServerProgressBars : MonoBehaviour
{
    [Header("Screen Sources")]
    //[SerializeField] Terminal serverTerminal;
    [SerializeField] LockdownPanel panel;

    [Header("Panels")]
    //[SerializeField] GameObject terminalShutdown;
    //[SerializeField] GameObject terminalReboot;
    [SerializeField] GameObject panelShutdown;
    [SerializeField] GameObject panelReboot;

    [Header("Sliders")]
    //[SerializeField] Slider terminalShutdownSlider;
    //[SerializeField] Slider terminalRebootSlider;
    [SerializeField] Slider panelShutdownSlider;
    [SerializeField] Slider panelRebootSlider;

    [Header("UploadText")]
    //[SerializeField] TMP_Text terminalShutdownText;
    //[SerializeField] TMP_Text terminalRebootText;
    [SerializeField] TMP_Text panelShutdownText;
    [SerializeField] TMP_Text panelRebootText;

    public bool shutdownComplete = false;
    public bool rebootComplete = false;

    private enum ProgressType { Shutdown, Reboot }

    private IEnumerator UploadProgress(float duration, GameObject uploadPanel, Slider progressFill, TMP_Text uploadText, ProgressType type)
    {
        uploadPanel.SetActive(true);
        float currentTime = 0f;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float progress = Mathf.Clamp01(currentTime / duration);
            progressFill.value = progress;
            uploadText.text = $"{(int)(progress * 100)}%";
            yield return null;
        }

        // Upload complete
        progressFill.value = 1f;
        uploadText.text = "100%";
        yield return new WaitForSeconds(1f);

        if (type == ProgressType.Shutdown)
            shutdownComplete = true;
        else if (type == ProgressType.Reboot)
            rebootComplete = true;

        
    }

    public void Shutdown() => StartCoroutine(StartShutdown());
    public void Reboot() => StartCoroutine(StartReboot());

    private IEnumerator StartShutdown()
    {
        shutdownComplete = false;

        //StartCoroutine(UploadProgress(6f, terminalShutdown, terminalShutdownSlider, terminalShutdownText, ProgressType.Shutdown));
        StartCoroutine(UploadProgress(6f, panelShutdown, panelShutdownSlider, panelShutdownText, ProgressType.Shutdown));

        yield return new WaitUntil(() => shutdownComplete);

        //terminalShutdown.SetActive(false);
        panelShutdown.SetActive(false);
        //serverTerminal.TurnOff();
        panel.TurnOff();
    }

    private IEnumerator StartReboot()
    {
        rebootComplete = false;
        //serverTerminal.TurnOn();
        panel.TurnOn();

        //StartCoroutine(UploadProgress(7f, terminalReboot, terminalRebootSlider, terminalRebootText, ProgressType.Reboot));
        StartCoroutine(UploadProgress(7f, panelReboot, panelRebootSlider, panelRebootText, ProgressType.Reboot));

        yield return new WaitUntil(() => rebootComplete);

        panel.CompleteLockdown();
        //terminalReboot.SetActive(false);
        panelReboot.SetActive(false);
    }
}
