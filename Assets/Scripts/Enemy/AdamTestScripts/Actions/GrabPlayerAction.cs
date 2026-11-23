using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "GrabPlayer", story: "GrabPlayer", category: "Action", id: "57b5710cd1bc2890fbd9c50ba5f96719")]
public partial class GrabPlayerAction : Action
{
    private GeistDirectorBrain brain;
    private Transform cam;
    private Quaternion startRotation;
    private float elapsed;
    protected override Status OnStart()
    {
        brain = GameObject.GetComponentInParent<GeistDirectorBrain>();
        if (brain == null || !brain.CanGrabPlayer)
            return Status.Failure; // missing references, or the player is already grabbed

        brain.LockPlayerInputs();

        cam = brain.PlayerCamera;
        startRotation = cam.rotation;
        elapsed = 0f;
        return Status.Running;

    }

    protected override Status OnUpdate()
    {
        float duration = brain.ForceLookDuration;

        if (elapsed < duration)
        {
            Vector3 direction = brain.transform.position - cam.position;
            if (direction.sqrMagnitude > 0.0001f)
                cam.rotation = Quaternion.Slerp(startRotation, Quaternion.LookRotation(direction), elapsed / duration);

            elapsed += Time.deltaTime;
            return Status.Running;
        }

        cam.LookAt(brain.transform); // finish exactly on the Geist
        return Status.Success;
    }

    protected override void OnEnd()
    {
        brain = null;
        cam = null;
    }
}

