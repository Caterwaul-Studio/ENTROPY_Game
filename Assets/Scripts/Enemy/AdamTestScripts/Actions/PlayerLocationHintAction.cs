using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "PlayerLocationHint", story: "PlayerLocation hint as [InterestPt]", category: "Action", id: "3f098ec90c32a925f07291a006377864")]
public partial class PlayerLocationHintAction : Action
{
    [SerializeReference] public BlackboardVariable<Transform> InterestPt;
    [SerializeReference] public BlackboardVariable<bool> NewInterestPt;


    protected override Status OnStart()
    {
        GeistDirectorBrain brain = GameObject.GetComponent<GeistDirectorBrain>();
        if (brain == null) return Status.Failure;

        // timer not finished: fail immediately so the Try In Order moves on
        Waypoint hint = brain.TryConsumePlayerLocationHint();
        if (hint == null) return Status.Failure;

        InterestPt.Value = hint.transform;
        if (NewInterestPt != null) NewInterestPt.Value = true;

        Debug.Log($"[AOE] f{Time.frameCount} Player hint -> {hint.name} Interest Pt -> {InterestPt.Value.name}", brain);
        return Status.Success;
    }
}

