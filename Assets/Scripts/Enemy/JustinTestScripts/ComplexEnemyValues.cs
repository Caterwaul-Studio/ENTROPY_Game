using System.Collections.Generic;
using UnityEngine;

public class ComplexEnemyValues : MonoBehaviour
{
    public static ComplexEnemyValues Instance { get; private set; }

    public void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

   
}
