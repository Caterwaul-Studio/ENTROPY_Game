using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "PlayerWithinChaseDistance", story: "[PlayerWithinChaseDistance] ?", category: "Conditions", id: "0f7164f4b00f8adb3194fa40e9bbaf22")]
public partial class PlayerWithinChaseDistanceCondition : Condition
{
    [SerializeReference] public BlackboardVariable<bool> PlayerWithinChaseDistance;

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
