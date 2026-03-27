using UnityEngine;
using TMPro;
using System;

public class BuildVersionDate : MonoBehaviour
{
    DateTime currDate;

    TextMeshProUGUI buildDateTxt;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //initialize 
        currDate = DateTime.Today;
        Debug.Log(currDate.ToString());
        buildDateTxt = GetComponent<TextMeshProUGUI>();
        Debug.Log(buildDateTxt.text);

        //set text
        buildDateTxt.text = "test build v:/"+ currDate.ToShortDateString();
    }
}
