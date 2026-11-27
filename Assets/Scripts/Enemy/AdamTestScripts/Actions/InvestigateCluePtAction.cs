using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "InvestigateCluePt", story: "Investigate [CluePt]", category: "Action", id: "da3289aff1b925d8e6e3f154969d3ba7")]
public partial class InvestigateCluePtAction : Action
{
    [SerializeReference] public BlackboardVariable<Transform> CluePt;
    private GeistDirectorBrain brain;
    private Vector3 point;
    private bool followingWaypoints;
    protected override Status OnStart()
    {
        brain = GameObject.GetComponent<GeistDirectorBrain>();
        if (brain == null || CluePt == null || CluePt.Value == null)
            return Status.Failure;

        point = CluePt.Value.position; // snapshot
        followingWaypoints = brain.BuildInvestigatePath(point);
        Debug.Log($"[Investigate] f{Time.frameCount} waypoints={brain.Path.Count}, then {point}", brain);
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (followingWaypoints)
        {
            if (brain.Step()) followingWaypoints = false; // waypoints done, head to the point
            return Status.Running;
        }

        if (brain.StepToPoint(point))
        {
            Debug.Log("Geist reached the InvestigatePt");
            brain.ClearLastKnownPlayerPos(); // one shot: don't re-investigate the same spot
            CluePt.Value = null; //clear the clue point;
            return Status.Success;
        }
        return Status.Running;
    }

    protected override void OnEnd()
    {
        brain = null;
    }
}

