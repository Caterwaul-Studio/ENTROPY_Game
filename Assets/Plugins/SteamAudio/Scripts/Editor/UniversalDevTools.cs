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
        if(player == null)
        {
            FindPlayer();
        }

        //creat a label for the window also bold it so we can see it
        GUILayout.Label("Player Controller Tools", EditorStyles.boldLabel);

        //get a reference to the player
        //This is a reference to the container component of the player
        //ZeroGravity.cs is a component of "Zero G Player", therefore we need to search for the script in the children components
        //player = EditorGUILayout.ObjectField("Player Container Reference", player, typeof(GameObject), true) as GameObject;

        //create a toggle to allow the player to go into god mode
        //while in god mode, the players health will be see to 99999999999. Making it very hard to die
        god = EditorGUILayout.Toggle("->God Mode", god);

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
            //array of all components of the player container
            Component[] allComponents = player.GetComponentsInChildren<Component>();
            //look for the ZeroGravity.cs component in the children
            foreach(Component comp in allComponents)
            {
                //if the name of the component is "ZeroGravity"
                if(comp != null && comp.GetType().Name == "ZeroGravity")
                {
                    //set the player script to this component
                    playerScript = comp;
                    //break once we've found it
                    break;
                }
            }

            //ensure the script is not empty
            if (playerScript != null)
            {
                // use reflection to access the GodMode property created in ZeroGravity.cs
                System.Type type = playerScript.GetType();
                PropertyInfo godModeProp = type.GetProperty("GodMode");
                PropertyInfo healthProp = type.GetProperty("PlayerHealth");
                PropertyInfo isDeadProp = type.GetProperty("IsDead");
                //if the god mode property reference we store is not empty
                if(godModeProp != null)
                {
                    //set the GodMode bool within ZeroGravity.cs to the toggle we created for the EditorWindow
                    godModeProp.SetValue(playerScript, god);

                    //display current status
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Status:", EditorStyles.boldLabel);
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
                else
                {
                    EditorGUILayout.HelpBox("GodMode property not found! Make sure you added it to ZeroGravity.cs", MessageType.Error);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No ZeroGravity component found on the player object!", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("PLease assign a player reference to enable dev tools.", MessageType.Info);
        }
    }

    private void FindPlayer()
    {
        GameObject foundPlayer = GameObject.Find("Player");

        if (foundPlayer != null)
        {
            player = foundPlayer;
            return;
        }
    }

    #endregion
}
