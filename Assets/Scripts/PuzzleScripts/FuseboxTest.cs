using UnityEngine;
using UnityEngine.InputSystem;


public class FuseboxTest : MonoBehaviour
{
    [SerializeField]
    GameObject powerManagerObject;
    PowerManager powerManager;
    public int storedFuses;
    private bool inRange = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        powerManager = powerManagerObject.GetComponent<PowerManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (inRange)
        {
            if (Keyboard.current[Key.Digit1].wasPressedThisFrame)
            {
                powerManager.PowerKeyPress("A1");
            }
            if (Keyboard.current[Key.Digit2].wasPressedThisFrame)
            {
                powerManager.PowerKeyPress("A2");
            }
            if (Keyboard.current[Key.Digit3].wasPressedThisFrame)
            {
                powerManager.PowerKeyPress("A3");
            }
            if (Keyboard.current[Key.Digit4].wasPressedThisFrame)
            {
                powerManager.PowerKeyPress("B1");
            }
            if (Keyboard.current[Key.Digit5].wasPressedThisFrame)
            {
                powerManager.PowerKeyPress("B2");
            }
            if (Keyboard.current[Key.Digit6].wasPressedThisFrame)
            {
                powerManager.PowerKeyPress("B3");
            }
            if (Keyboard.current[Key.Digit7].wasPressedThisFrame)
            {
                powerManager.PowerKeyPress("C1");
            }
            if (Keyboard.current[Key.Digit8].wasPressedThisFrame)
            {
                powerManager.PowerKeyPress("C2");
            }
            if (Keyboard.current[Key.Digit9].wasPressedThisFrame)
            {
                powerManager.PowerKeyPress("C3");
            }
            if (Keyboard.current[Key.Digit0].wasPressedThisFrame)
            {
                powerManager.PowerKeyPress("D1");
            }
            if (Keyboard.current[Key.Minus].wasPressedThisFrame)
            {
                powerManager.PowerKeyPress("D2");
            }
            if (Keyboard.current[Key.Equals].wasPressedThisFrame)
            {
                powerManager.PowerKeyPress("D3");
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            inRange = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            inRange = false;
        }
    }
}
