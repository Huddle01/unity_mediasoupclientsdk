using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class ScreenLogger : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _logTextBox;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Started up logging");
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        _logTextBox.text += logString + Environment.NewLine;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }
}
