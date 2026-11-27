using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SaveLocationOfSoundOfInterest", story: "SoundofInterest set to [CluePt]", category: "Action", id: "fbf294d5178877e1e5b83cb2bdaa53e8")]
public partial class SaveLocationOfSoundOfInterestAction : Action
{
    [SerializeReference] public BlackboardVariable<Transform> CluePt;
    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

