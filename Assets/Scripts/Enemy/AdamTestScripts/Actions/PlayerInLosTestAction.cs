using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "PlayerInLOSTest", story: "[PlayerInLOS] ?", category: "Action", id: "f910fe32bfed461f7b235cb75617767e")]
public partial class PlayerInLosTestAction : Action
{
    [SerializeReference] public BlackboardVariable<bool> PlayerInLOS;

    protected override Status OnStart()
    {

        if (PlayerInLOS.Value)
        {
            Debug.Log("Player in LOS");
            return Status.Success;
        }

        return Status.Failure;
    }
}

