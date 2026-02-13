using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class TendrilBehavior : MonoBehaviour
{
    public TendrilOrigin origin;
    public EnemySimpleAI ownerEnemy;  // Assigned during initialization
    public ComplexEnemyAI compOwnerEnemy;
    public float maxLength = 6f;
    public float extendSpeed = 5f;
    public float retractSpeed = 5f;
    public float holdDuration = 2f;
    private LineRenderer lineRenderer;
    public bool manualRetract = false;
    public bool retractCalled = false;

    private Vector3 targetPoint;
    private float progress = 0f;
    public bool retracting = false;
    private float holdTimer = 0f;
    private bool initialized = false;

    //curves
    public int curveResolution = 20; // number of points on the curve
    public float curveAmount = 2f;   // how far the curve bends outward
    private Vector3 curveDirectionOffset;

    public void Initialize(TendrilOrigin originPoint, EnemySimpleAI owner, bool retractsManually)
    {
        origin = originPoint;
        origin.activeTendril = this;
        ownerEnemy = owner;
        manualRetract = retractsManually;
        initialized = true;

        lineRenderer = GetComponentInChildren<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError("LineRenderer not found in children of " + gameObject.name);
            return;
        }

        lineRenderer.positionCount = 2;
        Vector3 endPoint = origin.transform.position + origin.transform.forward * 0.1f;

        // Pick a random angle around the forward axis
        float angle = Random.Range(0f, 360f);

        // Use Quaternion to rotate the up vector around the forward axis
        curveDirectionOffset = Quaternion.AngleAxis(angle, origin.transform.forward) * Vector3.up;

        lineRenderer.SetPosition(0, origin.transform.position);
        lineRenderer.SetPosition(1, endPoint);

        // Raycast to find wall hit point
        if (Physics.Raycast(origin.transform.position, origin.transform.forward, out RaycastHit hit, maxLength))
            targetPoint = hit.point;
        else
            targetPoint = origin.transform.position + origin.transform.forward * maxLength;
    }

    public void Initialize(TendrilOrigin originPoint, ComplexEnemyAI owner, bool retractsManually)
    {
        origin = originPoint;
        origin.activeTendril = this;
        compOwnerEnemy = owner;
        manualRetract = retractsManually;
        initialized = true;

        lineRenderer = GetComponentInChildren<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError("LineRenderer not found in children of " + gameObject.name);
            return;
        }

        lineRenderer.positionCount = 2;
        Vector3 endPoint = origin.transform.position + origin.transform.forward * 0.1f;

        // Pick a random angle around the forward axis
        float angle = Random.Range(0f, 360f);

        // Use Quaternion to rotate the up vector around the forward axis
        curveDirectionOffset = Quaternion.AngleAxis(angle, origin.transform.forward) * Vector3.up;

        lineRenderer.SetPosition(0, origin.transform.position);
        lineRenderer.SetPosition(1, endPoint);

        // Raycast to find wall hit point
        if (Physics.Raycast(origin.transform.position, origin.transform.forward, out RaycastHit hit, maxLength))
            targetPoint = hit.point;
        else
            targetPoint = origin.transform.position + origin.transform.forward * maxLength;
    }

    void Update()
    {
        if (!initialized || origin == null)
        {
            Debug.LogWarning("TendrilBehavior was not initialized with a valid origin.");
            return;
        }

        // Handle extension/retraction timing
        if (!retracting)
        {
            progress += Time.deltaTime * extendSpeed;

            if (progress >= 1f)
            {
                progress = 1f;

                if (!manualRetract)
                {
                    holdTimer += Time.deltaTime;
                    if (holdTimer >= holdDuration)
                    {
                        retracting = true;
                    }
                }
            }
        }
        else // we're retracting (either auto or manual)
        {
            progress -= Time.deltaTime * retractSpeed;
            if (progress <= 0f)
            {
                DestroyTendril();
                return;
            }
        }

        UpdateLineRenderer();
    }

    void UpdateLineRenderer()
    {
        if (lineRenderer == null || !initialized) return;

        Vector3 p0 = origin.transform.position;
        Vector3 p1 = GetControlPoint();
        Vector3 p2 = Vector3.Lerp(origin.transform.position, targetPoint, progress); // animate towards target

        lineRenderer.positionCount = curveResolution;
        for (int i = 0; i < curveResolution; i++)
        {
            float t = (float)i / (curveResolution - 1);
            Vector3 point = CalculateQuadraticBezierPoint(t, p0, p1, p2);
            lineRenderer.SetPosition(i, point);
        }
    }

    Vector3 GetControlPoint()
    {
        Vector3 midPoint = (origin.transform.position + targetPoint) * 0.5f;
        //Vector3 dir = (targetPoint - origin.transform.position).normalized;
        //Vector3 curve = Vector3.Cross(dir, Vector3.up).normalized;
        return midPoint + curveDirectionOffset.normalized * curveAmount;
    }

    Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        return Mathf.Pow(1 - t, 2) * p0 +
               2 * (1 - t) * t * p1 +
               Mathf.Pow(t, 2) * p2;
    }

    public void Retract()
    {
        if (!initialized || retracting) return;

        retracting = true;
        // This allows us to override the "hold forever" logic
        holdTimer = holdDuration;
        retractCalled = true;
    }

    void DestroyTendril()
    {
        if (origin != null)
        {
            if (origin.activeTendril == this)
            {
                origin.activeTendril = null;
            }
        }

        if (ownerEnemy != null && !ownerEnemy.availableOrigins.Contains(origin))
        {
            ownerEnemy.availableOrigins.Add(origin);
        }

        Destroy(gameObject);
    }
}
