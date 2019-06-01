using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    [SerializeField] private ConsoleGUI consoleGUI;

    private long _stopwatchFrequency;

    private void Awake()
    {
        _stopwatchFrequency = System.Diagnostics.Stopwatch.Frequency;
    }

    private void Start()
    {
        Console.Init(consoleGUI);
    }

    public void Update()
    {
        Console.ConsoleUpdate();
    }
}
