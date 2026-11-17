using UnityEngine;

public enum GeistsCutsceneEnd
{
    GoDormant,
    HandOffToAI,
    StayInPlace
}

[System.Serializable]

// one scripted run. Build these in the Inspector using the waypoints, call it in your Event script
public class GeistsCutsceneRoute
{
    public string label = "New route";

    [Tooltip("ordered points the Geist travels through. Drag waypoint objects (or empty transforms) in here.")]
    public Transform[] points;

    public float speed = 12f;
    public float turnspeed = 10f;

    [Tooltip("Teleport to the first point before moving. The Geist is revealed at that spot. ")]
    public bool snapToFirstPoint = true;

    public GeistsCutsceneEnd onFinish = GeistsCutsceneEnd.GoDormant;
}

[RequireComponent(typeof(GeistDirectorBrain))]
public class GeistCutSceneMode : MonoBehaviour
{
    private GeistDirectorBrain brain;

    //flag to read if running is not (t) or is (f) null
    public bool IsPlaying => brain != null && brain.IsCutscenePlaying;

    private void Awake()
    {
        brain = GetComponent<GeistDirectorBrain>();
    }

    private void OnDisable()
    {
        if(brain != null)
        {
            brain.CancelCutscene();
        }        
    }

    public void Play(GeistsCutsceneRoute route, System.Action onFinished = null)
    {
        if (route == null || route.points == null || route.points.Length == 0)
        {
            Debug.LogWarning("[GeistCutscene] Route is empty, nothing to play");
            return;
        }
        if (IsPlaying)
        {
            Debug.LogWarning($"[GeistCutscene] Already playing, ignored '{route.label}'");
            return;
        }
        if (brain == null) brain = GetComponent<GeistDirectorBrain>();

        // Place it Before activating so it never renders at its old spot
        if (route.snapToFirstPoint)
        {
            transform.position = route.points[0].position;
            if (route.points.Length > 1)
            {
                Vector3 dir = route.points[1].position - route.points[0].position;
                if (dir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(dir);
                }
            }
                
        }

        //the geist can be seen again
        gameObject.SetActive(true);
        brain.BeginCutscene(route, onFinished);
    }

    public void Cancel()
    {
        if (brain != null)
        {
            brain.CancelCutscene();
        }
    }

}
