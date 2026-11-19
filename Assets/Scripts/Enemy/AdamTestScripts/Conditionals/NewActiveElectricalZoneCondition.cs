using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "NewActiveElectricalZone", story: "[NewEZoneActive] is [Equal] to [Value]", category: "Variable Conditions", id: "ee5413f487733a3be923be5c15819920")]
public partial class NewActiveElectricalZoneCondition : Condition
{
    [SerializeReference] public BlackboardVariable<bool> NewEZoneActive;
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
