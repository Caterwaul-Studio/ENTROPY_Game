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
        if (Keyboard.current[Key.Digit1].wasPressedThisFrame)
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
        if (Keyboard.current[Key.Digit2].wasPressedThisFrame)
        {
            switch (GetZonePower("A2"))
            {
                case PowerObject.PowerStates.Powered:
                    SetZonePower("A2", PowerObject.PowerStates.Unpowered);
                    break;
                case PowerObject.PowerStates.Unpowered:
                    SetZonePower("A2", PowerObject.PowerStates.Powered);
                    break;
            }
        }
        if (Keyboard.current[Key.Digit3].wasPressedThisFrame)
        {
            switch (GetZonePower("A3"))
            {
                case PowerObject.PowerStates.Powered:
                    SetZonePower("A3", PowerObject.PowerStates.Unpowered);
                    break;
                case PowerObject.PowerStates.Unpowered:
                    SetZonePower("A3", PowerObject.PowerStates.Powered);
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
