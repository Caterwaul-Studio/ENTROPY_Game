using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Save New Electrical Zone Location", story: "Save [NewElectricalZone] Location", category: "Action", id: "0c9592b4053fb18543eb074eb13208ff")]
public partial class SaveNewElectricalZoneLocationAction : Action
{
    [SerializeReference] public BlackboardVariable<Transform> NewElectricalZone;
    [SerializeReference] public BlackboardVariable<bool> GeistInPoweredZone;
    [SerializeReference] public BlackboardVariable<bool> NewElectricalZoneActivated;
    protected override Status OnStart()
    {
        if (NewElectricalZoneActivated != null) NewElectricalZoneActivated.Value = false;

        //setup the brain so we can get references to the bools
        GeistDirectorBrain brain = GameObject.GetComponentInParent<GeistDirectorBrain>();
        if (brain == null)
        {
            Debug.LogError("GeistDirectorBrain not found in parent hierarchy.");
            return Status.Failure;
        }

        if (NewElectricalZone == null || NewElectricalZone.Value == null)
        {
            Debug.LogWarning("SaveNewElectricalZoneLocation: NewElectricalZone is not set");
            return Status.Failure;
        }

        ElectricalZoneManager zone = NewElectricalZone.Value.GetComponent<ElectricalZoneManager>();
        if (zone == null || zone.CurrentPowerState != PowerObject.PowerStates.Powered)
            return Status.Failure;

        //store location so we can use it as a hint
        brain.SaveZoneHint(NewElectricalZone.Value);

        // is the Geist standing in any powered zone, including the new one
        GeistInPoweredZone.Value = brain.IsPositionInPoweredZone(brain.transform.position);
        return Status.Success;
    }
}

