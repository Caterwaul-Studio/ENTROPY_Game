using TMPro;
using UnityEngine;

public class MapHoloTable : MonoBehaviour
{
    const float SearchInterval = 1f;

    public Transform map;
    public Transform viewer;

    [Header("Distance (m, from the edge of the map)")]
    public float turnOnDistance = 2.5f;
    public float turnOffDistance = 3.5f;

    [Header("Fade (s)")]
    public float turnOnTime = 0.8f;
    public float turnOffTime = 0.35f;

    [Header("Flicker")]
    [Range(0f, 1f)] public float flickerStrength = 0.8f;
    public float flickerSpeed = 14f;

    private Renderer[] parts;
    private TMP_Text[] texts;
    private float[] textAlphas;
    private MapFaceCamera[] labelGroups;
    private Bounds localBounds;
    private Transform player;
    private float nextSearch;
    private bool wantsOn;
    private float progress;
    private float shownOpacity = -1f;
    private float noiseSeed;

    private void Start()
    {
        if (map == null) map = transform;
        noiseSeed = Random.value * 100f;

        parts = map.GetComponentsInChildren<Renderer>(true);
        labelGroups = map.GetComponentsInChildren<MapFaceCamera>(true);
        texts = System.Array.FindAll(map.GetComponentsInChildren<TMP_Text>(true), text => text.GetComponentInParent<MapFaceCamera>(true) == null);
        textAlphas = new float[texts.Length];
        for (int i = 0; i < texts.Length; i++) textAlphas[i] = texts[i].alpha;
        localBounds = MeasureBounds(map, parts);
        SetOpacity(0f);
    }

    private void Update()
    {
        Transform from = viewer != null ? viewer : FindPlayer();
        if (from != null)
        {
            float distance = DistanceToMap(from.position);
            if (!wantsOn && distance <= turnOnDistance) wantsOn = true;
            else if (wantsOn && distance >= turnOffDistance) wantsOn = false;
        }
        Fade(Time.deltaTime);
    }

    private void Fade(float deltaTime)
    {
        float time = wantsOn ? turnOnTime : turnOffTime;
        progress = Mathf.MoveTowards(progress, wantsOn ? 1f : 0f, time > 0f ? deltaTime / time : 1f);

        float eased = progress * progress * (3f - 2f * progress);
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, noiseSeed);
        float dip = flickerStrength * Mathf.Sin(progress * Mathf.PI) * noise * noise * 1.6f;
        SetOpacity(Mathf.Clamp01(eased * (1f - dip)));
    }

    private Transform FindPlayer()
    {
        if (player == null && Time.unscaledTime >= nextSearch)
        {
            nextSearch = Time.unscaledTime + SearchInterval;
            ZeroGravity found = FindFirstObjectByType<ZeroGravity>();
            if (found != null) player = found.cam != null ? found.cam.transform : found.transform;
        }
        return player;
    }

    public float DistanceToMap(Vector3 worldPoint)
    {
        Vector3 nearest = localBounds.ClosestPoint(map.InverseTransformPoint(worldPoint));
        return Vector3.Distance(worldPoint, map.TransformPoint(nearest));
    }

    public void Switch(bool on)
    {
        wantsOn = on;
    }

    public void SetOpacity(float opacity)
    {
        if (Mathf.Approximately(opacity, shownOpacity)) return;
        shownOpacity = opacity;

        // forceRenderingOff leaves enabled alone for the other map scripts
        foreach (Renderer part in parts)
        {
            if (part == null) continue;
            part.forceRenderingOff = opacity <= 0f;
            if (part.GetComponent<TMP_Text>() != null) continue;
            MapPartValues.SetFade(part, opacity);
        }
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null) texts[i].alpha = textAlphas[i] * opacity;
        }
        foreach (MapFaceCamera labels in labelGroups)
        {
            if (labels != null) labels.SetOpacity(opacity);
        }
    }

    private static Bounds MeasureBounds(Transform map, Renderer[] parts)
    {
        Matrix4x4 toMap = map.worldToLocalMatrix;
        Bounds bounds = default;
        bool any = false;
        foreach (Renderer part in parts)
        {
            // Hidden pieces have no real bounds
            if (!part.enabled || !part.gameObject.activeInHierarchy) continue;
            Bounds world = part.bounds;
            if (!any) bounds = new Bounds(toMap.MultiplyPoint3x4(world.center), Vector3.zero);
            any = true;
            for (int i = 0; i < 8; i++)
            {
                var corner = world.center + Vector3.Scale(world.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                bounds.Encapsulate(toMap.MultiplyPoint3x4(corner));
            }
        }
        return bounds;
    }
}
