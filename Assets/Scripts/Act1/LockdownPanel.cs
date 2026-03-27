using UnityEngine;
using TMPro;
using System.Collections;
using static UnityEngine.GraphicsBuffer;

public class LockdownPanel : MonoBehaviour
{

    [SerializeField] private MeshRenderer terminalMesh;
    [SerializeField] private Transform LeverPivot;
    private Material screenMaterial;
    [SerializeField] private Color screenEmissiveColor = new Color(0.113f, 0.113f, 0.113f);
    private Material baseMaterial;
    [SerializeField] private Color baseEmissiveColor = new Color(0.12f, 0.12f, 0.12f);
    [Header("Panels")]
    [SerializeField] private GameObject reactivatePanel;
    [SerializeField] private GameObject deactivatePanel;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI reactivateText;
    [SerializeField] private TextMeshProUGUI deactivateText;

    [Header("Screen Components")]
    //[SerializeField] private GameObject screenBacking;
    //[SerializeField] private Material offMaterial;
    //[SerializeField] private Material onMaterial;
    [SerializeField] private GameObject offScreen;

    [Header("Lockdown Status")]
    [SerializeField] private GameObject awaitingLockdown;
    [SerializeField] private GameObject completedLockdown;

    [Header("Blink Settings")]
    [SerializeField] private float blinkInterval = 0.5f;

    private Coroutine currentBlinkCoroutine;

    private void Start()
    {
        // Initialize panels
        reactivatePanel.SetActive(true);
        deactivatePanel.SetActive(false);

        baseMaterial = terminalMesh.materials[1];
        screenMaterial = terminalMesh.materials[2];

        // Initialize lockdown status
        if (awaitingLockdown != null) awaitingLockdown.SetActive(true);
        if (completedLockdown != null) completedLockdown.SetActive(false);

        // Start with blinking reactivate text
        StartBlinking(reactivateText);

        // Start with screen turned on
        TurnOn();
    }

    private void StartBlinking(TextMeshProUGUI tmp)
    {
        if (currentBlinkCoroutine != null)
            StopCoroutine(currentBlinkCoroutine);

        currentBlinkCoroutine = StartCoroutine(BlinkText(tmp));
    }

    private IEnumerator BlinkText(TextMeshProUGUI tmp)
    {
        while (true)
        {
            tmp.enabled = !tmp.enabled;
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    public void SwitchToDeactivate()
    {
        if (currentBlinkCoroutine != null)
            StopCoroutine(currentBlinkCoroutine);

        reactivatePanel.SetActive(false);
        deactivatePanel.SetActive(true);

        StartBlinking(deactivateText);
    }

    public void TurnOn()
    {
        //if (screenBacking != null && onMaterial != null)
        //{
        //    var renderer = screenBacking.GetComponent<Renderer>();
        //    if (renderer != null)
        //        renderer.material = onMaterial;
        //}

        baseMaterial.SetColor("_EmissionColor", baseEmissiveColor);
        screenMaterial.SetColor("_EmissionColor", screenEmissiveColor);

        if (offScreen != null)
            offScreen.SetActive(false);
    }

    public void TurnOff()
    {
        //if (screenBacking != null && offMaterial != null)
        //{
        //    var renderer = screenBacking.GetComponent<Renderer>();
        //    if (renderer != null)
        //        renderer.material = offMaterial;
        //}

        baseMaterial.SetColor("_EmissionColor", baseEmissiveColor * -10.0f);
        screenMaterial.SetColor("_EmissionColor", screenEmissiveColor * -10.0f);

        if (offScreen != null)
            offScreen.SetActive(true);
    }

 
    public IEnumerator PlayLeverAnimation()
    {
        Quaternion startRot = LeverPivot.rotation;
        Quaternion endRot = Quaternion.Euler(82f, LeverPivot.eulerAngles.y, LeverPivot.eulerAngles.z);
        float duration = 0.8f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            LeverPivot.rotation = Quaternion.Lerp(startRot, endRot, t / duration);
            yield return null;
        }

        LeverPivot.rotation = endRot; // ensure final rotation
    }

    /// <summary>
    /// Marks the lockdown as complete: disables awaitingLockdown and enables completedLockdown.
    /// </summary>
    public void CompleteLockdown()
    {
        if (awaitingLockdown != null)
            awaitingLockdown.SetActive(false);

        if (completedLockdown != null)
            completedLockdown.SetActive(true);
    }
}