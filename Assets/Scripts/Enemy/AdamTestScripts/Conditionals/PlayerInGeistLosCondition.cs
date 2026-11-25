using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "PlayerInGeistLOS", story: "[PlayerInLOS] && ![GeistBlinded]?", category: "Conditions", id: "ee7f3c46c37120933b9601f51d581b08")]
public partial class PlayerInGeistLosCondition : Condition
{
    [SerializeReference] public BlackboardVariable<bool> PlayerInLOS;
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

        bool inLOS = brain.PlayerInLOS(out bool blinded);
        PlayerInLOS.Value = inLOS;
        GeistBlinded.Value = blinded;
        return inLOS && !blinded;
    }
}
