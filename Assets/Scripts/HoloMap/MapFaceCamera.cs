using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Turns the map's labels to face each camera just before it draws, so they read right on a turning
/// holotable and on the wrist monitor at the same time. The Map Builder puts this on the baked map's Labels group.
/// </summary>
public class MapFaceCamera : MonoBehaviour
{
    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += FaceCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= FaceCamera;
    }

    private void FaceCamera(ScriptableRenderContext context, Camera camera)
    {
        // Cameras that can't see the map don't need the labels turned
        if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;

        Quaternion facing = camera.transform.rotation;
        foreach (Transform label in transform)
        {
            label.rotation = facing;
        }
    }
}
