using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SavePlayerLastKnownPosition", story: "[LastKnownPlayerPos] as [InvestigatePt]", category: "Action", id: "c2f6fbe57fbc6a54337ef8535c216543")]
public partial class SavePlayerLastKnownPositionAction : Action
{
    [SerializeReference] public BlackboardVariable<Transform> InvestigatePt;
    [SerializeReference] public BlackboardVariable<Transform> LastKnownPlayerPos;

    protected override Status OnStart()
    {
        GeistDirectorBrain brain = GameObject.GetComponent<GeistDirectorBrain>();
        if (brain == null) return Status.Failure;

        if(brain.PlayerInLOS(out bool blinded) == false && LastKnownPlayerPos.Value != null)
        {
            InvestigatePt.Value = LastKnownPlayerPos.Value;
            Debug.Log("Geist going to last known player Pos");
            return Status.Success;
        }

        return Status.Failure;
    }
}

