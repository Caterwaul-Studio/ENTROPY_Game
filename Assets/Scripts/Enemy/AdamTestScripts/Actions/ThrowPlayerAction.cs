using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ThrowPlayer", story: "Throw Player", category: "Action", id: "7dca3ab6b31e24d845e777151e09458a")]
public partial class ThrowPlayerAction : Action
{
    private enum Stage { Turn, Recover }

    private GeistDirectorBrain brain;
    private Quaternion startRotation;
    private float elapsed;
    private Stage stage;
    protected override Status OnStart()
    {
        brain = GameObject.GetComponentInParent<GeistDirectorBrain>();
        if (brain == null || !brain.IsGrabbingPlayer)
        {
            Debug.LogWarning("ThrowPlayer: needs a GeistDirectorBrain and a grabbed player (run GrabPlayer first)");
            return Status.Failure;
        }

        brain.DetermineThrowTarget(); // finds where to throw
        startRotation = brain.transform.rotation;
        elapsed = 0f;
        stage = Stage.Turn;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (stage == Stage.Turn)
        {
            // Ported from EnemyStateMachine.RotateGeistToTarget: turn toward the throw target over grabDuration
            float duration = brain.GrabDuration;
            if (elapsed < duration)
            {
                Vector3 direction = brain.ThrowLocation - brain.transform.position;
                if (direction.sqrMagnitude > 0.0001f)
                    brain.transform.rotation = Quaternion.Slerp(startRotation, Quaternion.LookRotation(direction), elapsed / duration);

                elapsed += Time.deltaTime;
                return Status.Running;
            }

            brain.ExecuteKinematicThrow(); // releases the player and launches them
            elapsed = 0f;
            stage = Stage.Recover;
            return Status.Running;
        }

        // the old sequence waited half a second after the throw before cleaning up
        elapsed += Time.deltaTime;
        return elapsed >= 0.5f ? Status.Success : Status.Running;
    }

    protected override void OnEnd()
    {
        brain = null;
    }
}

