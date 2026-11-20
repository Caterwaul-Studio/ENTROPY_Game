using UnityEngine;

/// <summary>
/// Moves the map's player marker to where the player is in the level, so the hologram shows where you are.
/// Works wherever the map is shown and at any size, since it places the marker inside the map itself.
/// The Map Builder puts this on the baked map's Player marker.
/// </summary>
public class MapPlayerMarker : MonoBehaviour
{
    // Looking for the level and the player is slow, so when one is missing it's tried again only now and then
    const float SearchInterval = 1f;

    [Tooltip("The scene this map was baked from, so the marker follows the player through that level.")]
    public string levelScene;

    [Tooltip("Warns in the Console when the ball stays hidden because its level or the player can't be found.")]
    public bool debugLogging = true;

    // How long to look before warning, so a level still loading doesn't count
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

        // Hidden until there's a player to show, such as in a menu scene
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

    /// <summary>
    /// Puts the marker where the player is. The map is baked relative to the level root, so the player's
    /// spot relative to the level root is the marker's spot inside the map.
    /// </summary>
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
