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
            PowerKeyPress("A1");
        }
        if (Keyboard.current[Key.Digit2].wasPressedThisFrame)
        {
            PowerKeyPress("A2");
        }
        if (Keyboard.current[Key.Digit3].wasPressedThisFrame)
        {
            PowerKeyPress("A3"); 
        }
        if (Keyboard.current[Key.Digit4].wasPressedThisFrame)
        {
            PowerKeyPress("B1");
        }
        if (Keyboard.current[Key.Digit5].wasPressedThisFrame)
        {
            PowerKeyPress("B2");
        }
        if (Keyboard.current[Key.Digit6].wasPressedThisFrame)
        {
            PowerKeyPress("B3");
        }
        if (Keyboard.current[Key.Digit7].wasPressedThisFrame)
        {
            PowerKeyPress("C1");
        }
    }
    private void PowerKeyPress(string zone)
    {
        switch (GetZonePower(zone))
        {
            case PowerObject.PowerStates.Powered:
                SetZonePower(zone, PowerObject.PowerStates.Unpowered);
                break;
            case PowerObject.PowerStates.Unpowered:
                SetZonePower(zone, PowerObject.PowerStates.Powered);
                break;
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
