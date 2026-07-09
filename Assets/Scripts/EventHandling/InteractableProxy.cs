using UnityEngine;

/// <summary>
/// This is a generic forwarding component inhereting IInteractable. Place this on any collider that the raycast should hit to interact with, 
/// and point it at a MonoBehaviour elesewhere in the scene (typically an event/controller script) that implements IInteractable.
/// This is the only wiring class needed for new interactable events 
/// implement IInteractable on the event script itself, then reference it here
/// </summary>
public class InteractableProxy : MonoBehaviour, IInteractable
{
    [Tooltip("Must be a component implemening IInteractable.")]
    [SerializeField] private MonoBehaviour source;

    [Tooltip("Where the billboard prompt should anchor, defaults to this object's transform if unset")]
    [SerializeField] private Transform billboardAnchor;

    private IInteractable Source => source as IInteractable;

    public bool IsAvailableForInteraction => Source != null && Source.IsAvailableForInteraction;
    public bool HideCrosshairOnLook => Source != null && Source.HideCrosshairOnLook;
    public Sprite PromptIcon
    {
        get
        {
            Debug.Log($"[Proxy:{name}] source={source?.name}, PromptIcon={Source?.PromptIcon}");
            return Source?.PromptIcon;
        }
    }
    public Color PromptColor => Source != null ? Source.PromptColor : Color.white;
    public string PromptText => Source?.PromptText ?? "";
    public Transform BillboardParent
    {
        get
        {
            Debug.Log($"[Proxy:{name}] billboardAnchor={billboardAnchor?.name}, resolved={(billboardAnchor != null ? billboardAnchor : transform)?.name}");
            return billboardAnchor != null ? billboardAnchor : transform;
        }
    }

    public void OnLookEnter() => Source?.OnLookEnter();
    public void OnLookExit() => Source?.OnLookExit(); 

#if UNITY_EDITOR
    private void OnValidate()
    {
        if(source != null && !(source is IInteractable))
        {
            Debug.LogError($"{nameof(InteractableProxy)} on {name}: assigned source" + $"'{source.GetType().Name}' does not implement IInteractable.", this);
        }        
    }
#endif
}
