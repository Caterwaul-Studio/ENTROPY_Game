using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FireScript : MonoBehaviour
{
    public List<GameObject> myFireNodes;
    [SerializeField] private GameObject player;
    [SerializeField] private float distanceAway;

    private void Awake()
    {
        player = GameObject.FindWithTag("Player");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Update()
    {//This is the most basic possible 'optimization' solution just for now, in the future there will be something closer to the audiozones system
        if (Vector3.Distance(player.transform.position,transform.position) < distanceAway)
            for (int i = 0; i < myFireNodes.Count; i++)
                myFireNodes[i].SetActive(true);
        else
            for (int i = 0; i < myFireNodes.Count; i++)
                myFireNodes[i].SetActive(false);
    }
}
