using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class MapFaceCamera : MonoBehaviour
{
    const float AlphaStep = 0.01f;

    private TMP_Text[] texts;
    private Color[] baseColors;
    private Color[] shownColors;
    private float opacity = 1f;

    private float[] powerBrightness;
    private float[] powerSaturation;
    private Dictionary<TMP_Text, int> indexes;

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
        if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;
        Face(camera);
    }

    public void Face(Camera camera)
    {
        Quaternion facing = GetFacing(camera.transform.forward, camera.transform.up, transform.up);
        foreach (Transform label in transform)
        {
            label.rotation = facing;
        }
    }

    public static Quaternion GetFacing(Vector3 cameraForward, Vector3 cameraUp, Vector3 mapUp)
    {
        float alongUp = Mathf.Abs(Vector3.Dot(cameraForward.normalized, mapUp.normalized));
        float useCamera = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.9f, 0.995f, alongUp));
        Vector3 up = Vector3.Lerp(mapUp.normalized, cameraUp.normalized, useCamera);
        return Quaternion.LookRotation(cameraForward, up.sqrMagnitude > 0.0001f ? up : cameraUp);
    }

    public void SetOpacity(float value)
    {
        opacity = value;
    }

    public void ApplyDepth(Camera camera, float near, float far, float dimming)
    {
        FindTexts();
        Matrix4x4 toView = camera.worldToCameraMatrix;
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null) continue;

            float depth = -toView.MultiplyPoint3x4(text.transform.position).z;
            Color color = baseColors[i];
            float grey = color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
            color = Color.Lerp(new Color(grey, grey, grey, color.a), color, powerSaturation[i]);
            color.a = baseColors[i].a * opacity * powerBrightness[i] * GetDepthBrightness(depth, near, far, dimming);

            Color shown = shownColors[i];
            float change = Mathf.Abs(color.r - shown.r) + Mathf.Abs(color.g - shown.g) + Mathf.Abs(color.b - shown.b) + Mathf.Abs(color.a - shown.a);
            bool settled = color.a <= 0f || color == baseColors[i];
            if (change < AlphaStep && !settled) continue;
            if (color == shown) continue;

            shownColors[i] = color;
            text.color = color;
        }
    }

    public void SetPower(TMP_Text label, float power, float unpoweredBrightness, float unpoweredSaturation)
    {
        FindTexts();
        foreach (TMP_Text text in label.GetComponentsInChildren<TMP_Text>(true))
        {
            if (!indexes.TryGetValue(text, out int i)) continue;
            powerBrightness[i] = Mathf.Lerp(unpoweredBrightness, 1f, power);
            powerSaturation[i] = Mathf.Lerp(unpoweredSaturation, 1f, power);
        }
    }

    private void FindTexts()
    {
        if (texts != null) return;

        texts = GetComponentsInChildren<TMP_Text>(true);
        baseColors = new Color[texts.Length];
        shownColors = new Color[texts.Length];
        powerBrightness = new float[texts.Length];
        powerSaturation = new float[texts.Length];
        indexes = new Dictionary<TMP_Text, int>();
        for (int i = 0; i < texts.Length; i++)
        {
            baseColors[i] = texts[i].color;
            shownColors[i] = baseColors[i];
            powerBrightness[i] = 1f;
            powerSaturation[i] = 1f;
            indexes[texts[i]] = i;
        }
    }

    public static float GetDepthBrightness(float depth, float near, float far, float dimming)
    {
        float range = far - near;
        if (range <= 0.0001f) return 1f;
        float t = Mathf.Clamp01((depth - near) / (range * 0.75f));
        return 1f - dimming * t * t * (3f - 2f * t);
    }
}
