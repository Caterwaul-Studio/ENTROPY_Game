using TMPro;
using UnityEngine;

/// <summary>
/// The holo table: keeps a hologram map hidden until the player comes near, then fades it in with a
/// flicker like a projector warming up, and fades it out again when they walk away.
/// Put it on the object that holds the map (the table), not inside the map prefab: building the map
/// replaces the prefab, which would remove it.
/// </summary>
public class MapHoloTable : MonoBehaviour
{
    // Looking for the player is slow, so when there isn't one it's tried again only now and then
    const float SearchInterval = 1f;

    [Tooltip("The map to show. Leave empty to use everything under this object.")]
    public Transform map;

    [Tooltip("What has to come near. Leave empty to use the player's camera.")]
    public Transform viewer;

    [Header("Distance (m, from the edge of the map)")]
    [Tooltip("The map turns on when the viewer comes this close.")]
    public float turnOnDistance = 2.5f;

    [Tooltip("The map turns off when the viewer goes this far. A bit more than Turn On, so standing at the edge doesn't make it flicker over and over.")]
    public float turnOffDistance = 3.5f;

    [Header("Fade (s)")]
    [Tooltip("How long the map takes to fade fully in.")]
    public float turnOnTime = 0.8f;

    [Tooltip("How long the map takes to fade fully out.")]
    public float turnOffTime = 0.35f;

    [Header("Flicker")]
    [Tooltip("How deep the flicker dips while fading: 0 is a plain fade, 1 dips all the way to nothing.")]
    [Range(0f, 1f)] public float flickerStrength = 0.8f;

    [Tooltip("How fast the flicker wavers. Higher is a faster buzz.")]
    public float flickerSpeed = 14f;

    private Renderer[] parts;
    private TMP_Text[] texts;
    private float[] textAlphas;

    // The map's labels fade through their own script, which also dims them with depth
    private MapFaceCamera[] labelGroups;
    private Bounds localBounds;
    private Transform player;
    private float nextSearch;
    private bool wantsOn;

    // How far the fade has got, 0 off to 1 on, before the flicker is added
    private float progress;
    private float shownOpacity = -1f;
    private float noiseSeed;
    private MaterialPropertyBlock block;

    private static readonly int FadeId = Shader.PropertyToID("_Fade");

    private void Start()
    {
        if (map == null) map = transform;
        block = new MaterialPropertyBlock();
        noiseSeed = Random.value * 100f;

        // Includes pieces that start hidden, like disabled doors, so they fade with the rest once shown.
        // Labels fade through their text's own alpha, since their font shaders don't know the map's fade
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

    /// <summary>
    /// Moves the fade toward on or off, wavering on the way. Settled at either end, it stops changing anything.
    /// </summary>
    private void Fade(float deltaTime)
    {
        float time = wantsOn ? turnOnTime : turnOffTime;
        progress = Mathf.MoveTowards(progress, wantsOn ? 1f : 0f, time > 0f ? deltaTime / time : 1f);

        // Eased so it starts and lands softly, with the flicker strongest halfway and gone at both ends
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

    /// <summary>
    /// How far a point is from the nearest edge of the map, in meters. 0 inside it.
    /// </summary>
    public float DistanceToMap(Vector3 worldPoint)
    {
        Vector3 nearest = localBounds.ClosestPoint(map.InverseTransformPoint(worldPoint));
        return Vector3.Distance(worldPoint, map.TransformPoint(nearest));
    }

    /// <summary>
    /// Turns the map on or off from another script, fading as usual. Walking up or away still switches it too.
    /// </summary>
    public void Switch(bool on)
    {
        wantsOn = on;
    }

    /// <summary>
    /// Shows the map at this opacity, 0 to 1.
    /// </summary>
    public void SetOpacity(float opacity)
    {
        if (Mathf.Approximately(opacity, shownOpacity)) return;
        shownOpacity = opacity;

        // Fully on, the parts drop their property blocks so they batch as usual; fully off, they aren't drawn at all.
        // forceRenderingOff leaves each part's own enabled state alone, which the map's other scripts use
        bool full = opacity >= 1f;
        block.SetFloat(FadeId, opacity);
        foreach (Renderer part in parts)
        {
            if (part == null) continue;
            part.forceRenderingOff = opacity <= 0f;
            if (part.GetComponent<TMP_Text>() != null) continue;
            if (full) part.SetPropertyBlock(null);
            else part.SetPropertyBlock(block);
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
            // Hidden pieces have no real bounds, and would stretch the map out to the world's center
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
