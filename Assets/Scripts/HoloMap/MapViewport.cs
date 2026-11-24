using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapViewport : MonoBehaviour
{
    const float SearchInterval = 1f;

    // Far from every level, so the player's own camera never sees the copy
    static readonly Vector3 CopyPosition = new Vector3(0f, -10000f, 0f);

    public RawImage display;
    public GameObject unavailable;
    public string mapLayer = "MapView";
    public int resolution = 512;
    public Color background = new Color(0.02f, 0.03f, 0.05f, 1f);

    [Header("View (m)")]
    public float distance = 15f;
    [Range(0f, 90f)] public float tilt = 55f;
    public float fieldOfView = 40f;
    public float turnSpeed = 15f;

    [Header("Cut Away (m)")]
    public float clearance = 4f;
    public float cutFade = 3f;

    private static readonly int WristViewId = Shader.PropertyToID("_HoloMapWristView");
    private static readonly int CutStartId = Shader.PropertyToID("_HoloMapCutStart");
    private static readonly int CutEndId = Shader.PropertyToID("_HoloMapCutEnd");

    private MapLevel level;
    private GameObject copy;
    private MapPlayerMarker marker;
    private ZeroGravity player;
    private Camera view;
    private RenderTexture texture;
    private Quaternion facing;
    private bool aimed;
    private float nextSearch;
    private bool warned;

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += SetViewValues;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= SetViewValues;
        Shader.SetGlobalFloat(WristViewId, 0f);
        Shader.SetGlobalFloat(CutStartId, 0f);
        Shader.SetGlobalFloat(CutEndId, 0f);
        if (view != null) view.enabled = false;
    }

    // Only the wrist camera gets the wrist look: doors through walls, no distance fade, and the cut away
    private void SetViewValues(ScriptableRenderContext context, Camera camera)
    {
        bool wrist = view != null && camera == view;
        Shader.SetGlobalFloat(WristViewId, wrist ? 1f : 0f);
        float end = wrist ? Mathf.Max(distance - clearance, 0.01f) : 0f;
        Shader.SetGlobalFloat(CutEndId, end);
        Shader.SetGlobalFloat(CutStartId, wrist ? Mathf.Max(end - cutFade, 0f) : 0f);
    }

    private void OnDestroy()
    {
        if (copy != null) Destroy(copy);
        if (view != null) Destroy(view.gameObject);
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }
    }

    private void LateUpdate()
    {
        if ((copy == null || player == null) && Time.unscaledTime >= nextSearch)
        {
            nextSearch = Time.unscaledTime + SearchInterval;
            Refresh();
        }

        bool ready = copy != null && marker != null && player != null && level != null && view != null;
        if (unavailable != null) unavailable.SetActive(!ready);
        if (display != null) display.enabled = ready;

        // Only draws while the wrist monitor is showing it
        bool shown = ready && display.isActiveAndEnabled && display.canvasRenderer.GetInheritedAlpha() > 0.01f;
        if (shown) shown = UpdateTexture();
        if (view != null) view.enabled = shown;
        if (shown) Aim();
    }

    private void Refresh()
    {
        if (display == null) return;
        if (player == null) player = FindFirstObjectByType<ZeroGravity>();

        MapLevel found = FindLevel();
        if (found != level || copy == null)
        {
            if (copy != null) Destroy(copy);
            copy = null;
            marker = null;
            aimed = false;
            level = found;
            if (level != null && level.mapPrefab != null) CreateCopy();
        }
        if (copy != null && view == null) CreateView();
    }

    // The active scene's level first, since a menu or a loading scene can sit alongside it
    private static MapLevel FindLevel()
    {
        MapLevel best = null;
        foreach (MapLevel candidate in FindObjectsByType<MapLevel>(FindObjectsSortMode.None))
        {
            if (candidate.mapPrefab == null) continue;
            if (candidate.gameObject.scene == SceneManager.GetActiveScene()) return candidate;
            if (best == null) best = candidate;
        }
        return best;
    }

    private void CreateCopy()
    {
        int layer = LayerMask.NameToLayer(mapLayer);
        if (layer < 0)
        {
            if (warned) return;
            warned = true;
            Debug.LogWarning($"[HoloMap] No layer named '{mapLayer}'. Add it in Tags and Layers for the wrist map.", this);
            return;
        }

        copy = Instantiate(level.mapPrefab, CopyPosition, Quaternion.identity);
        copy.name = $"{level.mapPrefab.name} (Wrist)";
        foreach (Transform t in copy.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = layer;
        }

        marker = copy.GetComponentInChildren<MapPlayerMarker>(true);
    }

    private void CreateView()
    {
        // A child of the UI, so it survives level changes like the wrist monitor does
        var go = new GameObject("Wrist Map Camera");
        go.transform.SetParent(transform, false);
        view = go.AddComponent<Camera>();
        view.cullingMask = 1 << copy.layer;
        view.clearFlags = CameraClearFlags.SolidColor;
        // Solid, since the map's see-through lines would fade twice on a clear texture
        view.backgroundColor = background;
        view.nearClipPlane = 0.1f;
        view.enabled = false;
    }

    // Matches the image's shape, so the view never stretches
    private bool UpdateTexture()
    {
        Rect rect = display.rectTransform.rect;
        if (rect.width < 1f || rect.height < 1f) return false;

        int height = Mathf.Max(1, Mathf.RoundToInt(resolution * rect.height / rect.width));
        if (texture != null && texture.width == resolution && texture.height == height) return true;

        if (texture != null)
        {
            view.targetTexture = null;
            texture.Release();
            Destroy(texture);
        }
        texture = new RenderTexture(resolution, height, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.D24_UNorm_S8_UInt)
        {
            name = "Wrist Map",
            antiAliasing = 4
        };
        view.targetTexture = texture;
        display.texture = texture;
        return true;
    }

    private void Aim()
    {
        // Moved here first, so the camera doesn't trail the ball by a frame
        marker.Track(level.transform, player.transform);

        // Follows the player's whole view, Q-E roll included, so the map stays lined up with what they see
        Transform eye = player.cam != null ? player.cam.transform : player.transform;
        Quaternion target = copy.transform.rotation * Quaternion.Inverse(level.transform.rotation) * eye.rotation;
        facing = aimed ? Quaternion.Slerp(facing, target, 1f - Mathf.Exp(-turnSpeed * Time.unscaledDeltaTime)) : target;
        aimed = true;

        Quaternion look = facing * Quaternion.Euler(tilt, 0f, 0f);
        view.transform.SetPositionAndRotation(marker.transform.position - look * Vector3.forward * distance, look);
        view.fieldOfView = fieldOfView;
        view.farClipPlane = distance * 4f;
    }
}
