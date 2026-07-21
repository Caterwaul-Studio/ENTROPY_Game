using System;
using System.Collections.Generic;
using UnityEngine;
using static WristMonitor;
// Stores the player's data for use in saving and loading
[Serializable]
public class PlayerData
{
    [SerializeField]
    private Vector3 position;
    public Vector3 Position { get { return position; } }
    [SerializeField]
    private Quaternion rotation;
    public Quaternion Rotation { get { return rotation; } }
    [SerializeField]
    private int health;
    public int Health { get { return health; } }
    [SerializeField]
    private int stims;
    public int Stims { get { return stims; } }
    [SerializeField]
    private bool hasUsedStim;
    public bool HasUsedStim {  get { return hasUsedStim; } }
    //[SerializeField]
    //private bool inTutorial;
    //public bool InTutorial { get { return inTutorial; } }
    [SerializeField]
    private bool[] accessPermissions;
    public bool[] AccessPermissions { get { return accessPermissions; } }
    [SerializeField]
    private bool hasWristMonitor;
    public bool HasWristMonitor { get { return hasWristMonitor; } }
    [SerializeField]
    private bool showingWristMonitor;
    public bool ShowingWristMonitor { get { return showingWristMonitor; } }
    [SerializeField]
    private List<Objective> mainObjectives;
    public List<Objective> MainObjectives { get { return mainObjectives; } }
    [SerializeField]
    private List<Objective> completedObjectives;
    public List<Objective> CompletedObjectives { get { return completedObjectives; } }
    public PlayerData(
        Vector3 _position,
        Quaternion _rotation,
        int _health, 
        int _stims, 
        bool _hasUsedStim, 
        //bool _inTutorial,
        bool[] _accessPermissions,
        bool _hasWristMonitor,
        bool _showingWristMonitor,
        List<Objective> _mainObjectives,
        List<Objective> _completedObjectives
        )
    {
        position = _position;
        rotation = _rotation;
        health = _health;
        stims = _stims;
        hasUsedStim = _hasUsedStim;
        //inTutorial = _inTutorial;
        accessPermissions = _accessPermissions;
        hasWristMonitor = _hasWristMonitor;
        showingWristMonitor = _showingWristMonitor;
        mainObjectives = _mainObjectives;
        completedObjectives = _completedObjectives;
    }
}
