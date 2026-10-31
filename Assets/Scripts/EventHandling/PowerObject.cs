using UnityEngine;
using System;

public interface PowerObject
{
    [Serializable]
    public enum PowerStates
    {
        Powered,
        Unpowered
    }
    PowerStates CurrentPowerState { get;}
    void SetPowerState(PowerStates state);
}
