using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "MoveToInterestPt", story: "[Geist] Moves to [InterestPt]", category: "Action", id: "a8de3613a89fbe9b4637ff820e83fa86")]
public partial class MoveToInterestPtAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Geist;
    [SerializeReference] public BlackboardVariable<Transform> InterestPt;
    [SerializeReference] public BlackboardVariable<bool> PlayerInAOE;

    private GeistDirectorBrain brain;

    protected override Status OnStart()
    {
        GameObject geistObj = Geist.Value != null ? Geist.Value : GameObject;
        brain = geistObj.GetComponent<GeistDirectorBrain>();
        if(brain == null)
        {
            Debug.LogError("GeistDirectorBrain component not found on the Geist GameObject.");
            return Status.Failure;
        }
        Debug.Log($"[AOE] f{Time.frameCount} Move START path={brain.Path.Count}");
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        bool done = brain.Step();
        if(done) Debug.Log($"[AOE] f{Time.frameCount} Move DONE");
        return done ? Status.Success : Status.Running;
    }

    protected override void OnEnd()
    {
        brain = null;
    }
}

