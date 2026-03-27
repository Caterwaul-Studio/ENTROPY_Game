using UnityEngine;

public class BodyOpenTrigger : MonoBehaviour, ISaveable
{
    [SerializeField]
    private BodyScareEvent bodyScareEvent;
    [SerializeField]
    private Collider player;
    private bool triggered = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // continue from save
        if (GlobalSaveManager.LoadFromSave) GlobalSaveManager.LoadSavable(this, false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == player)
        {
            this.GetComponent<Collider>().enabled = false;
            StartCoroutine(bodyScareEvent.PlayBodyScare());
   
        }
    }
    public void LoadSaveFile(string fileName)
    {
        // this will load data from the file to a variable we will use to change this objects data
        string path = Application.persistentDataPath;
        string loadedData = GlobalSaveManager.LoadTextFromFile(path, fileName);
        if (loadedData != null && loadedData != "")
        {
            if (loadedData == "True")
            {
                triggered = true;
                GetComponent<Collider>().enabled = false;
            }
        }
    }

    public void CreateSaveFile(string fileName)
    {
        // this will create a file backing up the data we give it
        string path = Application.persistentDataPath;
        GlobalSaveManager.SaveTextToFile(path, fileName, triggered.ToString());
    }
}
