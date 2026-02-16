using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Quaternion rotation = mainCamera.transform.rotation;
        transform.LookAt(worldPosition: transform.position + rotation * Vector3.forward, worldUp: rotation * Vector3.up);
    }
}
