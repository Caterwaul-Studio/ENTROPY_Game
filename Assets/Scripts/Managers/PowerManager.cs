using UnityEngine;
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
