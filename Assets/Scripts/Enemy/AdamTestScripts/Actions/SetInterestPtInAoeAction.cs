using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SetInterestPtInAOE", story: "Set [InterestPt] within AOE and flag [NewInterestPt]", category: "Action", id: "2c6ec13fdf55e24b664b81c162703a97")]
public partial class SetInterestPtInAoeAction : Action
{
    [SerializeReference] public BlackboardVariable<Transform> InterestPt;
    [SerializeReference] public BlackboardVariable<bool> NewInterestPt;

    protected override Status OnStart()
    {
        GeistDirectorBrain brain = GameObject.GetComponentInParent<GeistDirectorBrain>();
        if (brain == null)
        {
            Debug.LogError("GeistDirectorBrain not found in parent hierarchy.");
            return Status.Failure;
        }
        //Debug.Log($"[AOE] f{Time.frameCount} Set START path={brain.Path.Count} current={(InterestPt.Value != null ? InterestPt.Value.name : "none")}");

        if (brain.Path.Count > 0 && InterestPt.Value != null)
        {
            NewInterestPt.Value = true; // keeps the Pass If in front of Calc A* open; A* will skip the replan
            //Debug.Log($"[AOE] f{Time.frameCount} Set KEEP existing point {InterestPt.Value.name}");
            return Status.Success;
        }

        Waypoint pick = brain.GetRandomWaypointInAOE();
        if (pick == null)
        {
            Debug.LogWarning("SetInterestPtInAOE: no reachable waypoint inside the AOE");
            return Status.Failure;
        }

        InterestPt.Value = pick.transform;
        NewInterestPt.Value = true; // opens the Pass If in front of Calc A*
        //Debug.Log($"[AOE] f{Time.frameCount} Set PICKED {pick.name}, flag={NewInterestPt.Value}");
        return Status.Success;
    }
}

