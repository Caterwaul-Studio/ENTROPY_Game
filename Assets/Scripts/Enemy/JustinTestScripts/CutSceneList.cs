using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SpawnManagerScriptableObject", order = 1)]

public class CutSceneList : ScriptableObject
{
    public enum CutsceneState
    {
        Move,
        Attack,
        Chase,
        Idle,
        Grab,
    }

    //[Serializable]
    public List<CutsceneState> List;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
