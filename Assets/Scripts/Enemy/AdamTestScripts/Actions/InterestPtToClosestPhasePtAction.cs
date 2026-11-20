using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "InterestPtToClosestPhasePt", story: "Set [InterestPt] to closest PhasePt", category: "Action", id: "229a36d378b2c3e305b2a4d45c8a000e")]
public partial class InterestPtToClosestPhasePtAction : Action
{
    [SerializeReference] public BlackboardVariable<Transform> InterestPt;
    [SerializeReference] public BlackboardVariable<bool> GeistInPoweredZone;

    protected override Status OnStart()
    {
        GeistDirectorBrain brain = GameObject.GetComponentInParent<GeistDirectorBrain>();
        if (brain == null) return Status.Failure;

        Debug.Log($"[Phase] bailing. Geist at {brain.transform.position}, inside a powered zone: {brain.IsPositionInPoweredZone(brain.transform.position)}");

        Waypoint phase = brain.GetClosestPhasingWaypoint(brain.transform.position);
        if (phase == null)
        {
            Debug.LogWarning("SetInterestPtToClosestPhase: no phasing waypoints registered");
            return Status.Failure;
        }

        InterestPt.Value = phase.transform;
        brain.SetDirectPath(phase);       // straight there, ignores A*
        GeistInPoweredZone.Value = false; // consumed: the escape is committed
        return Status.Success;
    }
}

