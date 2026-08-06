using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour, ISaveable
{
    [SerializeField] public IInventoryItem[] slots = new IInventoryItem[4];
    private int currentIndex = 0;

    [SerializeField] public bool InTutorial;

    [SerializeField] public bool ShowIndicators;

    [SerializeField] private int heldLayer = 8; // Layer for held objects
    [SerializeField] private int floatingObjLayer = 9; // Default layer for floating objects
    [SerializeField] private int iInteractableLayer = 10; // Layer for interactable objects

    [SerializeField] public Flashlight flashlight;
    [SerializeField] public FireExtinguisher fireExtinguisher;
    [SerializeField] public HeldFloatingObject heldFloatingObject;
    [SerializeField] public PickupScript pickupScript;

    [SerializeField] public PersistantManager persistant;

    [SerializeField] public GameObject pauseMenu;
    [SerializeField] public GameObject deathMenu;

    [SerializeField] public TutorialCanvases tutorialCanvases;

    [SerializeField] public GameObject toggleFlashlightCanvasPrefab;
    [SerializeField] public GameObject useFlashlightCanvasPrefab;

    [SerializeField] public GameObject toggleExtinguisherCanvasPrefab;
    [SerializeField] public GameObject useExtinguisherCanvasPrefab;

    [SerializeField] public GameObject toggleHeldObjectCanvasPrefab;
    [SerializeField] public GameObject throwHeldObjectCanvasPrefab;

    //Cache the original emission colors of materials to restore them when unequipping
    private Dictionary<Material, Color> _originalEmissionColors = new Dictionary<Material, Color>();

    private InventoryData _inventoryData;

    public int HeldLayer => heldLayer;
    public int FloatingObjLayer => floatingObjLayer;
    public int IInteractableLayer => iInteractableLayer;

    /// <summary>
    /// this method is called by the object to register it to the inventory slot, usually called in the Start()
    /// </summary>
    /// <param name="index"></param>
    /// <param name="item"></param>
    public void RegisterSlot(int index, IInventoryItem item)
    {
        if (item == null)
        {
            Debug.LogWarning($"InventoryManager: tried to register null item at slot {index}");
            return;
        }

        slots[index] = item;
        //Debug.Log($"InventoryManager: registered slot {index} with {item.GetType().Name} (is ISaveableInventoryItem: {item is ISaveableInventoryItem})");
    }

    public void ReleaseSlotIfActive(IInventoryItem item)
    {
        if (currentIndex >= 0 && currentIndex < slots.Length && slots[currentIndex] == item)
        {
            currentIndex = -1;
        }
    }

    /// <summary>
    /// This method requests to activate the object, therefore unequipping the other objects in the inventory
    /// </summary>
    /// <param name="index"></param>
    public void RequestActivate(int index)
    {
        if (index < 0 || index >= slots.Length || slots[index] == null)
        {
            Debug.LogWarning($"InventoryManager: no item registered at slot {index}");
            return;
        }

        if (index == currentIndex) return;

        if(currentIndex != -1 && slots[currentIndex] != null)
        {
            slots[currentIndex].Unequip();
        }
        currentIndex = index;
        slots[currentIndex].Equip();
    }

    /// <summary>
    /// This method requests to deactivate the currently active object, if any
    /// </summary>
    public void DeactivateCurrent()
    {
        if (currentIndex != -1 && slots[currentIndex] != null)
        {
            slots[currentIndex].Unequip();
        }
        currentIndex = -1;
    }

    /// <summary>
    /// This method assigns the held layer to the parent object and all its children, and disables emission on all materials of MeshRenderers in the hierarchy.
    /// </summary>
    /// <param name="parent"></param>
    public void SetChildrenToHoldLayer(GameObject parent)
    {
        parent.gameObject.layer = heldLayer; // Set the parent object to the held layer

        foreach (Transform child in parent.transform)
        {
            //this guard prevents any billboard objects childed from being reassigned
            if (child.gameObject.GetComponent<Canvas>() != null)
            {
                continue;
            }

            child.gameObject.layer = heldLayer;
            SetChildrenToHoldLayer(child.gameObject);

            MeshRenderer mr = child.gameObject.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                foreach (Material mat in mr.materials) // instances per-renderer material to avoid changing the original material
                {
                    if (!_originalEmissionColors.ContainsKey(mat))
                    {
                        _originalEmissionColors[mat] = mat.GetColor("_EmissionColor");
                    }
                    mat.DisableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.black); // set emission color to black to disable emission)
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack; // ensure that the emission is treated as black for GI
                }
            }
        }
    }

    /// <summary>
    /// This method assigns the specified layer to the parent object and all its children, and restores the original emission colors on all materials of MeshRenderers in the hierarchy.
    /// It's important to allow for a unique layer assignment as not all objects are default on the same layer, FloatingObject vs IInteractable for example.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="layer"></param>
    public void SetChildrenToDefaultLayer(GameObject parent, int layer)
    {
        parent.gameObject.layer = layer; // Set the parent object to the held layer

        foreach (Transform child in parent.transform)
        {
            //this guard prevents any billboard objects childed from being reassigned
            if (child.gameObject.GetComponent<Canvas>() != null)
            {
                continue;
            }

            child.gameObject.layer = layer;
            SetChildrenToDefaultLayer(child.gameObject, layer);

            MeshRenderer mr = child.gameObject.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                foreach (Material mat in mr.materials)
                {
                    mat.EnableKeyword("_EMISSION");
                    //restore original emission color if it was cached
                    if (_originalEmissionColors.TryGetValue(mat, out Color originalColor))
                    {
                        mat.SetColor("_EmissionColor", originalColor);
                    }

                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; // reset GI flags to default
                }
            }
        }
    }

    #region Global Save Manager Integration

    private void StoreInventoryData()
    {
        string[] data = new string[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            //Debug.Log($"StoreInventoryData: slot {i} = {(slots[i] == null ? "null" : slots[i].GetType().Name)}");
            if (slots[i] is ISaveableInventoryItem saveable)
            {
                data[i] = saveable.GetSaveData();
                //Debug.Log($"StoreInventoryData: slot {i} saved data = {data[i]}");
            }
            else
            {
                //Debug.Log($"StoreInventoryData: slot {i} is NOT ISaveableInventoryItem, skipping");
            }
        }
        _inventoryData = new InventoryData(currentIndex, data);

    }

    private void LoadInventoryData()
    {
        StartCoroutine(ApplyLoadedInventory());
    }

    private IEnumerator ApplyLoadedInventory()
    {
        yield return null; // let new items finish RegisterSlot in Start()

        for (int i = 0; i < slots.Length; i++)
        {
            //Debug.Log($"ApplyLoadedInventory: itemData length = {(_inventoryData.itemData == null ? "null" : _inventoryData.itemData.Length.ToString())}");
            if (slots[i] is ISaveableInventoryItem saveable)
            {
                string json = (_inventoryData.itemData != null && i < _inventoryData.itemData.Length)
                    ? _inventoryData.itemData[i]
                    : null;
                //Debug.Log($"ApplyLoadedInventory: slot {i} json = '{json}'");
                if (!string.IsNullOrEmpty(json))
                {
                    //Debug.Log($"ApplyLoadedInventory: calling LoadSaveData on slot {i}");
                    saveable.LoadSaveData(json);
                }
                else
                {
                    //Debug.Log($"ApplyLoadedInventory: calling ClearRuntimeState on slot {i}");
                    saveable.ClearRuntimeState();
                }
            }
            else
            {
                //Debug.Log($"ApplyLoadedInventory: slot {i} is NOT ISaveableInventoryItem, skipping");
            }
        }

            int indexToLoad = _inventoryData.currentIndex;
            if(indexToLoad < 0 || indexToLoad >= slots.Length || slots[indexToLoad] == null)
            {
                DeactivateCurrent();
                yield break;
            }

            currentIndex = -1;
            RequestActivate(indexToLoad);
        }

    public void CreateSaveFile(string fileName)
    {
        StoreInventoryData();
        string json = JsonUtility.ToJson(_inventoryData);
        //Debug.Log($"CreateSaveFile: full inventory json = {json}");
        string path = Application.persistentDataPath;
        GlobalSaveManager.SaveTextToFile(path, fileName, json);
    }

    public void LoadSaveFile(string fileName)
    {
        string path = Application.persistentDataPath;
        string loadedData = GlobalSaveManager.LoadTextFromFile(path, fileName);
        //Debug.Log($"LoadSaveFile: raw loaded json = {loadedData}");
        if (!string.IsNullOrEmpty(loadedData))
        {
            _inventoryData = JsonUtility.FromJson<InventoryData>(loadedData);
            //Debug.Log($"LoadSaveFile: parsed itemData = {(_inventoryData.itemData == null ? "null" : string.Join(" | ", _inventoryData.itemData))}");
            LoadInventoryData();
        }
    }

    #endregion
}
