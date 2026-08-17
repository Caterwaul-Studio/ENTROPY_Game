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
        buildDateTxt = GetComponent<TextMeshProUGUI>();
        //Debug.Log(buildDateTxt.text);

        BuildInfo info = Resources.Load<BuildInfo>("BuildInfo");
        string dateString = (info != null && !string.IsNullOrEmpty(info.buildDate))
            ? info.buildDate
            : "unknown";

        //set text
        buildDateTxt.text = "test build v:/"+ dateString;
    }
}
