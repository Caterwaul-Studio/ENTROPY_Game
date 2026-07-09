using UnityEngine;

/// <summary>
/// This interface is a generic component for interactable elements for unique event interactions. Allowing to change the UI elemenst outside of the PlayerUIManager script. 
/// Therefore decoupling the events from the script and allowing them to be scene bound
/// All event scripts with an interaction element will use this script
/// </summary>
public interface IInteractable
{
    bool IsAvailableForInteraction { get; }
    bool HideCrosshairOnLook { get; }
    Sprite PromptIcon { get; }
    Color PromptColor { get; }
    string PromptText { get; }
    Transform BillboardParent { get; }

    void OnLookEnter();
    void OnLookExit();
}
