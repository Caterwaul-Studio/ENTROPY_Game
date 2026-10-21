using System.Collections;
using UnityEngine;
using UnityEngine.ProBuilder;

public enum GeistCutsceneEnd
{
    GoDormant,
    HandOffToAI,
    StayInPlace
}

[System.Serializable]

// one scripted run. Build these in the Inspector using the waypoints, call it in your Event script
public class GeistCutsceneRoute
{
    public string label = "New route";

    [Tooltip("ordered points the Geist travels through. Drag waypoint objects (or empty transforms) in here.")]
    public Transform[] points;

    public float speed = 12f;
    public float turnspeed = 10f;

    [Tooltip("Teleport to the first point before moving. The Geist is revealed at that spot. ")]
    public bool snapToFirstPoint = true;

    public GeistCutsceneEnd onFinish = GeistCutsceneEnd.GoDormant;
}


[RequireComponent(typeof(ComplexEnemyAI))]
public class CutSceneStates : MonoBehaviour
{
    private ComplexEnemyAI ai;
    private Coroutine running;

    //flag to read if running is not (t) or is (f) null
    public bool IsPlaying => running != null;

    private void Awake()
    {
        ai = GetComponent<ComplexEnemyAI>();
    }

    private void OnDisable()
    {
        running = null;
    }

    public void Play(GeistCutsceneRoute route, System.Action onFinished = null)
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
        if (ai == null) ai = GetComponent<ComplexEnemyAI>();

        // Place it Before activating so it never renders at its old spot
        if (route.snapToFirstPoint)
        {
            transform.position = route.points[0].position;
            if (route.points.Length > 1)
                Face(route.points[1].position - route.points[0].position, 1f);
        }

        //the geist can be seen again
        gameObject.SetActive(true);
        ai.SetControlMode(ComplexEnemyAI.ControlMode.Cutscene);
        running = StartCoroutine(RunRoute(route, onFinished));
    }

    public void Cancel()
    {
        if (running != null) StopCoroutine(running);
        running = null;
    }

    private IEnumerator RunRoute(GeistCutsceneRoute route, System.Action onFinished)
    {
        // wait one frame so ComplexEnemyAI.Start has run (first activation) before we move or hand back control
        yield return null;

        //if else statement, if(route.snapToFirstPoint) -> startIndex = 1, else -> startIndex = 0
        int startIndex = route.snapToFirstPoint ? 1 : 0;

        for (int i = startIndex; i < route.points.Length; i++)
        {
            Transform point = route.points[i];
            if (point == null) continue;

            while ((point.position - transform.position).sqrMagnitude > .0001f)
            {
                Vector3 toTarget = point.position - transform.position;
                Face(toTarget, Mathf.Clamp01(route.turnspeed * Time.deltaTime));

                transform.position = Vector3.MoveTowards(
                    transform.position, point.position, route.speed * Time.deltaTime);

                yield return null;
            }
        }

        //the run is over
        running = null;

        switch (route.onFinish)
        {
            case GeistCutsceneEnd.GoDormant:
                ai.SetControlMode(ComplexEnemyAI.ControlMode.AI);
                break;
            case GeistCutsceneEnd.HandOffToAI:
                ai.SetControlMode(ComplexEnemyAI.ControlMode.AI);
                break;
            case GeistCutsceneEnd.StayInPlace:
                break;
        }
        onFinished?.Invoke();
    }

    private void Face(Vector3 direction, float t)
    {
        if(direction.sqrMagnitude < 0.001f) return;
        Quaternion target = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, t);
    }
}
