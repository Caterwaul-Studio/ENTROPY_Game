using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "NewInterestPt", story: "New [InterestPt] ?", category: "Conditions", id: "3adaa0559b8ff204943db6702c52e204")]
public partial class NewInterestPtCondition : Condition
{
    [SerializeReference] public BlackboardVariable<Transform> InterestPt;

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
