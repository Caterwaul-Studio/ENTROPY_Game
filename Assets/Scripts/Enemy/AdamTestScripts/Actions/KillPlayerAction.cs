using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "KillPlayer", story: "Kill Player", category: "Action", id: "e5a4c78d2502c112002a9ccadf6c0f8c")]
public partial class KillPlayerAction : Action
{
    private GeistDirectorBrain brain;
    private float elapsed;

    protected override Status OnStart()
    {
        brain = GameObject.GetComponentInParent<GeistDirectorBrain>();
        if (brain == null || !brain.HasPlayerRefs)
        {
            Debug.LogWarning("KillPlayer: no GeistDirectorBrain or player references");
            return Status.Failure;
        }

        // normally GrabPlayer already did this; keeps Kill working on its own
        if (!brain.IsGrabbingPlayer) brain.LockPlayerInputs();

        elapsed = 0f;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        brain.FacePlayerStep();

        // hold the player for a few seconds (old GrabAndWait)
        elapsed += Time.deltaTime;
        if (elapsed < brain.KillHoldDuration) return Status.Running;

        brain.KillPlayer();
        return Status.Success;
    }

    protected override void OnEnd()
    {
        brain = null;
    }
}

