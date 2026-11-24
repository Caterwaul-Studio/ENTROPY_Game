using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SetRandInterestPt", story: "Set Random [InterestPt] and flag [NewInterestPt]", category: "Action", id: "6b0ab49eefc59419724afe4acc7e2106")]
public partial class SetRandInterestPtAction : Action
{
    [SerializeReference] public BlackboardVariable<Transform> InterestPt;
    [SerializeReference] public BlackboardVariable<bool> NewInterestPt;
    protected override Status OnStart()
    {
        GeistDirectorBrain brain = GameObject.GetComponentInParent<GeistDirectorBrain>();
        if(brain == null)
        {
            Debug.LogError("GeistDirectorBrain not found in parent hierarchy.");
            return Status.Failure;
        }

        Waypoint pick = brain.GetRandomWaypoint();
        if(pick == null)
        {
            Debug.LogError("No random waypoint found.");
            return Status.Failure;
        }
        InterestPt.Value = pick.transform;
        NewInterestPt.Value = true;
        return Status.Success;
    }
}

