using UnityEngine;

public class DebugBars : MonoBehaviour
{
    //This is a script that has been used to debug the player controller by allowing me to expose variables without massive lag spikes due to the sheer number of serialized fields in the zero gravity script.
    private ZeroGravity playerController;
    private PlayerUIManager playerUI;
    public Collider PotentialGrabbedBar;
    public Collider GrabbedBar;
    public bool BarInRaycast;
    public bool BarInPeripheral;
    public Collider UIBar;
    public Vector3 PlayerGrabPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerController = FindFirstObjectByType<ZeroGravity>();
        playerUI = FindFirstObjectByType<PlayerUIManager>();
    }

    // Update is called once per frame
    void Update()
    {
        PotentialGrabbedBar = playerController.PotentialGrabbedBar;
        GrabbedBar = playerController.GrabbedBar;
        BarInRaycast = playerUI.BarInRaycast;
        BarInPeripheral = playerUI.BarInPeripheral;
        UIBar = playerUI.uiBar;
        PlayerGrabPosition = playerController.CurrentGrabPosition;
    }
}
