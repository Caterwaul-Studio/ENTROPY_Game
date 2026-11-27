using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "CluePtNull", story: "[CluePt] Not Null?", category: "Conditions", id: "aee4c65078ff2e3ea555014235dadeb1")]
public partial class CluePtNullCondition : Condition
{
    [SerializeReference] public BlackboardVariable<Transform> CluePt;

    public override bool IsTrue()
    {
        if (CluePt.Value != null)
        {
            Debug.Log("<color=red>Geist already has a clue to player position</color>");
            return true;
        }
        return false;
    }
}
