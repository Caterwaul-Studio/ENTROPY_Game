using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "PlayerInGrabRange", story: "[PlayerInGrabRange] ?", category: "Conditions", id: "c15e6f82b8e8d77228a673a30ea347ac")]
public partial class PlayerInGrabRangeCondition : Condition
{
    [SerializeReference] public BlackboardVariable<bool> PlayerInGrabRange;

    public override bool IsTrue()
    {
        return PlayerInGrabRange != null && PlayerInGrabRange.Value;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
