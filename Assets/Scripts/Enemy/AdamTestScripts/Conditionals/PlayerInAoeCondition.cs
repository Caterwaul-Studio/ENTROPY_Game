using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "PlayerInAOE", story: "[PlayerInAoe] is [Equal] to [Value]", category: "Variable Conditions", id: "562bb077f1db7f63f2d86bc3ed5f1eff")]
public partial class PlayerInAoeCondition : Condition
{
    [SerializeReference] public BlackboardVariable<bool> PlayerInAoe;
    [Comparison(comparisonType: ComparisonType.Boolean)]
    [SerializeReference] public BlackboardVariable<ConditionOperator> Equal;
    [SerializeReference] public BlackboardVariable<bool> Value;

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
