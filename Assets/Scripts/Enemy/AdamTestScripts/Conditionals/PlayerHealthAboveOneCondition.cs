using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "PlayerHealthAboveOne", story: "[PlayerHealthAbove1] ?", category: "Conditions", id: "efb1d95d8dc0cedcb0970001a2af3499")]
public partial class PlayerHealthAboveOneCondition : Condition
{
    [SerializeReference] public BlackboardVariable<bool> PlayerHealthAbove1;

    private ZeroGravity player;

    public override bool IsTrue()
    {
        if (player == null)
        {
            PersistantManager persistant = UnityEngine.Object.FindFirstObjectByType<PersistantManager>();
            if (persistant != null) player = persistant.Player;
        }
        if (player == null) return false;

        // old GrabAttackLogic: health <= 1 killed the player, anything higher grabbed and threw
        bool above = player.PlayerHealth > 1;

        // keep the blackboard bool in step so the debug view shows it
        if (PlayerHealthAbove1 != null) PlayerHealthAbove1.Value = above;

        return above;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
