using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class UniversalDevTools : EditorWindow
{
    //declare a gameobject so we can set the player as a reference.
    //with the reference to the player we can set the processes. 
    private GameObject player;

    //booleans for the all toggleable elements
    private bool god;
    private bool playerFreeMoveNoClip;
    private float noClipMoveSpeed = 10f;

    [MenuItem("Tools/UniversalDevTools")]
    public static void ShowWidow()
    {
        GetWindow(typeof(UniversalDevTools)); // Get window is a method inherited from EditorWindow
    }

    //this method will execute once the devtools window opens
    private void OnEnable()
    {
        FindPlayer();
    }

    public void OnGUI()
    {
        // Get a reference to the player
        // This is a reference to the container component of the player
        // ZeroGravity.cs is a component of "Zero G Player", therefore we need to search for the script in the children components
        if (player == null)
        {
            FindPlayer();
        }

        //creat a label for the window also bold it so we can see it
        GUILayout.Label("Player Controller Tools", EditorStyles.boldLabel);

        // Create a toggle to allow the player to go into god mode
        // While in god mode, the bool will block the Health related methods inside of ZeroGravity.cs
        // Therefore, we cannot die
        god = EditorGUILayout.Toggle("->God Mode", god);
        playerFreeMoveNoClip = EditorGUILayout.Toggle("->Free Move with No Clip",  playerFreeMoveNoClip); 


        //method calls to set up each section of the dev tools window
        PLayerControlsTools();
    }

    #region Helper Methods

    private void PLayerControlsTools()
    {
        //ensure that we have a player reference
        if (player != null)
        {
            // editor scripts cannot directly reference runtime scripts
            //therefore we must use proper namespace to handle the component dynamically
            // note: we need to ensure that we are looking in the children of the player reference
            // as stated above, "player" is a reference to the container. ZeroGravity.cs is a component of its child, not itself
            Component playerScript = null;
            //This is a reference to the fps display counter
            Component fpsCounter = null;
            //array of all components of the player container
            Component[] allComponents = player.GetComponentsInChildren<Component>();
            //look for the ZeroGravity.cs component in the children
            foreach(Component comp in allComponents)
            {
                //if the name of the component is "ZeroGravity"
                if (comp != null && comp.GetType().Name == "ZeroGravity")
                {
                    //set the player script to this component
                    playerScript = comp;
                }
                //if the name of the component is "FPS Display"
                if (comp != null && comp.GetType().Name == "FPSDisplay")
                {
                    // set the fps Counter to this component
                    fpsCounter = comp;
                    //let the checks continue as we are still looking for the "ZeroGravity" as well
                }
                //once we have found both components
                if(playerScript != null && fpsCounter != null)
                {
                    //break the loop
                    break;
                }
            }

            //ensure the script is not empty
            if (playerScript != null)
            {
                // use reflection to access all necessary properties created in ZeroGravity.cs
                System.Type type = playerScript.GetType();
                PropertyInfo godModeProp = type.GetProperty("GodMode");
                PropertyInfo freeMoveNoClipProp = type.GetProperty("PlayerFreeMoveNoClip");
                PropertyInfo noClipMoveSpeedProp = type.GetProperty("NoClipMoveSpeed");
                PropertyInfo healthProp = type.GetProperty("PlayerHealth");
                PropertyInfo isDeadProp = type.GetProperty("IsDead");
                // use reflection again to get the FPS property
                PropertyInfo fpsProp = null;
                if (fpsCounter != null)
                {
                    System.Type fpsType = fpsCounter.GetType();
                    fpsProp = fpsType.GetProperty("FPS");
                }
                else
                {
                    EditorGUILayout.HelpBox("No FPS Display component found on the player object!", MessageType.Warning);
                }
                //this is where we store logic for all player controller tools
                //calling methods that create and display the tools
                GodMode(playerScript, godModeProp);
                FreeMoveNoClip(playerScript, freeMoveNoClipProp);  
                NoClipMoveSpeedSlider(playerScript, noClipMoveSpeedProp);

                //display the current player status
                PlayerStatus(playerScript, healthProp, isDeadProp, fpsCounter, fpsProp);

                //This method forces the GUI to restore each frame. Without it the GUI only updates once when first being created or the when the player provides input
                Repaint();
            } // Below are error calls that show directly in the GUI, if something is wrong with finding a specific component you will know which one 
            else
            {
                EditorGUILayout.HelpBox("No ZeroGravity component found on the player object!", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Please assign a player reference to enable dev tools.", MessageType.Info);
        }
    }

    private void FindPlayer()
    {
        // Store a reference based n a call searching for "Player" component in scene
        // this is intensive, however only performs when player is null. So only happens once on scene load. 
        GameObject foundPlayer = GameObject.Find("Player");

        if (foundPlayer != null)
        {
            //if we found a player
            //store it as our player for the GUI 
            player = foundPlayer;
            return;
        }
    }

    private void GodMode(UnityEngine.Component playerScript, PropertyInfo godModeProp)
    {
        //if the god mode property reference we store is not empty
        if (godModeProp != null)
        {
            //set the GodMode bool within ZeroGravity.cs to the toggle we created for the EditorWindow
            godModeProp.SetValue(playerScript, god);
        }
        else
        {
            EditorGUILayout.HelpBox("GodMode property not found! Make sure you added it to ZeroGravity.cs", MessageType.Error);
        }
    }

    private void FreeMoveNoClip(UnityEngine.Component playerScript, PropertyInfo freeMoveNoClipProp)
    {
        if(freeMoveNoClipProp != null)
        {
            freeMoveNoClipProp.SetValue(playerScript, playerFreeMoveNoClip);

            //store logic to create a pop up to display the free move controls once the player clicks on the checkbox
            if (playerFreeMoveNoClip)
            {
                EditorGUILayout.HelpBox(
                    "Controls:\n" +
                    "W/S -> Forward & Back\n" +
                    "A/D -> Left & Right\n" +
                    "Space -> Up\n" +
                    "C -> Down",
                    MessageType.None
                    );
            }
        }
        else
        {
            EditorGUILayout.HelpBox("FreeMoveNoClip property not found! Make sure you added it to ZeroGravity.cs", MessageType.Error);
        }
    }

    private void NoClipMoveSpeedSlider(UnityEngine.Component playerScript, PropertyInfo noClipMoveSpeedProp)
    {
        if(noClipMoveSpeedProp == null)
        {
            EditorGUILayout.HelpBox("NoClipMoveSpeed property not found!", MessageType.Error);
            return;
        }

        noClipMoveSpeed = EditorGUILayout.Slider("->MoveSpeed", noClipMoveSpeed, 0.01f, 10f);
        noClipMoveSpeedProp.SetValue(playerScript, noClipMoveSpeed);
    }

    private void PlayerStatus(UnityEngine.Component playerScript, PropertyInfo healthProp, PropertyInfo isDeadProp, UnityEngine.Component fpsCounter, PropertyInfo fpsProp)
    {
        //display current status
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Status:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("FPS: " + Mathf.Round((float)fpsProp.GetValue(fpsCounter)), EditorStyles.boldLabel);
        EditorGUILayout.LabelField("God Mode: " + (god ? "ENABLED" : "Disabled"));

        if (healthProp != null)
        {
            EditorGUILayout.LabelField("Player Health: " + healthProp.GetValue(playerScript));
        }

        if (isDeadProp != null)
        {
            bool isDead = (bool)isDeadProp.GetValue(playerScript);
            EditorGUILayout.LabelField("Is Dead: " + (isDead ? "Yes" : "No"));
        }
    }

    #endregion
}
