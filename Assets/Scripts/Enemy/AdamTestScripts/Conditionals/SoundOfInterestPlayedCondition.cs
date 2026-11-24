using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "SoundOfInterestPlayed", story: "SoundofInterest Played?", category: "Conditions", id: "9966ae7f282d0106b4cc58da1eea6cc4")]
public partial class SoundOfInterestPlayedCondition : Condition
{

    public override bool IsTrue()
    {
        return true;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
