using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Turns the map's labels to face each camera just before it draws, so they read right on a turning
/// holotable and on the wrist monitor at the same time. They stay upright on the map, so a rolled or
/// upside-down camera doesn't turn them over. It also fades them with the rest of the map:
/// dimmer toward the back of the map, and in and out with the holo table.
/// The Map Builder puts this on the baked map's Labels group.
/// </summary>
public class MapFaceCamera : MonoBehaviour
{
    // Changes smaller than this aren't worth rebuilding a label's text for
    const float AlphaStep = 0.01f;

    private TMP_Text[] texts;
    private float[] baseAlphas;
    private float[] shownAlphas;
    private float opacity = 1f;

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
        Face(camera);
    }

    /// <summary>
    /// Turns every label toward this camera, keeping the map's up as the labels' up.
    /// </summary>
    public void Face(Camera camera)
    {
        Quaternion facing = GetFacing(camera.transform.forward, camera.transform.up, transform.up);
        foreach (Transform label in transform)
        {
            label.rotation = facing;
        }
    }

    /// <summary>
    /// Facing the camera with the map's up as the label's up. Looking almost straight down or up the map's up
    /// leaves no sideways direction to stand the label on, so there it follows the camera's own up instead,
    /// blended in so the label doesn't snap.
    /// </summary>
    public static Quaternion GetFacing(Vector3 cameraForward, Vector3 cameraUp, Vector3 mapUp)
    {
        float alongUp = Mathf.Abs(Vector3.Dot(cameraForward.normalized, mapUp.normalized));
        float useCamera = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.9f, 0.995f, alongUp));
        Vector3 up = Vector3.Lerp(mapUp.normalized, cameraUp.normalized, useCamera);
        return Quaternion.LookRotation(cameraForward, up.sqrMagnitude > 0.0001f ? up : cameraUp);
    }

    /// <summary>
    /// How much of the labels shows, 0 to 1. The holo table sets it as the map fades in and out.
    /// </summary>
    public void SetOpacity(float value)
    {
        opacity = value;
    }

    /// <summary>
    /// Dims each label by how far back in the map it is from this camera, the same way the map's shaders dim
    /// its lines. Labels use the game's font shaders, which can't do it themselves, so their text's alpha changes.
    /// MapDoors calls this with the map's depth for each camera.
    /// </summary>
    public void ApplyDepth(Camera camera, float near, float far, float dimming)
    {
        if (texts == null)
        {
            // Includes the invisible letter copies, so lines show through a faded label again
            texts = GetComponentsInChildren<TMP_Text>(true);
            baseAlphas = new float[texts.Length];
            shownAlphas = new float[texts.Length];
            for (int i = 0; i < texts.Length; i++)
            {
                baseAlphas[i] = texts[i].alpha;
                shownAlphas[i] = baseAlphas[i];
            }
        }

        Matrix4x4 toView = camera.worldToCameraMatrix;
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null) continue;

            float depth = -toView.MultiplyPoint3x4(text.transform.position).z;
            float alpha = baseAlphas[i] * opacity * GetDepthBrightness(depth, near, far, dimming);
            if (Mathf.Abs(alpha - shownAlphas[i]) < AlphaStep && alpha > 0f && alpha < baseAlphas[i]) continue;
            if (Mathf.Approximately(alpha, shownAlphas[i])) continue;

            shownAlphas[i] = alpha;
            text.alpha = alpha;
        }
    }

    /// <summary>
    /// The same dimming as the map's shaders: full at the map's front, fully dimmed three quarters of the way back, eased.
    /// </summary>
    public static float GetDepthBrightness(float depth, float near, float far, float dimming)
    {
        float range = far - near;
        if (range <= 0.0001f) return 1f;
        float t = Mathf.Clamp01((depth - near) / (range * 0.75f));
        return 1f - dimming * t * t * (3f - 2f * t);
    }
}
