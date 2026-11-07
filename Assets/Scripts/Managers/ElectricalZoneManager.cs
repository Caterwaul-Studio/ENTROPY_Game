using UnityEngine;

public class ElectricalZoneManager : MonoBehaviour, PowerObject
{
    [SerializeField]
    private GameObject doors;
    [SerializeField]
    private GameObject connectors;
    [SerializeField]
    private GameObject lights;
    [SerializeField]
    private GameObject wires;
    [SerializeField]
    private GameObject elecBarCover;
    private Transform[] elecBars;
    [SerializeField]
    private PowerObject.PowerStates currentPowerState;
    public PowerObject.PowerStates CurrentPowerState
    {
        get { return currentPowerState; }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (elecBarCover != null)
        {
            elecBars = elecBarCover.GetComponentsInChildren<Transform>();
        }
        SetPowerState(currentPowerState);
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void SetPowerState (PowerObject.PowerStates powerState) {
        currentPowerState = powerState;
        DoorScript[] doorScripts = doors.GetComponentsInChildren<DoorScript>();
        foreach (DoorScript doorScript in doorScripts) {
            doorScript.SetPowerState(powerState);
        }
        DoorScript[] connectorScripts = connectors.GetComponentsInChildren<DoorScript>();
        foreach (DoorScript connectorScript in connectorScripts)
        {
            connectorScript.SetPowerState(powerState);
        }
        if (wires != null)
        {
            LiveWire[] wireScripts = wires.GetComponentsInChildren<LiveWire>();
            foreach (LiveWire wireScript in wireScripts)
            {
                wireScript.SetPowerState(powerState);
            }
        }
        if(elecBarCover != null)
        {
            foreach (Transform elecBar in elecBars)
            {
                elecBar.gameObject.SetActive(powerState == PowerObject.PowerStates.Unpowered ? false : true);
            }
        }
    }
}
