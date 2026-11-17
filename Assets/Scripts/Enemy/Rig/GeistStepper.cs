using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections.Generic;

public class GeistStepper : MonoBehaviour
{
    [System.Serializable]
    public class TendrilPair
    {
        public Transform[] tendrils;
        public Transform ikTarget;
        public Transform defaultTarget;
        public AudioSource audioSource;

        [System.NonSerialized] public bool planted;
        [System.NonSerialized] public bool stepping;
        [System.NonSerialized] public Vector3 plant;
        [System.NonSerialized] public Vector3 stepStart;
        [System.NonSerialized] public Quaternion stepStartRotation;
        [System.NonSerialized] public Vector3 stepLift;
        [System.NonSerialized] public float stepProgress;
        [System.NonSerialized] public float nextSearchTime;
        [System.NonSerialized] public Vector3 hangVelocity;

        public Vector3 Origin
        {
            get
            {
                Vector3 sum = Vector3.zero;
                foreach (Transform tendril in tendrils)
                    sum += tendril.position;
                return sum / tendrils.Length;
            }
        }

        public Vector3 RestDirection
        {
            get
            {
                Vector3 sum = Vector3.zero;
                foreach (Transform tendril in tendrils)
                    sum += tendril.up;
                return sum.normalized;
            }
        }

        // Keeps the tip from twisting
        public Vector3 TwistAxis => tendrils[0].forward;
    }

    [Header("References")]
    public Transform body;
    public TendrilPair[] pairs;

    [Header("Step Area")]
    public float grabRange = 2.5f;
    public float grabAngle = 160f;
    [Tooltip("Degrees inside the grab cone where a plant behind the body starts a step")]
    public float stepBackZone = 25f;

    [Header("Reach")]
    [Tooltip("How far forward the tendrils reach, in degrees from straight out. Keep it under half the Grab Angle")]
    public float reachAngle = 65f;
    public float spreadAngle = 20f;
    public float maxBackDistance = 1f;
    [Tooltip("Direction of the eye stem, local to Body")]
    public Vector3 facingDirection = Vector3.forward;

    [Header("Gait")]
    public int maxConcurrentSteps = 2;
    [Tooltip("Pairs closer than this angle won't step at the same time")]
    [Range(0f, 180f)]
    public float minStepSeparation = 120f;

    [Header("Step Motion")]
    public float stepDuration = 0.5f;
    public float stepHeight = 0.3f;
    public float passiveMoveSpeed = 20f;

    [Header("Ground Detection")]
    public LayerMask groundMask;
    public float groundOffset = 0.05f;

    [Header("Audio")]
    public AudioClip[] tendrilAudioBank;

    private const int SearchRays = 8;
    private const float SearchInterval = 0.2f;
    private const float MinTravelSpeed = 0.1f;

    private Vector3 lastBodyPosition;
    private Vector3 bodyVelocity;

    private float StepAngle => grabAngle * 0.5f - stepBackZone;
    private bool IsTravelling => bodyVelocity.sqrMagnitude > MinTravelSpeed * MinTravelSpeed;

    void Reset()
    {
        GeistAmbientMotion ambientMotion = GetComponentInParent<GeistAmbientMotion>();
        if (ambientMotion != null)
            body = ambientMotion.transform;

        // Each IK target shared by 2+ tendrils becomes a pair (skips the eye)
        Dictionary<Transform, List<ChainIKConstraint>> chainsByTarget = new Dictionary<Transform, List<ChainIKConstraint>>();
        foreach (ChainIKConstraint chain in GetComponentsInChildren<ChainIKConstraint>())
        {
            Transform target = chain.data.target;
            if (target == null) continue;

            if (!chainsByTarget.TryGetValue(target, out List<ChainIKConstraint> chains))
                chainsByTarget[target] = chains = new List<ChainIKConstraint>();
            chains.Add(chain);
        }

        List<TendrilPair> found = new List<TendrilPair>();
        foreach (KeyValuePair<Transform, List<ChainIKConstraint>> group in chainsByTarget)
        {
            if (group.Value.Count < 2) continue;
            group.Value.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            Transform firstRoot = group.Value[0].data.root;

            found.Add(new TendrilPair
            {
                tendrils = group.Value.ConvertAll(chain => chain.transform).ToArray(),
                ikTarget = group.Key,
                // Tent1_2_Target -> Tent1_2_Default
                defaultTarget = FindChild(group.Key.name.Replace("_Target", "_Default")),
                audioSource = firstRoot != null ? firstRoot.GetComponentInChildren<AudioSource>(true) : null,
            });
        }

        found.Sort((a, b) => string.CompareOrdinal(a.ikTarget.name, b.ikTarget.name));
        pairs = found.ToArray();
    }

    void OnEnable()
    {
        lastBodyPosition = body.position;
        bodyVelocity = Vector3.zero;
    }

    void OnDisable()
    {
        // Don't leave a step half finished
        foreach (TendrilPair pair in pairs)
            if (pair.stepping) LetGo(pair);
    }

    void Start()
    {
        // Offset so the pairs don't all raycast on the same frame
        foreach (TendrilPair pair in pairs)
            pair.nextSearchTime = Time.time + Random.Range(0f, SearchInterval);
    }

    void Update()
    {
        TrackBodyVelocity();

        foreach (TendrilPair pair in pairs)
        {
            if (pair.stepping) UpdateStep(pair);
            else if (pair.planted) Hold(pair);
            else Hang(pair);
        }

        // Out of reach pairs step right away, ignoring the step limit
        foreach (TendrilPair pair in pairs)
            if (pair.planted && !pair.stepping && IsOutOfReach(pair))
                TryStep(pair, true);

        // Fill open step slots, most urgent first. Failed searches go on cooldown, so this ends.
        while (StepsInProgress() < maxConcurrentSteps)
        {
            TendrilPair next = MostUrgent();
            if (next == null) break;
            TryStep(next, false);
        }
    }

    // Let go of everything and re-grab
    public void ReleaseAll()
    {
        foreach (TendrilPair pair in pairs)
            LetGo(pair);
    }

    void LetGo(TendrilPair pair)
    {
        pair.planted = false;
        pair.stepping = false;
        pair.hangVelocity = Vector3.zero;
    }

    void Hold(TendrilPair pair)
    {
        pair.ikTarget.position = pair.plant;
        pair.ikTarget.rotation = PlantRotation(pair, pair.plant);
    }

    void Hang(TendrilPair pair)
    {
        Transform target = pair.ikTarget;
        target.position = Vector3.SmoothDamp(target.position, pair.defaultTarget.position, ref pair.hangVelocity, 1f / passiveMoveSpeed);
        target.rotation = Quaternion.Slerp(target.rotation, pair.defaultTarget.rotation, 1f - Mathf.Exp(-passiveMoveSpeed * Time.deltaTime));
    }

    bool IsOutOfReach(TendrilPair pair)
    {
        Vector3 toPlant = pair.plant - pair.Origin;
        return Vector3.Angle(pair.RestDirection, toPlant) > grabAngle * 0.5f || toPlant.magnitude > grabRange * 1.3f;
    }

    // 0 = fine, 1+ = wants to step (higher goes first)
    float StepUrgency(TendrilPair pair)
    {
        // Hanging pairs always want to grab
        if (!pair.planted) return 1f;

        Vector3 toPlant = pair.plant - pair.Origin;
        Vector3 lean = LeanDirection();
        float urgency = toPlant.magnitude / (grabRange * 1.1f);

        // Plants ahead are left alone, only ones that fall behind need to step
        float behind = Vector3.Dot(toPlant, -lean);
        if (lean == Vector3.zero || behind > 0f)
            urgency = Mathf.Max(urgency, Vector3.Angle(pair.RestDirection, toPlant) / StepAngle);
        if (behind > 0f)
            urgency = Mathf.Max(urgency, behind / maxBackDistance);

        return urgency >= 1f ? urgency : 0f;
    }

    TendrilPair MostUrgent()
    {
        TendrilPair best = null;
        float bestUrgency = 0f;

        foreach (TendrilPair pair in pairs)
        {
            if (pair.stepping || Time.time < pair.nextSearchTime || NeighbourIsStepping(pair)) continue;

            float urgency = StepUrgency(pair);
            if (urgency > bestUrgency)
            {
                best = pair;
                bestUrgency = urgency;
            }
        }

        return best;
    }

    bool NeighbourIsStepping(TendrilPair pair)
    {
        foreach (TendrilPair other in pairs)
            if (other != pair && other.stepping && Vector3.Angle(other.RestDirection, pair.RestDirection) < minStepSeparation)
                return true;

        return false;
    }

    int StepsInProgress()
    {
        int count = 0;
        foreach (TendrilPair pair in pairs)
            if (pair.stepping) count++;

        return count;
    }

    void TryStep(TendrilPair pair, bool mustStep)
    {
        if (FindPlant(pair, out Vector3 point, out Vector3 normal))
        {
            BeginStep(pair, point, normal);
            return;
        }

        // Nothing found. Keep holding unless it's out of reach, and try again soon
        if (mustStep)
            LetGo(pair);
        pair.nextSearchTime = Time.time + SearchInterval;
    }

    void BeginStep(TendrilPair pair, Vector3 point, Vector3 normal)
    {
        pair.stepStart = pair.ikTarget.position;
        pair.stepStartRotation = pair.ikTarget.rotation;
        pair.stepProgress = 0f;

        // Arc toward the body and off the surface. Short steps get a smaller arc
        Vector3 toBody = (pair.Origin - (pair.stepStart + point) * 0.5f).normalized;
        float height = Mathf.Min(stepHeight, Vector3.Distance(pair.stepStart, point) * 0.5f);
        pair.stepLift = (toBody + normal).normalized * height;

        pair.plant = point;
        pair.planted = true;
        pair.stepping = true;

        if (pair.audioSource != null && tendrilAudioBank.Length > 0)
        {
            pair.audioSource.clip = tendrilAudioBank[Random.Range(0, tendrilAudioBank.Length)];
            pair.audioSource.Play();
        }
    }

    void UpdateStep(TendrilPair pair)
    {
        pair.stepProgress = Mathf.Min(pair.stepProgress + Time.deltaTime / stepDuration, 1f);
        float t = pair.stepProgress;
        float eased = t * t * (3f - 2f * t);

        pair.ikTarget.position = Vector3.Lerp(pair.stepStart, pair.plant, eased) + pair.stepLift * Mathf.Sin(eased * Mathf.PI);
        pair.ikTarget.rotation = Quaternion.Slerp(pair.stepStartRotation, PlantRotation(pair, pair.plant), eased);

        if (pair.stepProgress >= 1f)
            pair.stepping = false;
    }

    bool FindPlant(TendrilPair pair, out Vector3 point, out Vector3 normal)
    {
        Vector3 origin = pair.Origin;
        Vector3 rest = pair.RestDirection;
        Vector3 lean = LeanDirection();
        float reach = Mathf.Min(reachAngle, grabAngle * 0.5f);

        bool found = false;
        float bestProgress = 0f;
        point = normal = Vector3.zero;

        for (int i = 0; i < SearchRays; i++)
        {
            Vector3 dir;
            if (lean != Vector3.zero)
            {
                // Fan rays from straight out toward the front. The steep ones find surfaces far ahead in tight spaces
                float pitch = reach * (i + Random.value) / SearchRays;
                dir = Quaternion.AngleAxis(Random.Range(-spreadAngle, spreadAngle), lean)
                    * Vector3.RotateTowards(rest, lean, pitch * Mathf.Deg2Rad, 0f);
            }
            else
            {
                // No forward direction, so just pick near straight out (sqrt keeps the rays evenly spread)
                dir = Quaternion.AngleAxis(Random.Range(0f, 360f), rest)
                    * Quaternion.AngleAxis(spreadAngle * Mathf.Sqrt(Random.value), Vector3.Cross(rest, pair.TwistAxis).normalized)
                    * rest;
            }

            if (!Physics.Raycast(origin, dir, out RaycastHit hit, grabRange, groundMask)) continue;

            // Keep the hit furthest ahead
            float progress = Vector3.Dot(hit.point - origin, lean);
            if (!found || progress > bestProgress)
            {
                found = true;
                bestProgress = progress;
                point = hit.point + hit.normal * groundOffset;
                normal = hit.normal;
            }
        }

        return found;
    }

    // Travel direction while moving, facing direction while still
    Vector3 LeanDirection()
    {
        Vector3 lean = IsTravelling ? bodyVelocity : body.TransformDirection(facingDirection);
        return lean.sqrMagnitude > 1e-6f ? lean.normalized : Vector3.zero;
    }

    // Point the tip from the body to the plant, keeping the root's twist
    Quaternion PlantRotation(TendrilPair pair, Vector3 plant)
    {
        Vector3 up = plant - body.position;
        Vector3 forward = Vector3.ProjectOnPlane(pair.TwistAxis, up);
        if (forward.sqrMagnitude < 1e-6f)
            return pair.ikTarget.rotation;

        return Quaternion.LookRotation(forward, up);
    }

    void TrackBodyVelocity()
    {
        if (Time.deltaTime <= 0f) return;

        Vector3 velocity = (body.position - lastBodyPosition) / Time.deltaTime;
        lastBodyPosition = body.position;

        // Smoothed so one jerky frame doesn't matter
        bodyVelocity = Vector3.Lerp(bodyVelocity, velocity, 1f - Mathf.Exp(-8f * Time.deltaTime));
    }

    Transform FindChild(string childName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            if (child.name == childName) return child;

        return null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (body != null)
        {
            // Facing direction, facing towards eye stem
            Gizmos.color = Color.blue;
            DrawArrow(body.position, body.TransformDirection(facingDirection).normalized * grabRange);

            // Velocity. Magenta when moving, grey when too slow to count
            if (Application.isPlaying)
            {
                Gizmos.color = IsTravelling ? Color.magenta : Color.gray;
                DrawArrow(body.position, bodyVelocity);
            }
        }

        if (pairs == null || body == null) return;

        foreach (TendrilPair pair in pairs)
        {
            if (pair.tendrils == null || pair.tendrils.Length == 0 || System.Array.Exists(pair.tendrils, t => t == null))
                continue;

            Vector3 apex = pair.Origin;
            Vector3 rest = pair.RestDirection;

            // Grab cone
            Gizmos.color = Color.cyan;
            DrawCone(apex, rest, pair.TwistAxis, grabAngle * 0.5f);

            // Step cone
            Gizmos.color = new Color(1f, 0.5f, 0f);
            DrawCone(apex, rest, pair.TwistAxis, StepAngle);

            // Reach area
            Gizmos.color = Color.green;
            DrawReach(apex, rest, pair.TwistAxis);

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(apex, rest * grabRange);

            if (!Application.isPlaying || !pair.planted) continue;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(pair.plant, 0.1f);
            Gizmos.DrawLine(apex, pair.plant);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(body.position, pair.plant - body.position);
        }
    }

    // Draws the reach fan at the center and both spread edges
    void DrawReach(Vector3 apex, Vector3 rest, Vector3 twistAxis)
    {
        Vector3 lean = LeanDirection();
        if (lean == Vector3.zero)
        {
            DrawCone(apex, rest, twistAxis, spreadAngle);
            return;
        }

        float reach = Mathf.Min(reachAngle, grabAngle * 0.5f);
        int segments = 12;

        for (int edge = -1; edge <= 1; edge++)
        {
            Quaternion spread = Quaternion.AngleAxis(edge * spreadAngle, lean);
            Vector3 prevPoint = Vector3.zero;

            for (int i = 0; i <= segments; i++)
            {
                Vector3 dir = spread * Vector3.RotateTowards(rest, lean, reach * i / segments * Mathf.Deg2Rad, 0f);
                Vector3 rimPoint = apex + dir * grabRange;

                if (i > 0) Gizmos.DrawLine(prevPoint, rimPoint);
                prevPoint = rimPoint;
            }

            Gizmos.DrawLine(apex, prevPoint);
        }
    }

    void DrawArrow(Vector3 start, Vector3 vector)
    {
        if (vector.sqrMagnitude < 1e-6f) return;

        Vector3 end = start + vector;
        Vector3 back = -vector.normalized * Mathf.Min(0.25f, vector.magnitude * 0.3f);
        Vector3 side = Vector3.Cross(vector, Vector3.up);
        if (side.sqrMagnitude < 1e-6f)
            side = Vector3.Cross(vector, Vector3.right);
        side = side.normalized * back.magnitude * 0.5f;

        Gizmos.DrawLine(start, end);
        Gizmos.DrawLine(end, end + back + side);
        Gizmos.DrawLine(end, end + back - side);
    }

    void DrawCone(Vector3 apex, Vector3 axis, Vector3 twistAxis, float halfAngle)
    {
        Vector3 side = Vector3.Cross(axis, twistAxis).normalized;
        int segments = 32;
        Vector3 prevPoint = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            Vector3 dir = Quaternion.AngleAxis(i * 360f / segments, axis)
                        * Quaternion.AngleAxis(halfAngle, side)
                        * axis;
            Vector3 rimPoint = apex + dir * grabRange;

            if (i > 0) Gizmos.DrawLine(prevPoint, rimPoint);
            prevPoint = rimPoint;

            if (i % 8 == 0) Gizmos.DrawLine(apex, rimPoint);
        }
    }
#endif
}
