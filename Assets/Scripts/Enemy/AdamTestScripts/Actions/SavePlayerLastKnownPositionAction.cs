using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SavePlayerLastKnownPosition", story: "[LastKnownPlayerPos] as [InterestPt]", category: "Action", id: "c2f6fbe57fbc6a54337ef8535c216543")]
public partial class SavePlayerLastKnownPositionAction : Action
{
    [SerializeReference] public BlackboardVariable<Transform> InterestPt;
    [SerializeReference] public BlackboardVariable<Transform> LastKnownPlayerPos;
    [SerializeReference] public BlackboardVariable<bool> NewInterestPt;

    protected override Status OnStart()
    {
        GeistDirectorBrain brain = GameObject.GetComponent<GeistDirectorBrain>();
        if (brain == null) return Status.Failure;

        if(brain.PlayerInLOS(out bool blinded) == false)
        {
            InterestPt.Value = LastKnownPlayerPos.Value;
            NewInterestPt.Value = true;
            Debug.Log("Geist going to last known player Pos");
            return Status.Success;
        }

        return Status.Failure;
    }
}

