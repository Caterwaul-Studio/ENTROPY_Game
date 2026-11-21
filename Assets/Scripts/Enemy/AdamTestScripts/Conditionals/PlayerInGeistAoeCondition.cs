using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "PlayerInGeistAOE", story: "[PlayerInAOE] && ![GeistBlinded]?", category: "Conditions", id: "b7c824375c9ab471b66a5883706ddfd6")]
public partial class PlayerInGeistAoeCondition : Condition
{
    [SerializeReference] public BlackboardVariable<bool> PlayerInAOE;
    [SerializeReference] public BlackboardVariable<bool> GeistBlinded;

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

        bool inAoe = brain.PlayerInAOE(out bool blinded);
        PlayerInAOE.Value = inAoe;
        GeistBlinded.Value = blinded;
        return inAoe && !blinded;
    }
}
