using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PowerManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current[Key.P].wasPressedThisFrame)
        {
            switch (GetZonePower("TestZone"))
            {
                case PowerObject.PowerStates.Powered:
                    SetZonePower("TestZone",PowerObject.PowerStates.Unpowered);
                    break;
                case PowerObject.PowerStates.Unpowered:
                    SetZonePower("TestZone", PowerObject.PowerStates.Powered);
                    break;
            }
        }   
    }
    public PowerObject.PowerStates GetZonePower(string zoneName)
    {
        return this.transform.Find(zoneName).GetComponent<ElectricalZoneManager>().CurrentPowerState;
    }

    public void SetZonePower(string zoneName, PowerObject.PowerStates state)
    {
        this.transform.Find(zoneName).GetComponent<ElectricalZoneManager>().SetPowerState(state);
    }
}
