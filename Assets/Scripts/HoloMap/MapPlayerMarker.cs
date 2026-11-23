using UnityEngine;

public class MapPlayerMarker : MonoBehaviour
{
    const float SearchInterval = 1f;

    public string levelScene;
    public bool debugLogging = true;

    const float WarnAfter = 3f;

    private MapLevel level;
    private ZeroGravity player;
    private Renderer ball;
    private float nextSearch;
    private float lookingSince = -1f;
    private bool warned;

    private void Awake()
    {
        ball = GetComponent<Renderer>();
    }

    private void LateUpdate()
    {
        if ((level == null || player == null) && Time.unscaledTime >= nextSearch)
        {
            nextSearch = Time.unscaledTime + SearchInterval;
            if (level == null) level = FindLevel(levelScene);
            if (player == null) player = FindFirstObjectByType<ZeroGravity>();
        }

        bool tracking = level != null && player != null;
        if (ball != null) ball.enabled = tracking;
        if (tracking) Track(level.transform, player.transform);
        else WarnIfLost();
    }

    private void WarnIfLost()
    {
        if (!debugLogging || warned) return;
        if (lookingSince < 0f) lookingSince = Time.unscaledTime;
        if (Time.unscaledTime - lookingSince < WarnAfter) return;

        warned = true;
        if (level == null) Debug.LogWarning($"[HoloMap] The player ball is hidden: no MapLevel found in a loaded scene named '{levelScene}'. Add one to the level root (Map Builder) and save the scene.", this);
        if (player == null) Debug.LogWarning("[HoloMap] The player ball is hidden: no ZeroGravity player found.", this);
    }

    public void Track(Transform levelRoot, Transform playerTransform)
    {
        transform.localPosition = levelRoot.InverseTransformPoint(playerTransform.position);
    }

    public static MapLevel FindLevel(string sceneName)
    {
        foreach (MapLevel candidate in FindObjectsByType<MapLevel>(FindObjectsSortMode.None))
        {
            if (candidate.gameObject.scene.name == sceneName) return candidate;
        }
        return null;
    }
}
