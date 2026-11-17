using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AstarToInterestPt", story: "Calc A* To [InterestPt]", category: "Action", id: "4c2b5a9f79277b7653dde23916d03336")]
public partial class AstarToInterestPtAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> InterestPt;
    [SerializeReference] public BlackboardVariable<bool> NewInterestPt;

    protected override Status OnStart()
    {
        GeistDirectorBrain brain = GameObject.GetComponentInParent<GeistDirectorBrain>();
        if(brain == null || InterestPt.Value == null)
        {
            return Status.Failure;
        }

        // Interest pt may be waypoint itself, something near one
        Waypoint goal = InterestPt.Value.GetComponent<Waypoint>();
        if(goal == null) goal = brain.FindClosestWaypoint(InterestPt.Value.transform.position);

        Waypoint start = brain.CurrentWaypoint != null
            ? brain.CurrentWaypoint : brain.FindClosestWaypoint(brain.transform.position);

        brain.Path = brain.FindPath(start, goal);
        brain.PathTarget = InterestPt.Value.transform;

        NewInterestPt.Value = false;

        //Empty path with a different goal means unreachable
        if (brain.Path.Count == 0 && start != goal) return Status.Failure;
        return Status.Success;

    }  
}

