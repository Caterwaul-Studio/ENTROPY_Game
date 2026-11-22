using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "PlayerInGeistAOE", story: "[PlayerInAOE] ?", category: "Conditions", id: "b7c824375c9ab471b66a5883706ddfd6")]
public partial class PlayerInGeistAoeCondition : Condition
{
    [SerializeReference] public BlackboardVariable<bool> PlayerInAOE;

    private GeistDirectorBrain brain;

    public override bool IsTrue()
    {
        if (brain == null)
            brain = GameObject.GetComponentInParent<GeistDirectorBrain>();
        if (brain == null)
        {
            Debug.LogError("GeistDirectorBrain not found in parent hierarchy.");
            return false;
        }

        bool inAoe = brain.PlayerInAOE();
        PlayerInAOE.Value = inAoe;
        return inAoe;
    }
}
