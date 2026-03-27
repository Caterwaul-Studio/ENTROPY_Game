using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class TerminalTextureRendererManager : MonoBehaviour
{
    [SerializeField]
    private GameObject terminalUIGroup;

    [SerializeField]
    private GameObject terminalUIPrefab;
    [SerializeField]
    private Material terminalBaseMaterial;


    private int[] textureDensity = new int[] { 1024, 1024 };

    void Awake()
    {
        foreach (Terminal terminal in this.GetComponentsInChildren<Terminal>())
        {
            // hard coding avoiding overwriting the server terminals screen material...
            // eventually can come back to this and use a special prefab instance instead
            if (terminal.gameObject.name == "TerminalServer" || terminal.gameObject.name == "TerminalDormHall") continue;

            // get the mesh renderer for the individual screen of the terminal
            MeshRenderer uiMeshRenderer = terminal.TerminalScreen.GetComponent<MeshRenderer>();

            // create a new instance of the UI exclusively for this terminal
            GameObject uiInstance = Instantiate(terminalUIPrefab, terminalUIGroup.transform);

            // create a new render texture for this terminal
            RenderTexture rt = new RenderTexture(textureDensity[0], textureDensity[1], 24);

            // hook up the camera in the UI prefab to the new render texture
            Camera cam = uiInstance.GetComponentInChildren<Camera>();
            cam.targetTexture = rt;

            // clone the base material to make a new one and change the base texture
            Material material = new Material(terminalBaseMaterial);
            material.mainTexture = rt;

            material.SetTexture("_EmissionMap", rt);

            // set the material of the screen to the new material
            uiMeshRenderer.material = material;
            terminal.OnMaterial = material;

            // Hook up all the instances items to the main terminal scripts
            // uiReferences script acts as a way to easily find important references.
            TerminalScreenUIReferences uiReferences = uiInstance.GetComponentInChildren<TerminalScreenUIReferences>();

            TerminalDisabled td = terminal.transform.GetComponent<TerminalDisabled>();
            TerminalScreen ts = terminal.transform.GetComponent<TerminalScreen>();
            TerminalPopup tp = terminal.transform.GetComponent<TerminalPopup>();

            terminal.ALANScreenUI = uiReferences.ALANConnected;

            td.DisabledScreen = uiReferences.NeedsConnection;

            ts.TerminalText = uiReferences.ComputerTerminalText;

            tp.UploadText = uiReferences.UploadText;
            tp.PopupObject = uiReferences.UploadPopup;
            tp.UploadCompleteText = uiReferences.UploadCompleteText;
            tp.ScreenBlur = uiReferences.ScreenBlur;
            tp.TerminalText = uiReferences.ComputerTerminalText.gameObject;
            tp.ProgressFill = uiReferences.ProgressFill;



        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
   
        
    }

    // Update is called once per frame
    void Update()
    {
    }
}
