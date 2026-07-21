using UnityEngine;

/// <summary>
/// This interface is a generic component for interactable elements for unique event interactions. Allowing to change the UI 
/// elemenst outside of the PlayerUIManager script. 
/// Therefore decoupling the events from the script and allowing them to be scene bound
/// All event scripts with an interaction element will use this script
/// </summary>
public interface IInteractable
{
    // is the interactable available for interaction
    bool IsAvailableForInteraction { get; }
    // does hovering on this object hide the crosshair
    bool HideCrosshairOnLook { get; }
    // the prompt icon for interacting, usually F key indicator sprite
    Sprite PromptIcon { get; }
    // what color is the prompt, usually white
    Color PromptColor { get; }
    // what message goes on this interactable
    string PromptText { get; }
    //where does the billboard object appear on screen
    Transform BillboardParent { get; }

    //what happens when you look at it
    void OnLookEnter();
    // what happens when you look away
    void OnLookExit();
}
