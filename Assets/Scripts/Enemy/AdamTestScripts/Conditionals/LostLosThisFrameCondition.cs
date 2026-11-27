using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "LostLOSThisFrame", story: "[LostLOSThisFrame] ? using flag [PlayerInLOS]", category: "Conditions", id: "e5b8fba108790cefa49642c15c0d2cdd")]
public partial class LostLosThisFrameCondition : Condition
{
    [SerializeReference] public BlackboardVariable<bool> LostLOSThisFrame;
    [SerializeReference] public BlackboardVariable<bool> PlayerInLOS;

    public override bool IsTrue()
    {
        //if PLayerInLOS is false and LostLOSThisFrame is false
        if(PlayerInLOS == false && LostLOSThisFrame == false)
        {
            Debug.Log("<color=red>Geist lost LOS of player this frame </color>");
            LostLOSThisFrame.Value = true;
            return true;
        }
        return false;
    }
}
