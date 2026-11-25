using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "PlayerInAOETest", story: "Player In AOE", category: "Action", id: "d2339896ea066f8416e30418f47e8b14")]
public partial class PlayerInAoeTestAction : Action
{

    [SerializeReference] public BlackboardVariable<bool> PlayerInAOE;
    [SerializeReference] public BlackboardVariable<bool> GeistBlinded;

    protected override Status OnStart()
    {

        if (PlayerInAOE.Value)
        {
            Debug.Log("<color=orange>Player in AOE</color>");
            return Status.Success;
        }

        return Status.Failure;
    }
}

