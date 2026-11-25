using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "StraightLineChase", story: "Straight Line Chase", category: "Action", id: "48463fd6a0c13733382487744a48d9e8")]
public partial class StraightLineChaseAction : Action
{
    [SerializeReference] public BlackboardVariable<bool> PlayerInLOS;
    
    private GeistDirectorBrain brain;
    protected override Status OnStart()
    {
        brain = GameObject.GetComponentInParent<GeistDirectorBrain>();
        if(brain == null || !brain.HasPlayer)
        {
            Debug.LogWarning("ChasePlayerStraight: no GeistDirectorBrain or player reference");
            return Status.Failure;
        }

        brain.Path.Clear();
        brain.PathTarget = null;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        brain.ChaseStep();
        if(PlayerInLOS != null && PlayerInLOS.Value == false)
        {
            return Status.Failure;
        }

        return brain.GrabRangeFlagged ? Status.Success : Status.Running;
    }

    protected override void OnEnd()
    {
        brain = null;
    }
}

