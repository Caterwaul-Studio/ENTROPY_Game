using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "FollowCutSceneRoute", story: "Follow Cutscene Route", category: "Action", id: "b0c04ebe31b06cfd9addb547680f3661")]
public partial class FollowCutSceneRouteAction : Action
{
    private GeistDirectorBrain brain;
    private GeistsCutsceneRoute route;
    private int index;

    protected override Status OnStart()
    {
        brain =  GameObject.GetComponent<GeistDirectorBrain>();
        route = null;
        return brain == null ? Status.Failure : Status.Running;
    }

    protected override Status OnUpdate()
    {
        // canceled (or finished elsewhere): drop it
        if (route != null && brain.ActiveCutsceneRoute != route)
        {
            route = null;
        }

        // Idle until a route is handed over
        if(route == null)
        {
            route = brain.ActiveCutsceneRoute;
            if (route == null) return Status.Running;
            index = route.snapToFirstPoint ? 1 : 0;
        }

        Transform self = brain.transform;

        //Skip missing points
        while ((index < route.points.Length && route.points[index] == null)) index++;
        
        if(index >= route.points.Length)
        {
            route = null;
            brain.FinishCutscene();
            return Status.Success;
        }

        Vector3 target = route.points[index].position;
        Vector3 toTarget = target - self.position;

        if(toTarget.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(toTarget);
            self.rotation = Quaternion.Slerp(self.rotation, targetRotation, Mathf.Clamp01(route.turnspeed * Time.deltaTime));
            self.position = Vector3.MoveTowards(self.position, target, route.speed * Time.deltaTime);
        }
        if((target - self.position).sqrMagnitude <= 0.0001f)
        {
            index++;
        }
        return Status.Running;
    }

    protected override void OnEnd()
    {
        route = null;
    }
}

