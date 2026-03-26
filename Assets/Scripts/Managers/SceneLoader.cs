using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using NUnit.Framework.Internal.Commands;
using UnityEngine.UIElements;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [SerializeField] private GameObject loadingScreenCanvas;
    [SerializeField] public TagHandle entryTag;

    //transfered variables
    private Vector3 _pendingLocalOffset;
    private Vector3 _pendingVelocity;
    private Vector3 _pendingAngularVelocity;
    private Quaternion _pendingCamRotation;
    private bool _hasPendingTransfer;


    private void Awake()
    {
        //if the scene we are currently in gets destroyed
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        entryTag = TagHandle.GetExistingTag("EntryTrigger");
    }

    public void LoadScene(string sceneName, Vector3 localOffset, Vector3 velocity, Vector3 angularVelocity, Quaternion camRotation)
    {
        _pendingLocalOffset = localOffset;
        _pendingVelocity = velocity;
        _pendingAngularVelocity = angularVelocity;
        _pendingCamRotation = camRotation;
        _hasPendingTransfer = true;
        StartCoroutine(LoadAsync(sceneName));
    }

    private IEnumerator LoadAsync(string sceneName)
    {
        if (loadingScreenCanvas)
        {
            loadingScreenCanvas.SetActive(true);
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        //wait until the scene is ready
        while(op.progress < 0.9f) yield return null;

        op.allowSceneActivation = true;

        //wait for scene objects to all initialize
        yield return null; // let scene activate
        yield return null; // let awake/start run
        yield return new WaitForSeconds(0.1f);

        ApplyTransfer();
    }

    private void ApplyTransfer()
    {
        if (!_hasPendingTransfer) return;

        // Find entry trigger: tag "EntryTrigger" in Inspector for specificity
        SceneTransitionTrigger[] allTriggers = FindObjectsByType<SceneTransitionTrigger>(FindObjectsSortMode.None);
        Debug.Log(allTriggers);
        SceneTransitionTrigger entryTrigger = null;
        foreach (var t in allTriggers)
        {
            if (t.CompareTag(entryTag)) { entryTrigger = t; break; }
        }

        if(entryTrigger == null)
        {
            Debug.LogError("No entry trigger found in new scene! Tag it 'EntryTrigger'.");
            return;
        }

        // Reconstruct position using entry box dimensions
        BoxCollider entryBox = entryTrigger.GetComponent<BoxCollider>();
        Vector3 scaledOffset = new Vector3(
            _pendingLocalOffset.x * entryBox.size.x,
            _pendingLocalOffset.y * entryBox.size.y,
            _pendingLocalOffset.z * entryBox.size.z
        );
        Vector3 worldPos = entryTrigger.transform.TransformPoint(scaledOffset);

        // Find player and ZeroGravity
        //GameObject player = GameObject.FindWithTag("Player");
        //if (player == null) { Debug.LogError("Player not found in new scene!"); return; }
        ZeroGravity zg = FindFirstObjectByType<ZeroGravity>();
        if (zg == null) { Debug.LogError("ZeroGravity not found on player!"); return; }

        //Apply position and physics state
        zg.transform.position = worldPos;
        zg.ApplySceneTransferState(_pendingVelocity, _pendingAngularVelocity, _pendingCamRotation);

        if (loadingScreenCanvas)loadingScreenCanvas.SetActive(false);
        _hasPendingTransfer = false;

        Debug.Log("Transfer applied. Player placed at: " + worldPos);
    }
}
