using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Save New Electrical Zone Location", story: "Save [NewElectricalZone] Location as [InterestPt]", category: "Action", id: "0c9592b4053fb18543eb074eb13208ff")]
public partial class SaveNewElectricalZoneLocationAction : Action
{
    [SerializeReference] public BlackboardVariable<Transform> NewElectricalZone;
    [SerializeReference] public BlackboardVariable<Transform> InterestPt;
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

