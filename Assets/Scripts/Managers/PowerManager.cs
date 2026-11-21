using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PowerManager : MonoBehaviour
{
    private string[] startOff = { "A1", "A2", "A3", "B1", "B2", "B3", "C1", "C2", "C3", "D1", "D2", "D3"};
    
    [SerializeField]
    private GameObject fusebox;
    FuseboxTest fuseboxScript;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fuseboxScript = fusebox.GetComponent<FuseboxTest>();
        foreach(string zone in startOff){
            //Debug.Log("setting Zone " + zone);
            SetZonePower(zone, PowerObject.PowerStates.Unpowered);
        }
    }

    // Update is called once per frame
    void Update()
    {
        /*
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
        if (Keyboard.current[Key.Digit8].wasPressedThisFrame)
        {
            PowerKeyPress("C2");
        }
        if (Keyboard.current[Key.Digit9].wasPressedThisFrame)
        {
            PowerKeyPress("C3");
        }
        if (Keyboard.current[Key.Digit0].wasPressedThisFrame)
        {
            PowerKeyPress("D1");
        }
        if (Keyboard.current[Key.Minus].wasPressedThisFrame)
        {
            PowerKeyPress("D2");
        }
        if (Keyboard.current[Key.Equals].wasPressedThisFrame)
        {
            PowerKeyPress("D3");
        }*/
    }
    public void PowerKeyPress(string zone)
    { 
        switch (GetZonePower(zone))
        {
            case PowerObject.PowerStates.Powered:
                fuseboxScript.storedFuses++;
                SetZonePower(zone, PowerObject.PowerStates.Unpowered);
                break;
            case PowerObject.PowerStates.Unpowered:
                if (fuseboxScript.storedFuses > 0)
                {
                    fuseboxScript.storedFuses--;
                    SetZonePower(zone, PowerObject.PowerStates.Powered);
                }
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
