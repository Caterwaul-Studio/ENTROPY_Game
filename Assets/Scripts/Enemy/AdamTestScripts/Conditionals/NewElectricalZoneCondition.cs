using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "NewElectricalZone", story: "New [ElectricalZone] Activated", category: "Conditions", id: "6036f775fc84b9b9e6d6eb9d64bb903f")]
public partial class NewElectricalZoneCondition : Condition
{
    [SerializeReference] public BlackboardVariable<bool> ElectricalZone;

    public override bool IsTrue()
    {
        return true;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
