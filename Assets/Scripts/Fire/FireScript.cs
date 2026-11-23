using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FireScript : MonoBehaviour
{
    public List<GameObject> myFireNodes;
    [SerializeField] private PersistantManager persistantManager;
    [SerializeField] private GameObject player;
    [SerializeField] private float distanceAway;

    private int fireCount;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Update()
    {//This is the most basic possible 'optimization' solution just for now, in the future there will be something closer to the audiozones system
        if(persistantManager == null)
        {
            persistantManager = FindFirstObjectByType<PersistantManager>();
            player = persistantManager.PlayerObject;
        }
        
        if (Vector3.Distance(player.transform.position,transform.position) < distanceAway)
            for (int i = 0; i < myFireNodes.Count; i++)
                myFireNodes[i].SetActive(true);
        else
            for (int i = 0; i < myFireNodes.Count; i++)
                myFireNodes[i].SetActive(false);

    }

    //Returns the amount of fire nodes this contains
    public int CheckFireCount() {
        //Ensures the value is reset
        fireCount = 0;

        //Iterates through every fire node, incrementing fire count if the fire active variable is true
        for (int i = 0; i < myFireNodes.Count; i++) {
            if (myFireNodes[i].GetComponent<FireNodeScript>().fireActive == true) {
                fireCount++;
            }
        }

        //Returns the final count
        return fireCount;
    }
}
