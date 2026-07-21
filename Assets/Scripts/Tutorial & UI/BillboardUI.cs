using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    [SerializeField] private PersistantManager persistenantManager;
    [SerializeField] private Camera mainCamera;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    { 
        if (persistenantManager == null)
        {
            persistenantManager = GameObject.FindFirstObjectByType<PersistantManager>();
            mainCamera = persistenantManager.MainCamera;
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (persistenantManager == null)
        {
            persistenantManager = GameObject.FindFirstObjectByType<PersistantManager>();
            mainCamera = persistenantManager.MainCamera;
        }
        //Creates billboarding effect (looking at camera)
        Quaternion rotation = mainCamera.transform.rotation;
        transform.LookAt(worldPosition: transform.position + rotation * Vector3.forward, worldUp: rotation * Vector3.up);
    }
}
